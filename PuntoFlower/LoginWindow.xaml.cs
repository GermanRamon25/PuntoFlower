using PuntoFlower.Data;
using System;
using System.Data.SqlClient;
using System.Windows;

namespace PuntoFlower
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

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
                    // MODIFICACIÓN: Seleccionamos también el Rol
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
                                // GUARDAMOS LA SESIÓN
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
                MessageBox.Show("Error al conectar: " + ex.Message);
            }
        }

        private void BtnAbrirRegistro_Click(object sender, RoutedEventArgs e)
        {
            RegistroWindow registro = new RegistroWindow();
            registro.ShowDialog();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}