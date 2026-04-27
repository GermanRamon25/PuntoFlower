using PuntoFlower.Data;
using System;
using System.Data.SqlClient;
using System.Windows;
using PuntoFlower.Views; // Asegúrate de importar el namespace donde está RegistroWindow

namespace PuntoFlower
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // Método para cerrar la ventana (vinculado al botón "X")
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Password))
            {
                MessageBox.Show("Por favor, ingresa usuario y contraseña.");
                return;
            }

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // Consulta para verificar usuario
                    string query = "SELECT COUNT(*) FROM Usuarios WHERE Username = @user AND PasswordHash = @pass";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@user", txtUsername.Text);
                    cmd.Parameters.AddWithValue("@pass", txtPassword.Password);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count > 0)
                    {
                        MainWindow main = new MainWindow();
                        main.Show();
                        this.Close(); // Cierra el login y abre el sistema
                    }
                    else
                    {
                        MessageBox.Show("Usuario o contraseña incorrectos.", "Error de Acceso", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar con la base de datos: " + ex.Message);
            }
        }

        // Método para abrir la ventana de registro profesional
        private void BtnAbrirRegistro_Click(object sender, RoutedEventArgs e)
        {
            // Instanciamos tu ventana RegistroWindow que ya tienes creada
            RegistroWindow registro = new RegistroWindow();

            // La mostramos como ventana modal
            registro.ShowDialog();
        }
    }
}