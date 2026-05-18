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
            ConexionDB db = new ConexionDB();

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "SELECT TOP 1 LicenciaClave, EquipoHWID FROM LicenciaSistema";
                    SqlCommand cmd = new SqlCommand(query, con);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string licenciaGuardada = reader["LicenciaClave"].ToString();
                            string hwidGuardado = reader["EquipoHWID"].ToString();

                            // 1. Validar que el hardware de la máquina coincida con el registrado en la BD
                            if (hwidActual != hwidGuardado) return false;

                            // 2. Validar mediante algoritmo matemático que la licencia sea la correcta para el HWID
                            string licenciaVerificadora = LicenciaManager.GenerarLicencia(hwidActual);
                            if (licenciaGuardada == licenciaVerificadora)
                            {
                                return true; // Licencia legítima
                            }
                        }
                    }
                }
            }
            catch
            {
                // Si la tabla no existe o falla la conexión base, se bloquea por seguridad
                return false;
            }

            return false;
        }
    }
}