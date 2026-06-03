using PuntoFlower.Data;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace PuntoFlower.Views
{
    public partial class NuevoPedidoWindow : Window
    {
        public NuevoPedidoWindow()
        {
            InitializeComponent();
        }

        // Lógica matemática: Suma (Ramo + Envío) y resta el Anticipo en tiempo real
        private void CalcularSaldo(object sender, TextChangedEventArgs e)
        {
            if (lblSaldo == null || txtPrecioTotal == null || txtCostoEnvio == null || txtAnticipo == null) return;

            decimal ramo = 0;
            decimal envio = 0;
            decimal anticipo = 0;

            decimal.TryParse(txtPrecioTotal.Text.Trim(), out ramo);
            decimal.TryParse(txtCostoEnvio.Text.Trim(), out envio);
            decimal.TryParse(txtAnticipo.Text.Trim(), out anticipo);

            // El precio total real del pedido incluye el ramo y las afueras/centro de la ciudad
            decimal precioTotalReal = ramo + envio;
            decimal saldo = precioTotalReal - anticipo;

            lblSaldo.Text = saldo.ToString("C");
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validación básica institucional
            if (string.IsNullOrEmpty(txtCliente.Text) || dpFecha.SelectedDate == null)
            {
                MessageBox.Show("Por favor, ingresa al menos el nombre del cliente y la fecha de entrega.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar montos numéricos fijos
            if (!decimal.TryParse(txtPrecioTotal.Text.Trim(), out decimal ramo) ||
                !decimal.TryParse(txtCostoEnvio.Text.Trim(), out decimal envio) ||
                !decimal.TryParse(txtAnticipo.Text.Trim(), out decimal anticipo))
            {
                MessageBox.Show("Por favor, ingresa montos numéricos válidos en los campos financieros.", "Error de datos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime fechaRegistro = DateTime.Now;

            // Lógica de cálculo final para base de datos
            decimal precioTotalFinal = ramo + envio;
            decimal saldoPendiente = precioTotalFinal - anticipo;

            var itemMetodo = cbMetodoAnticipo.SelectedItem as ComboBoxItem;
            string metodoAnticipo = itemMetodo != null ? itemMetodo.Content.ToString() : "Efectivo";

            ConexionDB db = new ConexionDB();

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // MODIFICADO: Se añade la columna CostoEnvio al INSERT parametrizado
                    string query = @"INSERT INTO Pedidos (ClienteNombre, Telefono, FechaEntrega, FechaRegistro, Direccion, NotaTarjeta, Estado, Descripcion, PrecioTotal, Anticipo, SaldoPendiente, MetodoPago, CostoEnvio) 
                                   VALUES (@nom, @tel, @fec, @fecReg, @dir, @not, 'Pendiente', @des, @total, @ant, @saldo, @metodo, @envio)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@nom", txtCliente.Text.Trim());
                        cmd.Parameters.AddWithValue("@tel", txtTelefono.Text.Trim());
                        cmd.Parameters.AddWithValue("@fec", dpFecha.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@fecReg", fechaRegistro);
                        cmd.Parameters.AddWithValue("@dir", string.IsNullOrWhiteSpace(txtDireccion.Text) ? "Recoge en Tienda" : txtDireccion.Text.Trim());
                        cmd.Parameters.AddWithValue("@not", txtNota.Text.Trim());
                        cmd.Parameters.AddWithValue("@des", txtDescripcion.Text.Trim());
                        cmd.Parameters.AddWithValue("@total", precioTotalFinal); // El total ya lleva el envío sumado
                        cmd.Parameters.AddWithValue("@ant", anticipo);
                        cmd.Parameters.AddWithValue("@saldo", saldoPendiente);
                        cmd.Parameters.AddWithValue("@metodo", metodoAnticipo);
                        cmd.Parameters.AddWithValue("@envio", envio); // Guardamos de forma limpia el costo de envío separado

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("¡Pedido y costo de envío agendados con éxito!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar el pedido en la base de datos: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}