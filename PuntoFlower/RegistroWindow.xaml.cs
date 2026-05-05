using PuntoFlower.Data;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PuntoFlower
{
    public partial class RegistroWindow : Window
    {
        public RegistroWindow()
        {
            InitializeComponent();
        }

        // --- LÓGICA PARA MOSTRAR CONTRASEÑA ---
        private void BtnShowPass_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            RevelarPassword();
        }

        private void BtnShowPass_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            OcultarPassword();
        }

        private void BtnShowPass_MouseLeave(object sender, MouseEventArgs e)
        {
            OcultarPassword();
        }

        private void RevelarPassword()
        {
            txtNewPassRevelada.Text = txtNewPass.Password;
            txtNewPass.Visibility = Visibility.Collapsed;
            txtNewPassRevelada.Visibility = Visibility.Visible;
            btnShowPass.Foreground = new SolidColorBrush(Color.FromRgb(52, 152, 219)); // Cambia a azul al ver
        }

        private void OcultarPassword()
        {
            txtNewPassRevelada.Visibility = Visibility.Collapsed;
            txtNewPass.Visibility = Visibility.Visible;
            btnShowPass.Foreground = new SolidColorBrush(Color.FromRgb(149, 165, 166)); // Vuelve a gris
        }

        // --- LÓGICA DE REGISTRO ---
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
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
                    // Se inserta el usuario con el rol por defecto 'Empleado' y estado 'Pendiente'
                    string query = "INSERT INTO Usuarios (Username, PasswordHash, Estado, Rol) VALUES (@u, @p, @est, @rol)";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@u", txtNewUser.Text);
                    cmd.Parameters.AddWithValue("@p", txtNewPass.Password);
                    cmd.Parameters.AddWithValue("@est", "Pendiente");
                    cmd.Parameters.AddWithValue("@rol", "Empleado"); // Rol asignado por defecto

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