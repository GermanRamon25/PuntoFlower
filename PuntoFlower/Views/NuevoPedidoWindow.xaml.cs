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

        // Lógica para calcular el saldo pendiente mientras se escribe
        private void CalcularSaldo(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(txtPrecioTotal.Text, out decimal total) &&
                decimal.TryParse(txtAnticipo.Text, out decimal anticipo))
            {
                decimal saldo = total - anticipo;
                lblSaldo.Text = saldo.ToString("C");
            }
            else
            {
                lblSaldo.Text = "$0.00";
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validación básica
            if (string.IsNullOrEmpty(txtCliente.Text) || dpFecha.SelectedDate == null)
            {
                MessageBox.Show("Por favor, ingresa al menos el nombre del cliente y la fecha de entrega.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar montos numéricos
            if (!decimal.TryParse(txtPrecioTotal.Text, out decimal total) || !decimal.TryParse(txtAnticipo.Text, out decimal anticipo))
            {
                MessageBox.Show("Por favor, ingresa montos válidos en los campos de dinero.", "Error de datos");
                return;
            }

            decimal saldoPendiente = total - anticipo;
            ConexionDB db = new ConexionDB();

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // Consulta actualizada con campos financieros
                    string query = "INSERT INTO Pedidos (ClienteNombre, Telefono, FechaEntrega, Direccion, NotaTarjeta, Estado, Descripcion, PrecioTotal, Anticipo, SaldoPendiente) " +
                                   "VALUES (@nom, @tel, @fec, @dir, @not, 'Pendiente', @des, @total, @ant, @saldo)";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@nom", txtCliente.Text);
                    cmd.Parameters.AddWithValue("@tel", txtTelefono.Text);
                    cmd.Parameters.AddWithValue("@fec", dpFecha.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@dir", txtDireccion.Text);
                    cmd.Parameters.AddWithValue("@not", txtNota.Text);
                    cmd.Parameters.AddWithValue("@des", txtDescripcion.Text);
                    cmd.Parameters.AddWithValue("@total", total);
                    cmd.Parameters.AddWithValue("@ant", anticipo);
                    cmd.Parameters.AddWithValue("@saldo", saldoPendiente);

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Pedido guardado con éxito en la agenda.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar en la base de datos: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}