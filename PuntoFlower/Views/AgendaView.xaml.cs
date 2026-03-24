using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class AgendaView : UserControl
    {
        public AgendaView()
        {
            InitializeComponent();
            CargarPedidos();
        }

        private void CargarPedidos()
        {
            List<object> listaPedidos = new List<object>();
            ConexionDB db = new ConexionDB();

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // CAMBIO: Se traen explícitamente los campos financieros para que el binding en el XAML funcione
                    string query = "SELECT Id, ClienteNombre, Telefono, FechaEntrega, Descripcion, Direccion, NotaTarjeta, Estado, PrecioTotal, Anticipo, SaldoPendiente " +
                                   "FROM Pedidos WHERE Estado != 'Entregado' ORDER BY FechaEntrega ASC";

                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            listaPedidos.Add(new
                            {
                                Id = r["Id"],
                                ClienteNombre = r["ClienteNombre"].ToString(),
                                Telefono = r["Telefono"].ToString(),
                                FechaEntrega = Convert.ToDateTime(r["FechaEntrega"]),
                                Descripcion = r["Descripcion"].ToString(),
                                Direccion = string.IsNullOrEmpty(r["Direccion"].ToString()) ? "Recoge en Tienda" : r["Direccion"].ToString(),
                                NotaTarjeta = r["NotaTarjeta"].ToString(),
                                Estado = r["Estado"].ToString(),
                                // NUEVOS CAMPOS PARA LA VISTA FINANCIERA
                                PrecioTotal = r["PrecioTotal"] != DBNull.Value ? Convert.ToDecimal(r["PrecioTotal"]) : 0,
                                Anticipo = r["Anticipo"] != DBNull.Value ? Convert.ToDecimal(r["Anticipo"]) : 0,
                                SaldoPendiente = r["SaldoPendiente"] != DBNull.Value ? Convert.ToDecimal(r["SaldoPendiente"]) : 0
                            });
                        }
                    }
                }
                icPedidos.ItemsSource = null;
                icPedidos.ItemsSource = listaPedidos;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la agenda: " + ex.Message);
            }
        }

        private void btnNuevoPedido_Click(object sender, RoutedEventArgs e)
        {
            NuevoPedidoWindow ventana = new NuevoPedidoWindow();
            ventana.Owner = Window.GetWindow(this);
            if (ventana.ShowDialog() == true)
            {
                CargarPedidos();
            }
        }

        private void btnVerDetalles_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            var pedidoSeleccionado = boton.DataContext; // Obtenemos el objeto con todos los datos (incluyendo anticipo y saldo)

            if (pedidoSeleccionado != null)
            {
                // Pasamos el objeto 'dynamic' que ya contiene PrecioTotal, Anticipo y SaldoPendiente
                DetallesPedidoWindow ventana = new DetallesPedidoWindow(pedidoSeleccionado);
                ventana.Owner = Window.GetWindow(this);

                if (ventana.ShowDialog() == true)
                {
                    CargarPedidos();
                }
            }
        }
    }
}