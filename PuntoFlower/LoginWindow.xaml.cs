using PuntoFlower.Data;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PuntoFlower
{
    public partial class LoginWindow : Window
    {
        private bool passwordVisible = false;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // --- LÓGICA DE OJO EN MODO INTERRUPTOR (UN SOLO CLIC) ---
        private void BtnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            passwordVisible = !passwordVisible;

            if (passwordVisible)
            {
                txtPasswordRevelada.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordRevelada.Visibility = Visibility.Visible;
                txtPasswordRevelada.Focus();
                txtPasswordRevelada.CaretIndex = txtPasswordRevelada.Text.Length;
                btnShowPassword.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Verde esmeralda activo
            }
            else
            {
                txtPassword.Password = txtPasswordRevelada.Text;
                txtPasswordRevelada.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                txtPassword.Focus();
                btnShowPassword.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // Gris suave inactivo
            }
        }

        // Sincroniza en tiempo real lo que se escribe mientras la contraseña está visible
        private void TxtPasswordRevelada_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (passwordVisible && txtPassword.Password != txtPasswordRevelada.Text)
            {
                txtPassword.Password = txtPasswordRevelada.Text;
            }
        }

        // --- LÓGICA DE ACCESO CON BLINDAJE MULTI-SUCURSAL ---
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string passwordActual = passwordVisible ? txtPasswordRevelada.Text : txtPassword.Password;

            if (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(passwordActual))
            {
                MessageBox.Show("Por favor, introduce tu usuario y contraseña.", "Campos Requeridos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string usuarioFormateado = txtUsername.Text.Trim().ToLower();
            string passwordIngresada = passwordActual.Trim();

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // MODIFICACIÓN: Solicitamos también el Id para la clase Session
                    string query = "SELECT Id, Estado, Rol, Sucursal FROM Usuarios WHERE Username = @u AND PasswordHash = @p";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@u", usuarioFormateado);
                    cmd.Parameters.AddWithValue("@p", passwordIngresada);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string estado = reader["Estado"].ToString();

                            if (estado == "Activo")
                            {
                                int idUsuario = Convert.ToInt32(reader["Id"]);
                                string rol = reader["Rol"].ToString();
                                string sucursal = reader["Sucursal"].ToString();

                                reader.Close();

                                // Auditoría de acceso
                                string queryAuditoria = "UPDATE Usuarios SET UltimoAcceso = GETDATE() WHERE Username = @u";
                                using (SqlCommand cmdAudit = new SqlCommand(queryAuditoria, con))
                                {
                                    cmdAudit.Parameters.AddWithValue("@u", usuarioFormateado);
                                    cmdAudit.ExecuteNonQuery();
                                }

                                // 1. Guardamos los datos en la memoria temporal (Session)
                                Session.IdUsuario = idUsuario;
                                Session.UsuarioActual = usuarioFormateado;
                                Session.RolActual = rol;
                                Session.SucursalAsignada = sucursal;

                                // 2. EL BLINDAJE: Forzamos la variable global a su sucursal asignada (Si aún la usas)
                                ConexionGlobal.SucursalActual = sucursal;

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
                MessageBox.Show("Error al enlazar con la base de datos central: " + ex.Message, "Fallo de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}