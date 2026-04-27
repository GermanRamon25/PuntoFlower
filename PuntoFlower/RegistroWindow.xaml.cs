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
        public RegistroWindow() => InitializeComponent();

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtNewUser.Text) || string.IsNullOrEmpty(txtNewPass.Password)) return;

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "INSERT INTO Usuarios (Username, PasswordHash) VALUES (@u, @p)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@u", txtNewUser.Text);
                    cmd.Parameters.AddWithValue("@p", txtNewPass.Password);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Usuario registrado con éxito.");
                    this.Close();
                }
            }
            catch (System.Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}