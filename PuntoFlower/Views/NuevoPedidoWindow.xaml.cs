using PuntoFlower.Data;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace PuntoFlower.Views
{
    public partial class NuevoPedidoWindow : Window
    {
        // Variables de control de estado interno (Muy seguras)
        private bool esModoEdicion = false;
        private int pedidoIdEditar = 0;

        // Constructor 1: Mantiene intacta la creación normal de un nuevo pedido
        public NuevoPedidoWindow()
        {
            InitializeComponent();
        }

        // Constructor 2: Se ejecuta al dar clic en 'Modificar' desde la agenda
        public NuevoPedidoWindow(dynamic pedido) : this()
        {
            try
            {
                esModoEdicion = true;
                pedidoIdEditar = Convert.ToInt32(pedido.Id);

                // Adaptamos la visualización de la ventana de forma profesional
                this.Title = "Modificar e Igualar Pedido";
                lblTituloVentana.Text = "Editar Datos del Pedido";
                btnGuardar.Content = "APLICAR CAMBIOS";
                btnGuardar.Background = System.Windows.Media.Brushes.DarkOrange;

                // Mapeamos y pintamos los datos almacenados de forma directa en las cajas de texto
                txtCliente.Text = pedido.ClienteNombre;
                txtTelefono.Text = pedido.Telefono;
                dpFecha.SelectedDate = pedido.FechaEntrega;
                txtDescripcion.Text = pedido.Descripcion;

                // Si la dirección dice 'Recoge en Tienda', la limpiamos para comodidad al editar
                txtDireccion.Text = pedido.Direccion == "Recoge en Tienda" ? "" : pedido.Direccion;
                txtNota.Text = pedido.NotaTarjeta;

                // VARIABLES DE RESPALDO SEGURO DESDE LA BASE DE DATOS
                decimal costoEnvioOriginal = 0;
                string metodoOriginal = "Efectivo"; // Valor por defecto seguro

                ConexionDB db = new ConexionDB();
                using (SqlConnection con = db.OpenConnection())
                {
                    // Leemos de forma directa y segura tanto el Costo de Envío como el Método de Pago para evitar el RuntimeBinderException
                    string queryFija = "SELECT ISNULL(CostoEnvio, 0), ISNULL(MetodoPago, 'Efectivo') FROM Pedidos WHERE Id = @id";
                    using (SqlCommand cmd = new SqlCommand(queryFija, con))
                    {
                        cmd.Parameters.AddWithValue("@id", pedidoIdEditar);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                costoEnvioOriginal = Convert.ToDecimal(reader[0]);
                                metodoOriginal = reader[1].ToString();
                            }
                        }
                    }
                }

                // Descontamos el envío del PrecioTotal para pintar exactamente el costo base del ramo original
                decimal precioRamoPuro = pedido.PrecioTotal - costoEnvioOriginal;

                txtPrecioTotal.Text = precioRamoPuro.ToString("F2");
                txtCostoEnvio.Text = costoEnvioOriginal.ToString("F2");
                txtAnticipo.Text = Convert.ToDecimal(pedido.Anticipo).ToString("F2");

                // Posicionar el ComboBox en el método de pago correcto de forma segura
                bool encontrado = false;
                foreach (ComboBoxItem item in cbMetodoAnticipo.Items)
                {
                    if (item.Content.ToString().Equals(metodoOriginal, StringComparison.OrdinalIgnoreCase))
                    {
                        cbMetodoAnticipo.SelectedItem = item;
                        encontrado = true;
                        break;
                    }
                }

                // Si por alguna razón guardaste un método de pago personalizado, seleccionamos el primero por seguridad
                if (!encontrado && cbMetodoAnticipo.Items.Count > 0)
                {
                    cbMetodoAnticipo.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los datos en modo edición: " + ex.Message, "Error de Inicialización");
            }
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
                    if (esModoEdicion)
                    {
                        // UPDATE DIRECTO (Mantiene intactos los estados e historial del flujo original)
                        string queryUpdate = @"UPDATE Pedidos SET 
                                                ClienteNombre = @nom, 
                                                Telefono = @tel, 
                                                FechaEntrega = @fec, 
                                                Direccion = @dir, 
                                                NotaTarjeta = @not, 
                                                Descripcion = @des, 
                                                PrecioTotal = @total, 
                                                Anticipo = @ant, 
                                                SaldoPendiente = @saldo, 
                                                MetodoPago = @metodo, 
                                                CostoEnvio = @envio 
                                               WHERE Id = @id";

                        using (SqlCommand cmd = new SqlCommand(queryUpdate, con))
                        {
                            cmd.Parameters.AddWithValue("@nom", txtCliente.Text.Trim());
                            cmd.Parameters.AddWithValue("@tel", txtTelefono.Text.Trim());
                            cmd.Parameters.AddWithValue("@fec", dpFecha.SelectedDate.Value);
                            cmd.Parameters.AddWithValue("@dir", string.IsNullOrWhiteSpace(txtDireccion.Text) ? "Recoge en Tienda" : txtDireccion.Text.Trim());
                            cmd.Parameters.AddWithValue("@not", txtNota.Text.Trim());
                            cmd.Parameters.AddWithValue("@des", txtDescripcion.Text.Trim());
                            cmd.Parameters.AddWithValue("@total", precioTotalFinal);
                            cmd.Parameters.AddWithValue("@ant", anticipo);
                            cmd.Parameters.AddWithValue("@saldo", saldoPendiente);
                            cmd.Parameters.AddWithValue("@metodo", metodoAnticipo);
                            cmd.Parameters.AddWithValue("@envio", envio);
                            cmd.Parameters.AddWithValue("@id", pedidoIdEditar);

                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("¡Los cambios al pedido se guardaron correctamente!", "Pedido Modificado", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // FUNCIONALIDAD ORIGINAL MANTENIDA: Creación tradicional de pedido
                        string queryInsert = @"INSERT INTO Pedidos (ClienteNombre, Telefono, FechaEntrega, FechaRegistro, Direccion, NotaTarjeta, Estado, Descripcion, PrecioTotal, Anticipo, SaldoPendiente, MetodoPago, CostoEnvio) 
                                               VALUES (@nom, @tel, @fec, @fecReg, @dir, @not, 'Pendiente', @des, @total, @ant, @saldo, @metodo, @envio)";

                        using (SqlCommand cmd = new SqlCommand(queryInsert, con))
                        {
                            cmd.Parameters.AddWithValue("@nom", txtCliente.Text.Trim());
                            cmd.Parameters.AddWithValue("@tel", txtTelefono.Text.Trim());
                            cmd.Parameters.AddWithValue("@fec", dpFecha.SelectedDate.Value);
                            cmd.Parameters.AddWithValue("@fecReg", DateTime.Now);
                            cmd.Parameters.AddWithValue("@dir", string.IsNullOrWhiteSpace(txtDireccion.Text) ? "Recoge en Tienda" : txtDireccion.Text.Trim());
                            cmd.Parameters.AddWithValue("@not", txtNota.Text.Trim());
                            cmd.Parameters.AddWithValue("@des", txtDescripcion.Text.Trim());
                            cmd.Parameters.AddWithValue("@total", precioTotalFinal);
                            cmd.Parameters.AddWithValue("@ant", anticipo);
                            cmd.Parameters.AddWithValue("@saldo", saldoPendiente);
                            cmd.Parameters.AddWithValue("@metodo", metodoAnticipo);
                            cmd.Parameters.AddWithValue("@envio", envio);

                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("¡Pedido y costo de envío agendados con éxito!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar el pedido en la base de datos: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}