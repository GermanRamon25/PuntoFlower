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
            VerificarAlertaMensualidad(); // Evaluamos la alerta de licencia
            CargarEstadisticasReales();
        }

        // ========================================================
        // NUEVO: LÓGICA DE ALERTA DE MENSUALIDAD (DÍA 1 AL 6)
        // ========================================================
        private void VerificarAlertaMensualidad()
        {
            int diaActual = DateTime.Now.Day;

            // Evaluamos si estamos en los primeros 6 días del mes
            if (diaActual >= 1 && diaActual <= 6)
            {
                BannerMensualidad.Visibility = Visibility.Visible;

                if (diaActual == 6)
                {
                    // El mero día del pago, lo ponemos en Rojo Peligro
                    BannerMensualidad.Background = System.Windows.Media.Brushes.DarkRed;
                    txtMensajeMensualidad.Text = "⚠️ AVISO IMPORTANTE: Hoy es día 6, corte mensual del sistema. Favor de contemplar el pago de la licencia.";
                }
                else
                {
                    // Días previos (del 1 al 5), lo mantenemos en Naranja Preventivo
                    int diasFaltantes = 6 - diaActual;
                    string textoDias = diasFaltantes == 1 ? "1 día" : $"{diasFaltantes} días";

                    BannerMensualidad.Background = System.Windows.Media.Brushes.DarkOrange;
                    txtMensajeMensualidad.Text = $"⏳ Recordatorio: Faltan {textoDias} para el corte mensual del sistema (Día 6).";
                }
            }
            else
            {
                // Del día 7 al 31, el banner se esconde
                BannerMensualidad.Visibility = Visibility.Collapsed;
            }
        }
        // ========================================================

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

                    // 2. INVERSIÓN EN STOCK Y GASTOS GENERALES
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