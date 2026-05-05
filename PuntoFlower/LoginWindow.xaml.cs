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
            // Validar que los campos no estén vacíos
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
                    // Seleccionamos Estado y Rol para validar acceso y permisos
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

                            // Verificamos si el administrador ya activó la cuenta
                            if (estado == "Activo")
                            {
                                // --- GUARDAMOS LOS DATOS EN LA CLASE GLOBAL DE SESIÓN ---
                                // Esto permite que MainWindow aplique las restricciones
                                Session.UsuarioActual = txtUsername.Text;
                                Session.RolActual = rol;

                                // Abrir la ventana principal
                                MainWindow main = new MainWindow();
                                main.Show();
                                this.Close();
                            }
                            else
                            {
                                // Bloqueo de seguridad para usuarios 'Pendientes'[cite: 2]
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
            Application.Current.Shutdown(); // Cierra toda la aplicación
        }
    }
}