using PuntoFlower.Data;
using PuntoFlower.Models;
using System;
using System.Data.SqlClient;
using System.Windows;

namespace PuntoFlower
{
    public partial class LicenciaWindow : Window
    {
        private string _currentHWID;

        public LicenciaWindow()
        {
            InitializeComponent();
            _currentHWID = LicenciaManager.ObtenerHWID();
            txtHWID.Text = _currentHWID;
        }

        // Este es el método exacto que busca el evento Click="BtnActivar_Click" del XAML
        private void BtnActivar_Click(object sender, RoutedEventArgs e)
        {
            string licenciaIngresada = txtLicencia.Text.Trim();
            string licenciaCorrecta = LicenciaManager.GenerarLicencia(_currentHWID);

            if (licenciaIngresada == licenciaCorrecta)
            {
                ConexionDB db = new ConexionDB();
                try
                {
                    using (SqlConnection con = db.OpenConnection())
                    {
                        // Limpiar activaciones previas
                        SqlCommand cmdClear = new SqlCommand("DELETE FROM LicenciaSistema", con);
                        cmdClear.ExecuteNonQuery();

                        // Guardar la nueva licencia legítima
                        SqlCommand cmdInsert = new SqlCommand(
                            "INSERT INTO LicenciaSistema (LicenciaClave, EquipoHWID) VALUES (@lic, @hwid)", con);
                        cmdInsert.Parameters.AddWithValue("@lic", licenciaIngresada);
                        cmdInsert.Parameters.AddWithValue("@hwid", _currentHWID);
                        cmdInsert.ExecuteNonQuery();
                    }

                    MessageBox.Show("¡Sistema Activado con Éxito! Reinicie la aplicación para ingresar.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al guardar la activación en la base de datos: " + ex.Message, "Error");
                }
            }
            else
            {
                MessageBox.Show("La clave de licencia introducida es incorrecta o no corresponde a este equipo.", "Licencia Inválida", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}