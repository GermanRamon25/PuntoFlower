using System;
using System.Data.SqlClient;
using System.Windows;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class DetallesPedidoWindow : Window
    {
        private int _pedidoId;

        public DetallesPedidoWindow(dynamic pedido)
        {
            InitializeComponent();
            _pedidoId = pedido.Id;
            lblCliente.Text = pedido.ClienteNombre;
            lblFecha.Text = "Entrega: " + pedido.FechaEntrega.ToString("dd/MM/yyyy HH:mm");
            lblDescripcion.Text = pedido.Descripcion;
            lblDireccion.Text = pedido.Direccion;
            txtNota.Text = pedido.NotaTarjeta; // Necesitaremos jalar esto en la consulta de la agenda
        }

        private void btnEntregar_Click(object sender, RoutedEventArgs e)
        {
            ConexionDB db = new ConexionDB();
            using (SqlConnection con = db.OpenConnection())
            {
                string query = "UPDATE Pedidos SET Estado = 'Entregado' WHERE Id = @id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", _pedidoId);
                cmd.ExecuteNonQuery();
            }
            MessageBox.Show("Pedido marcado como entregado.");
            this.DialogResult = true;
        }
    }
}