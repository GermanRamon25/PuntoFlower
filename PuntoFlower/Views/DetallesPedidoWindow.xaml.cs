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

            // Mostrar montos financieros tradicionales
            lblTotal.Text = string.Format("{0:C}", pedido.PrecioTotal);
            lblAnticipo.Text = string.Format("{0:C}", pedido.Anticipo);
            lblSaldo.Text = string.Format("{0:C}", _saldoPendienteNum);

            // NUEVO: Recuperar de forma dinámica el costo del envío directo del objeto agenda
            decimal envioMonto = 0;
            try
            {
                // Intentamos leer la propiedad si viene cargada dinámicamente
                envioMonto = pedido.CostoEnvio;
            }
            catch
            {
                // Consulta de seguridad complementaria en texto plano por si el objeto anónimo no trae la columna
                envioMonto = ConsultarCostoEnvioDeSeguridad(_pedidoId);
            }

            lblCostoEnvio.Text = string.Format("{0:C}", envioMonto);

            if (panelMetodoLiquidacion != null)
            {
                panelMetodoLiquidacion.Visibility = Visibility.Collapsed;
            }
        }

        // Método auxiliar para salvaguardar la lectura de datos del costo de traslado
        private decimal ConsultarCostoEnvioDeSeguridad(int idPedido)
        {
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "SELECT ISNULL(CostoEnvio, 0) FROM Pedidos WHERE Id = @id";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", idPedido);
                        return Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return 0; // Fallback por integridad
            }
        }

        private void btnPreparar_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("En Preparación");
            MessageBox.Show("El pedido ahora aparece 'En Preparación'.", "Estatus Actualizado", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
        }

        private void btnListo_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Listo para Entregar");
            MessageBox.Show("El pedido ahora aparece como 'Listo para Entregar'.", "Estatus Actualizado", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
        }

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
                MessageBox.Show("Error al actualizar el estado: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEntregar_Click(object sender, RoutedEventArgs e)
        {
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
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