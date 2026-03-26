using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class ConfigurationView : UserControl
    {
        public ConfigurationView()
        {
            InitializeComponent();
            CargarPreciosActuales();
        }

        private void CargarPreciosActuales()
        {
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "SELECT Capacidad, Precio FROM PreciosRamos";
                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int cap = (int)r["Capacidad"];
                            string p = Convert.ToDecimal(r["Precio"]).ToString("N2");
                            if (cap == 6) txtPrecio6.Text = p;
                            else if (cap == 12) txtPrecio12.Text = p;
                            else if (cap == 24) txtPrecio24.Text = p;
                            else if (cap == 50) txtPrecio50.Text = p;
                        }
                    }
                }
            }
            catch { /* Tabla no creada aún */ }
        }

        // ESTE MÉTODO DEBE EXISTIR EXACTAMENTE ASÍ
        private void btnAplicarAjustes_Click(object sender, RoutedEventArgs e)
        {
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    GuardarPrecio(con, 6, txtPrecio6.Text);
                    GuardarPrecio(con, 12, txtPrecio12.Text);
                    GuardarPrecio(con, 24, txtPrecio24.Text);
                    GuardarPrecio(con, 50, txtPrecio50.Text);
                }
                MessageBox.Show("Precios actualizados correctamente.");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void GuardarPrecio(SqlConnection con, int cap, string precioTxt)
        {
            if (decimal.TryParse(precioTxt, out decimal p))
            {
                string sql = "IF EXISTS(SELECT 1 FROM PreciosRamos WHERE Capacidad=@c) " +
                             "UPDATE PreciosRamos SET Precio=@p WHERE Capacidad=@c " +
                             "ELSE INSERT INTO PreciosRamos VALUES(@c, @p)";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@c", cap);
                cmd.Parameters.AddWithValue("@p", p);
                cmd.ExecuteNonQuery();
            }
        }
    }
}