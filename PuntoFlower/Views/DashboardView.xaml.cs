using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            CargarEstadisticasReales();
        }

        private void CargarEstadisticasReales()
        {
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // 1. VENTAS DEL MES
                    string qVentas = @"SELECT ISNULL(SUM(Total), 0) FROM Ventas 
                                     WHERE MONTH(Fecha) = MONTH(GETDATE()) 
                                     AND YEAR(Fecha) = YEAR(GETDATE())";
                    SqlCommand cmdVentas = new SqlCommand(qVentas, con);
                    decimal totalVentas = Convert.ToDecimal(cmdVentas.ExecuteScalar());
                    txtVentasMes.Text = totalVentas.ToString("C");

                    // 2. INVERSIÓN EN STOCK
                    string qCompras = @"SELECT ISNULL(SUM(Cantidad * PrecioCosto), 0) FROM DetalleCompras 
                                      WHERE MONTH(Fecha) = MONTH(GETDATE())";
                    SqlCommand cmdCompras = new SqlCommand(qCompras, con);
                    decimal totalCompras = Convert.ToDecimal(cmdCompras.ExecuteScalar());
                    txtGastosSurtido.Text = totalCompras.ToString("C");

                    // 3. STOCK CRÍTICO
                    string qStock = "SELECT COUNT(*) FROM Productos WHERE StockActual <= StockMinimo";
                    SqlCommand cmdStock = new SqlCommand(qStock, con);
                    int alertas = (int)cmdStock.ExecuteScalar();
                    txtStockAlerta.Text = $"{alertas} Flores";

                    // 4. PEDIDOS PARA HOY
                    string qPedidos = "SELECT COUNT(*) FROM Pedidos WHERE CAST(FechaEntrega AS DATE) = CAST(GETDATE() AS DATE)";
                    SqlCommand cmdPedidos = new SqlCommand(qPedidos, con);
                    int pedidosHoy = (int)cmdPedidos.ExecuteScalar();
                    txtPedidosHoy.Text = pedidosHoy.ToString();

                    if (pedidosHoy > 0) lblMensajeAgenda.Text = $"Tienes {pedidosHoy} entregas para hoy.";
                }
            }
            catch (Exception ex)
            {
                // Solo muestra error si hay algo grave en la conexión
                Console.WriteLine("Error Dashboard: " + ex.Message);
            }
        }
    }
}