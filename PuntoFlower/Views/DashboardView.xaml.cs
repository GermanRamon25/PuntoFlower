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
            VerificarAlertaMensualidad();
            CargarEstadisticasReales();
        }

        private void VerificarAlertaMensualidad()
        {
            int diaActual = DateTime.Now.Day;
            if (diaActual >= 1 && diaActual <= 6)
            {
                BannerMensualidad.Visibility = Visibility.Visible;
                if (diaActual == 6)
                {
                    BannerMensualidad.Background = System.Windows.Media.Brushes.DarkRed;
                    txtMensajeMensualidad.Text = "⚠️ AVISO IMPORTANTE: Hoy es día 6, corte mensual del sistema. Favor de contemplar el pago de la licencia.";
                }
                else
                {
                    int diasFaltantes = 6 - diaActual;
                    string textoDias = diasFaltantes == 1 ? "1 día" : $"{diasFaltantes} días";
                    BannerMensualidad.Background = System.Windows.Media.Brushes.DarkOrange;
                    txtMensajeMensualidad.Text = $"⏳ Recordatorio: Faltan {textoDias} para el corte mensual del sistema (Día 6).";
                }
            }
            else
            {
                BannerMensualidad.Visibility = Visibility.Collapsed;
            }
        }

        private void CargarEstadisticasReales()
        {
            ConexionDB db = new ConexionDB();
            List<object> entregasDeHoy = new List<object>();

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // 1. VENTAS DEL MES (AISLADO POR SUCURSAL)
                    string qVentas = @"SELECT ISNULL(SUM(Total), 0) FROM Ventas 
                                     WHERE MONTH(Fecha) = MONTH(GETDATE()) 
                                     AND YEAR(Fecha) = YEAR(GETDATE())
                                     AND Sucursal = @s";
                    SqlCommand cmdVentas = new SqlCommand(qVentas, con);
                    cmdVentas.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                    decimal totalVentas = Convert.ToDecimal(cmdVentas.ExecuteScalar());
                    txtVentasMes.Text = totalVentas.ToString("C");

                    // 2. INVERSIÓN EN STOCK Y GASTOS GENERALES (AISLADO POR SUCURSAL)
                    string qComprasYGastos = @"
                        SELECT 
                            (SELECT ISNULL(SUM(Cantidad * PrecioCosto), 0) FROM DetalleCompras 
                             WHERE MONTH(Fecha) = MONTH(GETDATE()) AND YEAR(Fecha) = YEAR(GETDATE()) AND Sucursal = @s)
                            +
                            (SELECT ISNULL(SUM(Monto), 0) FROM Gastos 
                             WHERE MONTH(Fecha) = MONTH(GETDATE()) AND YEAR(Fecha) = YEAR(GETDATE()) AND Sucursal = @s)";

                    SqlCommand cmdCompras = new SqlCommand(qComprasYGastos, con);
                    cmdCompras.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                    decimal totalComprasYGastos = Convert.ToDecimal(cmdCompras.ExecuteScalar());
                    txtGastosSurtido.Text = totalComprasYGastos.ToString("C");

                    // 3. STOCK CRÍTICO (AISLADO POR SUCURSAL)
                    string qStock = "SELECT COUNT(*) FROM Productos WHERE StockActual <= StockMinimo AND Sucursal = @s";
                    SqlCommand cmdStock = new SqlCommand(qStock, con);
                    cmdStock.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                    int alertas = (int)cmdStock.ExecuteScalar();
                    txtStockAlerta.Text = $"{alertas} Flores";

                    // 4. CARGAR LISTA DE ENTREGAS PARA HOY (AISLADO POR SUCURSAL)
                    string qPedidosHoy = @"SELECT ClienteNombre, Descripcion, FechaEntrega, FechaRegistro, Estado 
                                         FROM Pedidos 
                                         WHERE CAST(FechaEntrega AS DATE) = CAST(GETDATE() AS DATE) 
                                         AND Estado != 'Entregado'
                                         AND Sucursal = @s
                                         ORDER BY FechaEntrega ASC";

                    SqlCommand cmdPedidos = new SqlCommand(qPedidosHoy, con);
                    cmdPedidos.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);

                    using (SqlDataReader r = cmdPedidos.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string estado = r["Estado"].ToString();
                            DateTime horaReal = r["FechaRegistro"] != DBNull.Value
                                ? Convert.ToDateTime(r["FechaRegistro"])
                                : Convert.ToDateTime(r["FechaEntrega"]);

                            string colorHex = "#3498DB";
                            if (estado == "En Preparación") colorHex = "#F39C12";
                            else if (estado == "Listo para Entregar") colorHex = "#27AE60";

                            entregasDeHoy.Add(new
                            {
                                ClienteNombre = r["ClienteNombre"].ToString(),
                                Descripcion = r["Descripcion"].ToString(),
                                FechaEntrega = horaReal.ToString("h:mm tt"),
                                Estado = estado,
                                ColorEstado = colorHex
                            });
                        }
                    }
                }

                txtPedidosHoy.Text = entregasDeHoy.Count.ToString();
                lblContadorHoy.Text = $"{entregasDeHoy.Count} pendientes para hoy";
                icEntregasHoy.ItemsSource = entregasDeHoy;

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