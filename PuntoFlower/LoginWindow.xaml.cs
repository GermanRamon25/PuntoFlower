using PuntoFlower.Data;
using System;
using System.Data.SqlClient;
using System.Windows;
using PuntoFlower.Views; // Asegúrate de importar el namespace donde está RegistroWindow

namespace PuntoFlower
{
    public partial class RegistroWindow : Window
    {
        public RegistroWindow() => InitializeComponent();

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validamos que los campos no estén vacíos
            if (string.IsNullOrEmpty(txtNewUser.Text) || string.IsNullOrEmpty(txtNewPass.Password))
            {
                MessageBox.Show("Por favor, completa todos los campos.");
                return;
            }

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // CAMBIO AQUÍ: Incluimos la columna 'Estado' y le asignamos 'Pendiente'
                    string query = "INSERT INTO Usuarios (Username, PasswordHash, Estado) VALUES (@u, @p, @est)";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@u", txtNewUser.Text);
                    cmd.Parameters.AddWithValue("@p", txtNewPass.Password);
                    cmd.Parameters.AddWithValue("@est", "Pendiente"); // Marcamos como pendiente por seguridad

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Registro exitoso. Tu cuenta está pendiente de activación por un administrador.");
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar: " + ex.Message);
            }
        }
    }
}