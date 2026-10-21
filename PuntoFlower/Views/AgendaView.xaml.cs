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
                    string condicion = "WHERE Estado != 'Entregado'";

                    if (modoFiltro == "HOY")
                    {
                        condicion = "WHERE Estado != 'Entregado' AND CAST(FechaEntrega AS DATE) = CAST(GETDATE() AS DATE)";
                    }
                    else if (modoFiltro == "CERRADOS")
                    {
                        condicion = "WHERE Estado = 'Entregado'";
                    }

                    // Se agrega FechaEntregaReal a la consulta para cumplir con la Mejora #4
                    string query = $@"SELECT Id, ClienteNombre, Telefono, FechaEntrega, FechaRegistro, Descripcion, Direccion, 
                                    NotaTarjeta, Estado, PrecioTotal, Anticipo, SaldoPendiente, FechaEntregaReal 
                                    FROM Pedidos 
                                    {condicion} 
                                    ORDER BY FechaEntrega ASC";

                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            decimal saldo = r["SaldoPendiente"] != DBNull.Value ? Convert.ToDecimal(r["SaldoPendiente"]) : 0;
                            string estadoBD = r["Estado"].ToString();
                            string subtitulo = "";

                            // MEJORA #4: Mostrar fecha/hora exacta de entrega si ya está cerrado
                            if (estadoBD == "Entregado" && r["FechaEntregaReal"] != DBNull.Value)
                            {
                                subtitulo = "Entregado el: " + Convert.ToDateTime(r["FechaEntregaReal"]).ToString("dd/MM/yy h:mm tt");
                            }
                            else
                            {
                                subtitulo = "Agendado: " + (r["FechaRegistro"] != DBNull.Value ? Convert.ToDateTime(r["FechaRegistro"]).ToString("h:mm tt") : "12:00 AM");
                            }

                            // MEJORA #3: Reflejar estatus financiero "Liquidado" si el saldo es 0, manteniendo el estado logístico
                            string estadoVis = estadoBD;
                            if (estadoBD != "Entregado" && saldo == 0)
                            {
                                estadoVis = $"{estadoBD} (Liquidado)";
                            }

                            listaPedidos.Add(new
                            {
                                Id = r["Id"],
                                ClienteNombre = r["ClienteNombre"].ToString(),
                                Telefono = r["Telefono"].ToString(),
                                FechaEntrega = Convert.ToDateTime(r["FechaEntrega"]),
                                Descripcion = r["Descripcion"].ToString(),
                                Direccion = string.IsNullOrEmpty(r["Direccion"].ToString()) ? "Recoge en Tienda" : r["Direccion"].ToString(),
                                NotaTarjeta = r["NotaTarjeta"].ToString(),
                                PrecioTotal = r["PrecioTotal"] != DBNull.Value ? Convert.ToDecimal(r["PrecioTotal"]) : 0,
                                Anticipo = r["Anticipo"] != DBNull.Value ? Convert.ToDecimal(r["Anticipo"]) : 0,
                                SaldoPendiente = saldo,

                                // Propiedades de apoyo para la Interfaz (UI) mejorada:
                                EstadoOriginal = estadoBD,           // Para mantener los colores de los bordes de la tarjeta
                                EstadoVisual = estadoVis,            // El texto que lee la florista (Ej: "Pendiente (Liquidado)")
                                SubtituloSuperior = subtitulo,       // Hora de registro u hora de entrega
                                ColorSaldo = saldo == 0 ? "#27AE60" : "#E74C3C" // Verde si ya pagó, Rojo si debe
                            });
                        }
                    }
                }

                icPedidos.ItemsSource = null;
                icPedidos.ItemsSource = listaPedidos;

                RegularizarEstiloBotones(modoFiltro);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la agenda de pedidos: " + ex.Message);
            }
        }

        private void RegularizarEstiloBotones(string activo)
        {
            btnFiltrarTodos.Background = Brushes.White; btnFiltrarTodos.Foreground = Brushes.Black;
            btnFiltrarHoy.Background = Brushes.White; btnFiltrarHoy.Foreground = Brushes.Black;
            btnFiltrarCerrados.Background = Brushes.White; btnFiltrarCerrados.Foreground = Brushes.Black;

            var azulWPF = (Brush)new BrushConverter().ConvertFromString("#3498DB");

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

        private void btnModificarPedido_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            if (boton == null) return;

            // Extrae el objeto anónimo enlazado a la tarjeta de la Agenda
            dynamic pedidoSeleccionado = boton.DataContext;

            if (pedidoSeleccionado != null)
            {
                // Invocamos la ventana de Nuevo Pedido enviando el objeto para activar el "Modo Edición"
                NuevoPedidoWindow ventana = new NuevoPedidoWindow(pedidoSeleccionado);
                ventana.Owner = Window.GetWindow(this);

                if (ventana.ShowDialog() == true)
                {
                    // Si guardó cambios con éxito, refresca la pantalla de inmediato en el filtro que estabas viendo
                    CargarPedidos(filtroActual);
                }
            }
        }
    }
}