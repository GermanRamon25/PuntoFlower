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

            // CAPTURA AUTOMÁTICA DE LA HORA DE RESERVA
            DateTime fechaRegistro = DateTime.Now;

            decimal saldoPendiente = total - anticipo;

            // Obtener el método de pago del anticipo de forma segura
            var itemMetodo = cbMetodoAnticipo.SelectedItem as ComboBoxItem;
            string metodoAnticipo = itemMetodo != null ? itemMetodo.Content.ToString() : "Efectivo";

            ConexionDB db = new ConexionDB();

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // MODIFICADO: Se inyecta @metodo (MetodoPago) para controlar la entrada financiera del anticipo
                    string query = @"INSERT INTO Pedidos (ClienteNombre, Telefono, FechaEntrega, FechaRegistro, Direccion, NotaTarjeta, Estado, Descripcion, PrecioTotal, Anticipo, SaldoPendiente, MetodoPago) 
                                   VALUES (@nom, @tel, @fec, @fecReg, @dir, @not, 'Pendiente', @des, @total, @ant, @saldo, @metodo)";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@nom", txtCliente.Text);
                    cmd.Parameters.AddWithValue("@tel", txtTelefono.Text);
                    cmd.Parameters.AddWithValue("@fec", dpFecha.SelectedDate.Value); // Solo fecha elegida para entrega
                    cmd.Parameters.AddWithValue("@fecReg", fechaRegistro);           // Sello de tiempo automático (RESERVA)
                    cmd.Parameters.AddWithValue("@dir", txtDireccion.Text);
                    cmd.Parameters.AddWithValue("@not", txtNota.Text);
                    cmd.Parameters.AddWithValue("@des", txtDescripcion.Text);
                    cmd.Parameters.AddWithValue("@total", total);
                    cmd.Parameters.AddWithValue("@ant", anticipo);
                    cmd.Parameters.AddWithValue("@saldo", saldoPendiente);
                    cmd.Parameters.AddWithValue("@metodo", metodoAnticipo); // Almacena el método inicial

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