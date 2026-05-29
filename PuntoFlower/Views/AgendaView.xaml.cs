using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class AgendaView : UserControl
    {
        private string filtroActual = "TODOS";

        public AgendaView()
        {
            InitializeComponent();
            CargarPedidos("TODOS");
        }

        private void CargarPedidos(string modoFiltro)
        {
            filtroActual = modoFiltro;
            List<object> listaPedidos = new List<object>();
            ConexionDB db = new ConexionDB();

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // Construcción dinámica de la query basada en la pestaña activa seleccionada
                    string condicion = "WHERE Estado != 'Entregado'";

                    if (modoFiltro == "HOY")
                    {
                        condicion = "WHERE Estado != 'Entregado' AND CAST(FechaEntrega AS DATE) = CAST(GETDATE() AS DATE)";
                    }
                    else if (modoFiltro == "CERRADOS")
                    {
                        condicion = "WHERE Estado = 'Entregado'";
                    }

                    string query = $@"SELECT Id, ClienteNombre, Telefono, FechaEntrega, FechaRegistro, Descripcion, Direccion, 
                                    NotaTarjeta, Estado, PrecioTotal, Anticipo, SaldoPendiente 
                                    FROM Pedidos 
                                    {condicion} 
                                    ORDER BY FechaEntrega ASC";

                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string horaReserva = r["FechaRegistro"] != DBNull.Value
                                ? Convert.ToDateTime(r["FechaRegistro"]).ToString("h:mm tt")
                                : "12:00 AM";

                            listaPedidos.Add(new
                            {
                                Id = r["Id"],
                                ClienteNombre = r["ClienteNombre"].ToString(),
                                Telefono = r["Telefono"].ToString(),
                                FechaEntrega = Convert.ToDateTime(r["FechaEntrega"]),
                                HoraRegistro = horaReserva,
                                Descripcion = r["Descripcion"].ToString(),
                                Direccion = string.IsNullOrEmpty(r["Direccion"].ToString()) ? "Recoge en Tienda" : r["Direccion"].ToString(),
                                NotaTarjeta = r["NotaTarjeta"].ToString(),
                                Estado = r["Estado"].ToString(),
                                PrecioTotal = r["PrecioTotal"] != DBNull.Value ? Convert.ToDecimal(r["PrecioTotal"]) : 0,
                                Anticipo = r["Anticipo"] != DBNull.Value ? Convert.ToDecimal(r["Anticipo"]) : 0,
                                SaldoPendiente = r["SaldoPendiente"] != DBNull.Value ? Convert.ToDecimal(r["SaldoPendiente"]) : 0
                            });
                        }
                    }
                }

                icPedidos.ItemsSource = null;
                icPedidos.ItemsSource = listaPedidos;

                // Actualizar resaltado visual de botones corporativos
                RegularizarEstiloBotones(modoFiltro);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la agenda de pedidos: " + ex.Message);
            }
        }

        private void RegularizarEstiloBotones(string activo)
        {
            // Reseteo base de colores
            btnFiltrarTodos.Background = Brushes.White; btnFiltrarTodos.Foreground = Brushes.Black;
            btnFiltrarHoy.Background = Brushes.White; btnFiltrarHoy.Foreground = Brushes.Black;
            btnFiltrarCerrados.Background = Brushes.White; btnFiltrarCerrados.Foreground = Brushes.Black;

            var azulWPF = (Brush)new BrushConverter().ConvertFromString("#3498DB");

            // Resaltamos el botón activo actual
            if (activo == "TODOS") { btnFiltrarTodos.Background = azulWPF; btnFiltrarTodos.Foreground = Brushes.White; }
            else if (activo == "HOY") { btnFiltrarHoy.Background = azulWPF; btnFiltrarHoy.Foreground = Brushes.White; }
            else if (activo == "CERRADOS") { btnFiltrarCerrados.Background = azulWPF; btnFiltrarCerrados.Foreground = Brushes.White; }
        }

        private void btnFiltrarTodos_Click(object sender, RoutedEventArgs e)
        {
            CargarPedidos("TODOS");
        }

        private void btnFiltrarHoy_Click(object sender, RoutedEventArgs e)
        {
            CargarPedidos("HOY");
        }

        private void btnFiltrarCerrados_Click(object sender, RoutedEventArgs e)
        {
            CargarPedidos("CERRADOS");
        }

        private void btnNuevoPedido_Click(object sender, RoutedEventArgs e)
        {
            NuevoPedidoWindow ventana = new NuevoPedidoWindow();
            ventana.Owner = Window.GetWindow(this);
            if (ventana.ShowDialog() == true)
            {
                CargarPedidos(filtroActual);
            }
        }

        private void btnVerDetalles_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            if (boton == null) return;
            var pedidoSeleccionado = boton.DataContext;

            if (pedidoSeleccionado != null)
            {
                DetallesPedidoWindow ventana = new DetallesPedidoWindow(pedidoSeleccionado);
                ventana.Owner = Window.GetWindow(this);

                if (ventana.ShowDialog() == true)
                {
                    CargarPedidos(filtroActual);
                }
            }
        }
    }
}