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
            txtNota.Text = pedido.NotaTarjeta;

            // Mostrar montos financieros
            lblTotal.Text = string.Format("{0:C}", pedido.PrecioTotal);
            lblAnticipo.Text = string.Format("{0:C}", pedido.Anticipo);
            lblSaldo.Text = string.Format("{0:C}", pedido.SaldoPendiente);
        }

        private void btnEntregar_Click(object sender, RoutedEventArgs e)
        {
            ConexionDB db = new ConexionDB();
            using (SqlConnection con = db.OpenConnection())
            {
                // Al entregar, el estado cambia y el saldo pendiente se vuelve 0
                string query = "UPDATE Pedidos SET Estado = 'Entregado', SaldoPendiente = 0 WHERE Id = @id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", _pedidoId);
                cmd.ExecuteNonQuery();
            }
            MessageBox.Show("Pedido entregado. El saldo ha sido liquidado en el sistema.");
            this.DialogResult = true;
        }
    }
}