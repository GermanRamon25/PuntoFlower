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
                    // 1. VENTAS DEL MES (Mantenido intacto)
                    string qVentas = @"SELECT ISNULL(SUM(Total), 0) FROM Ventas 
                                     WHERE MONTH(Fecha) = MONTH(GETDATE()) 
                                     AND YEAR(Fecha) = YEAR(GETDATE())";
                    SqlCommand cmdVentas = new SqlCommand(qVentas, con);
                    decimal totalVentas = Convert.ToDecimal(cmdVentas.ExecuteScalar());
                    txtVentasMes.Text = totalVentas.ToString("C");

                    // 2. INVERSIÓN EN STOCK Y GASTOS GENERALES (¡AQUÍ SE INYECTÓ LA MEJORA!)
                    // Sumamos el surtido de DetalleCompras del mes + todos los gastos de administración y tienda de la tabla Gastos del mes
                    string qComprasYGastos = @"
                        SELECT 
                            (SELECT ISNULL(SUM(Cantidad * PrecioCosto), 0) FROM DetalleCompras 
                             WHERE MONTH(Fecha) = MONTH(GETDATE()) AND YEAR(Fecha) = YEAR(GETDATE()))
                            +
                            (SELECT ISNULL(SUM(Monto), 0) FROM Gastos 
                             WHERE MONTH(Fecha) = MONTH(GETDATE()) AND YEAR(Fecha) = YEAR(GETDATE()))";

                    SqlCommand cmdCompras = new SqlCommand(qComprasYGastos, con);
                    decimal totalComprasYGastos = Convert.ToDecimal(cmdCompras.ExecuteScalar());
                    txtGastosSurtido.Text = totalComprasYGastos.ToString("C");

                    // 3. STOCK CRÍTICO (Mantenido intacto)
                    string qStock = "SELECT COUNT(*) FROM Productos WHERE StockActual <= StockMinimo";
                    SqlCommand cmdStock = new SqlCommand(qStock, con);
                    int alertas = (int)cmdStock.ExecuteScalar();
                    txtStockAlerta.Text = $"{alertas} Flores";

                    // 4. CARGAR LISTA DE ENTREGAS PARA HOY (Mantenido intacto)
                    string qPedidosHoy = @"SELECT ClienteNombre, Descripcion, FechaEntrega, FechaRegistro, Estado 
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

                            // Lógica para obtener la hora de reserva en formato 12h (AM/PM)
                            DateTime horaReal = r["FechaRegistro"] != DBNull.Value
                                ? Convert.ToDateTime(r["FechaRegistro"])
                                : Convert.ToDateTime(r["FechaEntrega"]);

                            // Asignamos el color dinámicamente según el estado
                            string colorHex = "#3498DB"; // Azul (Pendiente)
                            if (estado == "En Preparación") colorHex = "#F39C12"; // Naranja
                            else if (estado == "Listo para Entregar") colorHex = "#27AE60"; // Verde

                            entregasDeHoy.Add(new
                            {
                                ClienteNombre = r["ClienteNombre"].ToString(),
                                Descripcion = r["Descripcion"].ToString(),
                                // Enviamos la hora formateada como h:mm tt (Ej: 4:30 PM)
                                FechaEntrega = horaReal.ToString("h:mm tt"),
                                Estado = estado,
                                ColorEstado = colorHex
                            });
                        }
                    }
                }

                // Actualizar interfaz
                txtPedidosHoy.Text = entregasDeHoy.Count.ToString();
                lblContadorHoy.Text = $"{entregasDeHoy.Count} pendientes para hoy";
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