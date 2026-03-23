using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class CashCloseOutView : UserControl
    {
        public CashCloseOutView()
        {
            InitializeComponent(); // Este método se genera automáticamente si el XAML está bien vinculado
            RealizarCorteDelDia();
        }

        private void RealizarCorteDelDia()
        {
            List<object> ventasHoy = new List<object>();
            decimal sumaRecibido = 0;
            decimal sumaCambio = 0;
            ConexionDB db = new ConexionDB();

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // Consulta filtrando por la fecha actual
                    string query = @"SELECT Fecha, ProductoNombre, Total, MontoRecibido, MontoCambio 
                                   FROM Ventas 
                                   WHERE CAST(Fecha AS DATE) = CAST(GETDATE() AS DATE)";

                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            // Validamos nulos en caso de que haya ventas antiguas sin estos campos
                            decimal recibido = r["MontoRecibido"] != DBNull.Value ? Convert.ToDecimal(r["MontoRecibido"]) : 0;
                            decimal cambio = r["MontoCambio"] != DBNull.Value ? Convert.ToDecimal(r["MontoCambio"]) : 0;
                            decimal totalVenta = Convert.ToDecimal(r["Total"]);

                            sumaRecibido += recibido;
                            sumaCambio += cambio;

                            ventasHoy.Add(new
                            {
                                Fecha = (DateTime)r["Fecha"],
                                ProductoNombre = r["ProductoNombre"].ToString(),
                                Total = totalVenta,
                                MontoRecibido = recibido,
                                MontoCambio = cambio
                            });
                        }
                    }
                }

                // Asignación a los controles del XAML
                dgCorte.ItemsSource = ventasHoy;
                txtTotalRecibido.Text = sumaRecibido.ToString("C");
                txtTotalCambio.Text = sumaCambio.ToString("C");

                decimal enCaja = sumaRecibido - sumaCambio;
                txtEfectivoReal.Text = enCaja.ToString("C");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al realizar el corte: " + ex.Message);
            }
        }
    }
}