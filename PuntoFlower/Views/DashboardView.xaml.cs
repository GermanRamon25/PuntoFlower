using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            List<object> entregasDeHoy = new List<object>();

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

                    // 4. CARGAR LISTA DE ENTREGAS PARA HOY
                    string qPedidosHoy = @"SELECT ClienteNombre, Descripcion, FechaEntrega, Estado 
                                         FROM Pedidos 
                                         WHERE CAST(FechaEntrega AS DATE) = CAST(GETDATE() AS DATE) 
                                         AND Estado != 'Entregado'
                                         ORDER BY FechaEntrega ASC";

                    SqlCommand cmdPedidos = new SqlCommand(qPedidosHoy, con);
                    using (SqlDataReader r = cmdPedidos.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string estado = r["Estado"].ToString();

                            // Asignamos el color dinámicamente según el estado para que se vea en el banner
                            string colorHex = "#3498DB"; // Azul por defecto (Pendiente)
                            if (estado == "En Preparación") colorHex = "#F39C12"; // Naranja
                            else if (estado == "Listo para Entregar") colorHex = "#27AE60"; // Verde

                            entregasDeHoy.Add(new
                            {
                                ClienteNombre = r["ClienteNombre"].ToString(),
                                Descripcion = r["Descripcion"].ToString(),
                                FechaEntrega = Convert.ToDateTime(r["FechaEntrega"]),
                                Estado = estado,
                                ColorEstado = colorHex
                            });
                        }
                    }
                }

                // Actualizar interfaz
                txtPedidosHoy.Text = entregasDeHoy.Count.ToString();
                lblContadorHoy.Text = $"{entregasDeHoy.Count} pendientes";
                icEntregasHoy.ItemsSource = entregasDeHoy;

                // Controlar visibilidad del mensaje de "No hay entregas"
                if (entregasDeHoy.Count == 0)
                {
                    lblMensajeAgenda.Visibility = Visibility.Visible;
                    icEntregasHoy.Visibility = Visibility.Collapsed;
                }
                else
                {
                    lblMensajeAgenda.Visibility = Visibility.Collapsed;
                    icEntregasHoy.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Dashboard: " + ex.Message);
            }
        }
    }
}