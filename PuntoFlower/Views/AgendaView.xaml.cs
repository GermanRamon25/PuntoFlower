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
                    // Traemos todos los campos, incluyendo Estado para filtrar y Descripcion/Nota para detalles
                    string query = "SELECT * FROM Pedidos WHERE Estado != 'Entregado' ORDER BY FechaEntrega ASC";
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
                                Estado = r["Estado"].ToString()
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

        // NUEVO MÉTODO PARA VER DETALLES Y MARCAR ENTREGA
        private void btnVerDetalles_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            var pedidoSeleccionado = boton.DataContext; // Obtenemos el objeto vinculado a la tarjeta

            if (pedidoSeleccionado != null)
            {
               
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