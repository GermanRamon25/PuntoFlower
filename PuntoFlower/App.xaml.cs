using PuntoFlower.Data;
using PuntoFlower.Models;
using PuntoFlower.Views;
using System;
using System.Data.SqlClient;
using System.Windows;

namespace PuntoFlower
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (EsLicenciaValida())
            {
                // Si pasa la auditoría de hardware, abre el login normal del sistema
                LoginWindow login = new LoginWindow();
                login.Show();
            }
            else
            {
                // Si es un fraude (movieron el archivo o no tiene licencia), abre ventana de bloqueo
                LicenciaWindow licenciaWin = new LicenciaWindow();
                if (licenciaWin.ShowDialog() == true)
                {
                    // Si se activa con éxito en la ventana, cerramos el hilo actual y pedimos que lo abran de nuevo
                    Application.Current.Shutdown();
                }
                else
                {
                    // Si cierran la ventana de activación sin registrarla, el sistema se apaga
                    Application.Current.Shutdown();
                }
            }
        }

        private bool EsLicenciaValida()
        {
            string hwidActual = LicenciaManager.ObtenerHWID();
            string licenciaEsperada = LicenciaManager.GenerarLicencia(hwidActual);
            ConexionDB db = new ConexionDB();

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // Buscamos específicamente el HWID de esta máquina (soporta las 4 sucursales en la misma BD)
                    string query = "SELECT TOP 1 LicenciaClave, EquipoHWID FROM LicenciaSistema WHERE EquipoHWID = @hwid";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@hwid", hwidActual);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string licenciaGuardada = reader["LicenciaClave"].ToString().Trim();
                            string hwidGuardado = reader["EquipoHWID"].ToString().Trim();

                            if (hwidActual == hwidGuardado &&
                                licenciaGuardada.Equals(licenciaEsperada, StringComparison.OrdinalIgnoreCase))
                            {
                                // Actualizamos/creamos el respaldo local en ProgramData para futuras aperturas sin internet
                                LicenciaManager.GuardarRespaldoLocal(licenciaGuardada);
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Si hay micro-corte o lentitud en Azure SQL, verificamos el respaldo local ligado al HWID
                if (LicenciaManager.ValidarRespaldoLocal())
                {
                    return true;
                }
                return false;
            }

            // Respaldo adicional por si la consulta no falló pero la red devolvió vacío transitoriamente
            return LicenciaManager.ValidarRespaldoLocal();
        }
    }
}