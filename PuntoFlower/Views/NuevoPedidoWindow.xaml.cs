using PuntoFlower.Data;
using PuntoFlower.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace PuntoFlower.Views
{
    public partial class NuevoPedidoWindow : Window
    {
        private bool esModoEdicion = false;
        private int pedidoIdEditar = 0;
        private decimal anticipoOriginalEdicion = 0;

        public NuevoPedidoWindow()
        {
            InitializeComponent();
            dpFecha.SelectedDate = DateTime.Today;
            cbHoraEntrega.SelectedIndex = 3;
            CargarCatalogosAuxiliares();
        }

        public NuevoPedidoWindow(dynamic pedido) : this()
        {
            try
            {
                esModoEdicion = true;
                pedidoIdEditar = Convert.ToInt32(pedido.Id);

                this.Title = $"Modificar e Igualar Pedido - {ConexionGlobal.SucursalActual}";
                lblTituloVentana.Text = "Editar Datos del Pedido";
                btnGuardar.Content = "APLICAR CAMBIOS";
                btnGuardar.Background = System.Windows.Media.Brushes.DarkOrange;

                txtCliente.Text = pedido.ClienteNombre;
                txtTelefono.Text = pedido.Telefono;

                DateTime fechaCompleta = pedido.FechaEntrega;
                dpFecha.SelectedDate = fechaCompleta.Date;
                cbHoraEntrega.Text = fechaCompleta.ToString("hh:mm tt");

                txtDescripcion.Text = pedido.Descripcion;
                txtDireccion.Text = pedido.Direccion == "Recoge en Tienda" ? "" : pedido.Direccion;
                txtNota.Text = pedido.NotaTarjeta;

                decimal costoEnvioOriginal = 0;
                string metodoOriginal = "Efectivo";
                int? productoIdOriginal = null;
                int cantidadFloresOriginal = 0;

                ConexionDB db = new ConexionDB();
                using (SqlConnection con = db.OpenConnection())
                {
                    string queryFija = @"SELECT ISNULL(CostoEnvio, 0), ISNULL(MetodoPago, 'Efectivo'), ProductoId, ISNULL(CantidadFlores, 0) FROM Pedidos WHERE Id = @id AND Sucursal = @s";
                    using (SqlCommand cmd = new SqlCommand(queryFija, con))
                    {
                        cmd.Parameters.AddWithValue("@id", pedidoIdEditar);
                        cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                costoEnvioOriginal = Convert.ToDecimal(reader[0]);
                                metodoOriginal = reader[1].ToString();
                                if (reader[2] != DBNull.Value) productoIdOriginal = Convert.ToInt32(reader[2]);
                                cantidadFloresOriginal = Convert.ToInt32(reader[3]);
                            }
                        }
                    }
                }

                if (productoIdOriginal.HasValue && productoIdOriginal.Value > 0)
                {
                    cbProductoInventario.SelectedValue = productoIdOriginal.Value;
                    txtCantidadFlores.Text = cantidadFloresOriginal.ToString();
                }
                else
                {
                    cbProductoInventario.SelectedValue = 0;
                    txtCantidadFlores.Text = "0";
                }

                decimal precioRamoPuro = pedido.PrecioTotal - costoEnvioOriginal;
                anticipoOriginalEdicion = Convert.ToDecimal(pedido.Anticipo);

                txtPrecioTotal.Text = precioRamoPuro.ToString("F2");
                txtCostoEnvio.Text = costoEnvioOriginal.ToString("F2");
                txtAnticipo.Text = anticipoOriginalEdicion.ToString("F2");

                txtAnticipo.IsReadOnly = true;
                txtAnticipo.Background = System.Windows.Media.Brushes.LightGray;
                txtAnticipo.ToolTip = "Para agregar más pagos a este pedido, usa el botón 'Abonar' en la Agenda.";

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

                if (!encontrado && cbMetodoAnticipo.Items.Count > 0) cbMetodoAnticipo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los datos en modo edición: " + ex.Message, "Error de Inicialización");
            }
        }

        private void CargarCatalogosAuxiliares()
        {
            ConexionDB db = new ConexionDB();
            try
            {
                List<Producto> listaFlores = new List<Producto> { new Producto { Id = 0, Nombre = "-- Detectar por descripción / Sin flor fija --" } };
                using (SqlConnection con = db.OpenConnection())
                {
                    string q = "SELECT Id, Nombre, StockActual FROM Productos WHERE Categoria = 'Venta' AND Sucursal = @s ORDER BY Nombre ASC";
                    using (SqlCommand cmd = new SqlCommand(q, con))
                    {
                        cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                listaFlores.Add(new Producto { Id = Convert.ToInt32(r["Id"]), Nombre = r["Nombre"].ToString(), StockActual = Convert.ToInt32(r["StockActual"]) });
                            }
                        }
                    }
                }
                cbProductoInventario.ItemsSource = listaFlores;
                cbProductoInventario.SelectedIndex = 0;

                List<string> cuentas = new List<string>();
                string enc1 = db.ObtenerEncargadoCuenta1();
                if (!string.IsNullOrWhiteSpace(enc1)) foreach (var c in enc1.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) cuentas.Add(c.Trim());
                string enc2 = db.ObtenerEncargadoCuenta2();
                if (!string.IsNullOrWhiteSpace(enc2) && !cuentas.Contains(enc2.Trim())) cuentas.Add(enc2.Trim());
                if (cuentas.Count == 0) cuentas.Add("General");
                cbCuentaAnticipo.ItemsSource = cuentas;
                cbCuentaAnticipo.SelectedIndex = 0;
            }
            catch { }
        }

        private void cbMetodoAnticipo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (panelTransferenciaAnticipo == null) return;
            var item = cbMetodoAnticipo.SelectedItem as ComboBoxItem;
            string metodo = item != null ? item.Content.ToString() : "Efectivo";
            panelTransferenciaAnticipo.Visibility = metodo == "Transferencia" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CalcularSaldo(object sender, TextChangedEventArgs e)
        {
            if (lblSaldo == null || txtPrecioTotal == null || txtCostoEnvio == null || txtAnticipo == null) return;

            decimal.TryParse(txtPrecioTotal.Text.Trim(), out decimal ramo);
            decimal.TryParse(txtCostoEnvio.Text.Trim(), out decimal envio);
            decimal.TryParse(txtAnticipo.Text.Trim(), out decimal anticipo);

            decimal precioTotalReal = ramo + envio;
            decimal saldo = precioTotalReal - anticipo;
            if (saldo < 0) saldo = 0;

            lblSaldo.Text = saldo.ToString("C");

            if (lblTituloSaldo != null)
            {
                if (precioTotalReal > 0 && saldo == 0)
                {
                    lblTituloSaldo.Text = "SALDO PENDIENTE (PAGADO - PENDIENTE ENTREGA):";
                    lblTituloSaldo.Foreground = System.Windows.Media.Brushes.SeaGreen;
                    lblSaldo.Foreground = System.Windows.Media.Brushes.SeaGreen;
                }
                else
                {
                    lblTituloSaldo.Text = "SALDO PENDIENTE:";
                    lblTituloSaldo.Foreground = System.Windows.Media.Brushes.Crimson;
                    lblSaldo.Foreground = System.Windows.Media.Brushes.Crimson;
                }
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtCliente.Text) || dpFecha.SelectedDate == null)
            {
                MessageBox.Show("Por favor, ingresa al menos el nombre del cliente y la fecha de entrega.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrecioTotal.Text.Trim(), out decimal ramo) ||
                !decimal.TryParse(txtCostoEnvio.Text.Trim(), out decimal envio) ||
                !decimal.TryParse(txtAnticipo.Text.Trim(), out decimal anticipo))
            {
                MessageBox.Show("Por favor, ingresa montos numéricos válidos en los campos financieros.", "Error de datos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal precioTotalFinal = ramo + envio;
            if (anticipo > precioTotalFinal)
            {
                MessageBox.Show("El anticipo no puede ser mayor al precio total del pedido (Ramo + Envío).", "Monto Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime fechaBase = dpFecha.SelectedDate.Value.Date;
            TimeSpan horaAdicional = new TimeSpan(12, 0, 0);
            try
            {
                if (!string.IsNullOrWhiteSpace(cbHoraEntrega.Text))
                {
                    DateTime parsedTime = DateTime.Parse(cbHoraEntrega.Text);
                    horaAdicional = parsedTime.TimeOfDay;
                }
            }
            catch { }
            DateTime fechaEntregaFinal = fechaBase.Add(horaAdicional);

            decimal saldoPendiente = precioTotalFinal - anticipo;
            var itemMetodo = cbMetodoAnticipo.SelectedItem as ComboBoxItem;
            string metodoAnticipo = itemMetodo != null ? itemMetodo.Content.ToString() : "Efectivo";
            string cuentaTransf = metodoAnticipo == "Transferencia" && cbCuentaAnticipo.SelectedItem != null ? cbCuentaAnticipo.SelectedItem.ToString() : "";
            string refTransf = metodoAnticipo == "Transferencia" ? txtReferenciaAnticipo.Text.Trim() : "";

            int? prodIdSeleccionado = null;
            if (cbProductoInventario.SelectedValue != null && Convert.ToInt32(cbProductoInventario.SelectedValue) > 0)
                prodIdSeleccionado = Convert.ToInt32(cbProductoInventario.SelectedValue);

            int.TryParse(txtCantidadFlores.Text.Trim(), out int cantFlores);
            if (cantFlores < 0) cantFlores = 0;

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                using (SqlTransaction tra = con.BeginTransaction())
                {
                    try
                    {
                        if (esModoEdicion)
                        {
                            string queryUpdate = @"UPDATE Pedidos SET 
                                                    ClienteNombre = @nom, Telefono = @tel, FechaEntrega = @fec, Direccion = @dir, NotaTarjeta = @not, Descripcion = @des, 
                                                    PrecioTotal = @total, Anticipo = @ant, SaldoPendiente = @saldo, MetodoPago = @metodo, CostoEnvio = @envio, ProductoId = @prodId, CantidadFlores = @cantFlores
                                                   WHERE Id = @id AND Sucursal = @s";
                            using (SqlCommand cmd = new SqlCommand(queryUpdate, con, tra))
                            {
                                cmd.Parameters.AddWithValue("@nom", txtCliente.Text.Trim());
                                cmd.Parameters.AddWithValue("@tel", txtTelefono.Text.Trim());
                                cmd.Parameters.AddWithValue("@fec", fechaEntregaFinal);
                                cmd.Parameters.AddWithValue("@dir", string.IsNullOrWhiteSpace(txtDireccion.Text) ? "Recoge en Tienda" : txtDireccion.Text.Trim());
                                cmd.Parameters.AddWithValue("@not", txtNota.Text.Trim());
                                cmd.Parameters.AddWithValue("@des", txtDescripcion.Text.Trim());
                                cmd.Parameters.AddWithValue("@total", precioTotalFinal);
                                cmd.Parameters.AddWithValue("@ant", anticipo);
                                cmd.Parameters.AddWithValue("@saldo", saldoPendiente);
                                cmd.Parameters.AddWithValue("@metodo", metodoAnticipo);
                                cmd.Parameters.AddWithValue("@envio", envio);
                                cmd.Parameters.AddWithValue("@prodId", (object)prodIdSeleccionado ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@cantFlores", cantFlores);
                                cmd.Parameters.AddWithValue("@id", pedidoIdEditar);
                                cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                                cmd.ExecuteNonQuery();
                            }
                            tra.Commit();
                            MessageBox.Show($"¡Los cambios al pedido de {ConexionGlobal.SucursalActual} se guardaron correctamente!", "Pedido Modificado", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            // ZONA HORARIA BLINDADA: Insertar pedido
                            string queryInsert = @"INSERT INTO Pedidos (ClienteNombre, Telefono, FechaEntrega, FechaRegistro, Direccion, NotaTarjeta, Estado, Descripcion, PrecioTotal, Anticipo, SaldoPendiente, MetodoPago, CostoEnvio, ProductoId, CantidadFlores, InventarioDescontado, FleteCobradoEnCaja, Sucursal) 
                                                   VALUES (@nom, @tel, @fec, @fechaLocal, @dir, @not, 'Pendiente', @des, @total, @ant, @saldo, @metodo, @envio, @prodId, @cantFlores, 0, 0, @s);
                                                   SELECT SCOPE_IDENTITY();";
                            int nuevoPedidoId = 0;
                            using (SqlCommand cmd = new SqlCommand(queryInsert, con, tra))
                            {
                                cmd.Parameters.AddWithValue("@nom", txtCliente.Text.Trim());
                                cmd.Parameters.AddWithValue("@tel", txtTelefono.Text.Trim());
                                cmd.Parameters.AddWithValue("@fec", fechaEntregaFinal);
                                cmd.Parameters.AddWithValue("@fechaLocal", DateTime.Now);
                                cmd.Parameters.AddWithValue("@dir", string.IsNullOrWhiteSpace(txtDireccion.Text) ? "Recoge en Tienda" : txtDireccion.Text.Trim());
                                cmd.Parameters.AddWithValue("@not", txtNota.Text.Trim());
                                cmd.Parameters.AddWithValue("@des", txtDescripcion.Text.Trim());
                                cmd.Parameters.AddWithValue("@total", precioTotalFinal);
                                cmd.Parameters.AddWithValue("@ant", anticipo);
                                cmd.Parameters.AddWithValue("@saldo", saldoPendiente);
                                cmd.Parameters.AddWithValue("@metodo", metodoAnticipo);
                                cmd.Parameters.AddWithValue("@envio", envio);
                                cmd.Parameters.AddWithValue("@prodId", (object)prodIdSeleccionado ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@cantFlores", cantFlores);
                                cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                                nuevoPedidoId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            if (anticipo > 0)
                            {
                                // ZONA HORARIA BLINDADA: Historial de abonos
                                string qAbono = @"INSERT INTO HistorialAbonosPedidos (PedidoId, FechaAbono, Monto, MetodoPago, CuentaTransferencia, NumeroReferencia, UsuarioRegistro, Sucursal)
                                                  VALUES (@pedId, @fechaLocal, @monto, @metodo, @cuenta, @ref, @user, @s)";
                                using (SqlCommand cmdAb = new SqlCommand(qAbono, con, tra))
                                {
                                    cmdAb.Parameters.AddWithValue("@pedId", nuevoPedidoId);
                                    cmdAb.Parameters.AddWithValue("@fechaLocal", DateTime.Now);
                                    cmdAb.Parameters.AddWithValue("@monto", anticipo);
                                    cmdAb.Parameters.AddWithValue("@metodo", metodoAnticipo);
                                    cmdAb.Parameters.AddWithValue("@cuenta", string.IsNullOrEmpty(cuentaTransf) ? (object)DBNull.Value : cuentaTransf);
                                    cmdAb.Parameters.AddWithValue("@ref", string.IsNullOrEmpty(refTransf) ? (object)DBNull.Value : refTransf);
                                    cmdAb.Parameters.AddWithValue("@user", Session.UsuarioActual ?? "Empleado");
                                    cmdAb.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                                    cmdAb.ExecuteNonQuery();
                                }

                                string etiquetaConcepto = saldoPendiente == 0 ? $"Anticipo Liquidación Pedido #{nuevoPedidoId}: {txtCliente.Text.Trim()} ({txtDescripcion.Text.Trim()})" : $"Anticipo Nuevo Pedido #{nuevoPedidoId}: {txtCliente.Text.Trim()} ({txtDescripcion.Text.Trim()})";

                                // ZONA HORARIA BLINDADA: Ingresar anticipo a la caja
                                string qVenta = @"INSERT INTO Ventas (Fecha, ProductoNombre, Cantidad, Total, MetodoPago, MontoRecibido, MontoCambio, CuentaTransferencia, DescuentoAplicado, NumeroReferencia, ProductoId, Sucursal)
                                                  VALUES (@fechaLocal, @concepto, 1, @total, @metodo, @recibido, 0, @cuenta, 0, @ref, NULL, @s)";
                                using (SqlCommand cmdV = new SqlCommand(qVenta, con, tra))
                                {
                                    cmdV.Parameters.AddWithValue("@fechaLocal", DateTime.Now);
                                    cmdV.Parameters.AddWithValue("@concepto", etiquetaConcepto);
                                    cmdV.Parameters.AddWithValue("@total", anticipo);
                                    cmdV.Parameters.AddWithValue("@metodo", metodoAnticipo);
                                    cmdV.Parameters.AddWithValue("@recibido", anticipo);
                                    cmdV.Parameters.AddWithValue("@cuenta", string.IsNullOrEmpty(cuentaTransf) ? (object)DBNull.Value : cuentaTransf);
                                    cmdV.Parameters.AddWithValue("@ref", string.IsNullOrEmpty(refTransf) ? (object)DBNull.Value : refTransf);
                                    cmdV.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                                    cmdV.ExecuteNonQuery();
                                }
                            }

                            tra.Commit();
                            MessageBox.Show($"¡Pedido agendado con éxito en {ConexionGlobal.SucursalActual}!\n\n• El inventario y el gasto de flete se aplicarán automáticamente el día que se entregue el pedido.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch { tra.Rollback(); throw; }
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