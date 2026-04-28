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

        // Evento para el botón de Iniciar Sesión
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
                    // Buscamos el usuario, contraseña y su estado de activación
                    string query = "SELECT Estado FROM Usuarios WHERE Username = @u AND PasswordHash = @p";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@u", txtUsername.Text);
                    cmd.Parameters.AddWithValue("@p", txtPassword.Password);

                    object resultado = cmd.ExecuteScalar();

                    if (resultado != null)
                    {
                        string estado = resultado.ToString();
                        if (estado == "Activo")
                        {
                            // Login exitoso
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
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar: " + ex.Message);
            }
        }

        // Evento para abrir la ventana de registro
        private void BtnAbrirRegistro_Click(object sender, RoutedEventArgs e)
        {
            RegistroWindow registro = new RegistroWindow();
            registro.ShowDialog();
        }

        // Evento para cerrar la ventana
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}