using PuntoFlower.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PuntoFlower
{
    public partial class RegistroWindow : Window
    {
        public RegistroWindow()
        {
            InitializeComponent();
        }

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
                    string query = "INSERT INTO Usuarios (Username, PasswordHash, Estado) VALUES (@u, @p, @est)";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@u", txtNewUser.Text);
                    cmd.Parameters.AddWithValue("@p", txtNewPass.Password);
                    cmd.Parameters.AddWithValue("@est", "Pendiente");

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Registro exitoso. Tu cuenta está pendiente de activación.");
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