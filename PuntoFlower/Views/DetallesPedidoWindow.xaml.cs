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

        // NUEVO: Método para poner el pedido en preparación (Naranja en Agenda)
        private void btnPreparar_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("En Preparación");
            MessageBox.Show("El pedido ahora aparece 'En Preparación'.", "Estatus Actualizado");
            this.DialogResult = true;
        }

        // NUEVO: Método para poner el pedido como listo (Verde en Agenda)
        private void btnListo_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Listo para Entregar");
            MessageBox.Show("El pedido ahora aparece como 'Listo'.", "Estatus Actualizado");
            this.DialogResult = true;
        }

        // Método genérico para ahorrar código al actualizar estados
        private void ActualizarEstado(string nuevoEstado)
        {
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "UPDATE Pedidos SET Estado = @estado WHERE Id = @id";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@estado", nuevoEstado);
                    cmd.Parameters.AddWithValue("@id", _pedidoId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar estado: " + ex.Message);
            }
        }

        private void btnEntregar_Click(object sender, RoutedEventArgs e)
        {
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // Al entregar, el estado cambia y el saldo pendiente se vuelve 0 (Liquidado)
                    string query = "UPDATE Pedidos SET Estado = 'Entregado', SaldoPendiente = 0 WHERE Id = @id";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@id", _pedidoId);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Pedido entregado. El saldo ha sido liquidado en el sistema.", "Venta Finalizada");
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar entrega: " + ex.Message);
            }
        }
    }
}