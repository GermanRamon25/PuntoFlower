using PuntoFlower.Data;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace PuntoFlower
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // --- LÓGICA PARA MOSTRAR CONTRASEÑA ---
        private void BtnShowPassword_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            RevelarPassword();
        }

        private void BtnShowPassword_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            OcultarPassword();
        }

        private void BtnShowPassword_MouseLeave(object sender, MouseEventArgs e)
        {
            OcultarPassword();
        }

        private void RevelarPassword()
        {
            txtPasswordRevelada.Text = txtPassword.Password;
            txtPassword.Visibility = Visibility.Collapsed;
            txtPasswordRevelada.Visibility = Visibility.Visible;
            btnShowPassword.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96)); // Cambia a verde al ver
        }

        private void OcultarPassword()
        {
            txtPasswordRevelada.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Visible;
            btnShowPassword.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166)); // Vuelve a gris
        }

        // --- LÓGICA DE LOGIN EXISTENTE ---
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Password))
            {
                MessageBox.Show("Por favor, introduce tu usuario y contraseña.");
                return;
            }

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "SELECT Estado, Rol FROM Usuarios WHERE Username = @u AND PasswordHash = @p";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@u", txtUsername.Text);
                    cmd.Parameters.AddWithValue("@p", txtPassword.Password);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string estado = reader["Estado"].ToString();
                            string rol = reader["Rol"].ToString();

                            if (estado == "Activo")
                            {
                                Session.UsuarioActual = txtUsername.Text;
                                Session.RolActual = rol;

                                MainWindow main = new MainWindow();
                                main.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Tu cuenta está pendiente de activación por un administrador.");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Usuario o contraseña incorrectos.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar con la base de datos: " + ex.Message);
            }
        }

        private void BtnAbrirRegistro_Click(object sender, RoutedEventArgs e)
        {
            RegistroWindow registro = new RegistroWindow();
            registro.ShowDialog();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}