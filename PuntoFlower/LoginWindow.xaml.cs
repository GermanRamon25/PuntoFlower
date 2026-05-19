using PuntoFlower.Data;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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
            btnShowPassword.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Verde al activar vista
        }

        private void OcultarPassword()
        {
            txtPasswordRevelada.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Visible;
            btnShowPassword.Foreground = new SolidColorBrush(Color.FromRgb(149, 165, 166)); // Vuelve a gris obscuro
        }

        // --- LÓGICA DE ACCESO CON AUDITADO LOCAL ---
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Password))
            {
                MessageBox.Show("Por favor, introduce tu usuario y contraseña.", "Campos Requeridos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // Consultamos el Rol y el Estado de activación en la base de datos local
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
                                // Cerramos el reader para liberar la conexión activa de SQL Server y permitir el UPDATE
                                reader.Close();

                                // AUDITORÍA LOCAL AUTOMÁTICA: Guardamos marca de tiempo de entrada
                                string queryAuditoria = "UPDATE Usuarios SET UltimoAcceso = GETDATE() WHERE Username = @u";
                                using (SqlCommand cmdAudit = new SqlCommand(queryAuditoria, con))
                                {
                                    cmdAudit.Parameters.AddWithValue("@u", txtUsername.Text);
                                    cmdAudit.ExecuteNonQuery();
                                }

                                Session.UsuarioActual = txtUsername.Text;
                                Session.RolActual = rol;

                                MainWindow main = new MainWindow();
                                main.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Tu cuenta está pendiente de activación por un administrador.", "Cuenta Inactiva", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Usuario o contraseña incorrectos.", "Error de Credenciales", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al enlazar con la base de datos de la sucursal: " + ex.Message, "Fallo de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
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