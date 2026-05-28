using System;
using System.Data.SqlClient;
using System.Windows;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class DetallesPedidoWindow : Window
    {
        private int _pedidoId;
        private decimal _saldoPendienteNum;

        public DetallesPedidoWindow(dynamic pedido)
        {
            InitializeComponent();
            _pedidoId = pedido.Id;
            _saldoPendienteNum = pedido.SaldoPendiente;

            lblCliente.Text = pedido.ClienteNombre;
            lblFecha.Text = "Entrega: " + pedido.FechaEntrega.ToString("dd/MM/yyyy HH:mm");
            lblDescripcion.Text = pedido.Descripcion;
            lblDireccion.Text = pedido.Direccion;
            txtNota.Text = pedido.NotaTarjeta;

            // Mostrar montos financieros
            lblTotal.Text = string.Format("{0:C}", pedido.PrecioTotal);
            lblAnticipo.Text = string.Format("{0:C}", pedido.Anticipo);
            lblSaldo.Text = string.Format("{0:C}", _saldoPendienteNum);

            // NUEVO: Ocultamos el contenedor del método de pago de liquidación en esta ventana 
            // ya que el cobro financiero y la salida de stock se realizarán directamente desde el Punto de Venta (SalesView)
            if (panelMetodoLiquidacion != null)
            {
                panelMetodoLiquidacion.Visibility = Visibility.Collapsed;
            }
        }

        // Método para poner el pedido en preparación (Naranja en Agenda)
        private void btnPreparar_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("En Preparación");
            MessageBox.Show("El pedido ahora aparece 'En Preparación'.", "Estatus Actualizado");
            this.DialogResult = true;
        }

        // Método para poner el pedido como listo (Verde en Agenda)
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

        // CORREGIDO: Procesa la entrega física de la agenda, elimina la inyección redundante de dinero
        // para cederle el control absoluto al Punto de Venta tradicional.
        private void btnEntregar_Click(object sender, RoutedEventArgs e)
        {
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // Al entregar desde la agenda, el estatus cambia y el saldo pendiente se vuelve 0 para cerrar el expediente del cliente
                    string query = "UPDATE Pedidos SET Estado = 'Entregado', SaldoPendiente = 0, MetodoPagoLiquidacion = 'Mostrador POS' WHERE Id = @id";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", _pedidoId);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Pedido marcado como 'Entregado' en la agenda.\n\nRecuerda registrar los insumos de este arreglo en el Punto de Venta para procesar el cobro del saldo restante y descontar el inventario de flores.", "Agenda Actualizada", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar la entrega en la agenda: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}