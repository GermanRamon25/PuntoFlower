using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;
using PuntoFlower.Data;
using PuntoFlower.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DgAlignment = System.Drawing.StringAlignment;
using DgBrush = System.Drawing.SolidBrush;
using DgColor = System.Drawing.Color;
using DgFont = System.Drawing.Font;
using DgGraphics = System.Drawing.Graphics;
using DgRectangle = System.Drawing.RectangleF;
using DgStringFormat = System.Drawing.StringFormat;
using DgStyle = System.Drawing.FontStyle;
using iTextDocument = iTextSharp.text.Document;
using iTextFont = iTextSharp.text.Font;
using iTextParagraph = iTextSharp.text.Paragraph;

namespace PuntoFlower.Views
{
    public partial class SalesView : UserControl
    {
        public static ObservableCollection<ItemTicket> ProductosEnTicket { get; set; } = new ObservableCollection<ItemTicket>();

        private List<DetalleInsumo> composicionRamoActual = new List<DetalleInsumo>();
        private List<DetalleInsumo> composicionEspecialActual = new List<DetalleInsumo>();

        private int capacityRamo = 0;
        private decimal precioRamo = 0;
        private int floresAgregadas = 0;
        private Dictionary<int, decimal> preciosDinamicos = new Dictionary<int, decimal>();

        private List<ItemTicket> productosParaImprimir = new List<ItemTicket>();
        private decimal ticketTotal = 0;
        private decimal ticketPagado = 0;
        private decimal ticketCambio = 0;

        private decimal ticketDescuentoDinero = 0;
        private float ticketPorcentajeAplicado = 0;
        private string ticketReferencia = "";

        private int idPedidoParaAbonar = 0;
        private bool esCobroDeAbonoExistente = false;

        public SalesView()
        {
            InitializeComponent();
            lstVenta.ItemsSource = ProductosEnTicket;
            CargarPreciosDesdeDB();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarInsumos();
            CargarPreciosDesdeDB();
            CargarEncargadosCuentasDinamicos();
            ListarPedidosPendientesEnTabla();
            CargarPedidosAlComboBoxDesplegable();
            ActualizarTotal();
        }

        private void CargarPedidosAlComboBoxDesplegable()
        {
            List<PedidoComboClass> listaCombo = new List<PedidoComboClass>();
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "SELECT Id, ClienteNombre, Telefono, FechaEntrega, MetodoPago, Anticipo, ISNULL(CostoEnvio, 0) AS CostoEnvio FROM Pedidos WHERE Estado != 'Entregado' ORDER BY ClienteNombre ASC";
                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            listaCombo.Add(new PedidoComboClass
                            {
                                Id = (int)r["Id"],
                                ClienteNombre = r["ClienteNombre"].ToString(),
                                Telefono = r["Telefono"] != DBNull.Value ? r["Telefono"].ToString() : "",
                                FechaEntrega = r["FechaEntrega"] != DBNull.Value ? Convert.ToDateTime(r["FechaEntrega"]) : (DateTime?)null,
                                MetodoPagoOrigen = r["MetodoPago"] != DBNull.Value ? r["MetodoPago"].ToString() : "Efectivo",
                                AnticipoOrigen = r["Anticipo"] != DBNull.Value ? Convert.ToDecimal(r["Anticipo"]) : 0,
                                CostoEnvio = Convert.ToDecimal(r["CostoEnvio"])
                            });
                        }
                    }
                }
                cbPedidosAgendaDesplegable.ItemsSource = listaCombo;
            }
            catch { }
        }

        private void cbPedidosAgendaDesplegable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var pedidoSeleccionado = cbPedidosAgendaDesplegable.SelectedItem as PedidoComboClass;

            if (pedidoSeleccionado != null)
            {
                txtClientePedidoCaja.Text = pedidoSeleccionado.ClienteNombre;
                txtTelPedidoCaja.Text = pedidoSeleccionado.Telefono;
                dpFechaPedidoCaja.SelectedDate = pedidoSeleccionado.FechaEntrega;

                // CORREGIDO: Mantenemos el porcentaje de descuento en 0 y pintamos las etiquetas informativas
                txtDescuento.Text = "0";

                if (panelFleteAuditoria != null && lblEnvioApartado != null && lblAnticipoRegistro != null)
                {
                    panelFleteAuditoria.Visibility = Visibility.Visible;
                    lblEnvioApartado.Text = pedidoSeleccionado.CostoEnvio.ToString("C");
                    lblAnticipoRegistro.Text = pedidoSeleccionado.AnticipoOrigen.ToString("C");
                }

                if (cbMetodoPago != null)
                {
                    string met = pedidoSeleccionado.MetodoPagoOrigen.Trim();
                    bool encontrado = false;
                    for (int i = 0; i < cbMetodoPago.Items.Count; i++)
                    {
                        var item = cbMetodoPago.Items[i] as ComboBoxItem;
                        if (item != null && item.Content.ToString().Equals(met, StringComparison.OrdinalIgnoreCase))
                        {
                            cbMetodoPago.SelectedIndex = i;
                            encontrado = true;
                            break;
                        }
                    }
                    if (!encontrado) cbMetodoPago.SelectedIndex = 0;
                }

                txtClientePedidoCaja.IsEnabled = false;
                txtTelPedidoCaja.IsEnabled = false;
                dpFechaPedidoCaja.IsEnabled = false;
                txtDescuento.IsEnabled = false; // Bloqueado temporalmente solo para este modo

                lblInfoNuevoCliente.Text = "* Información y montos recuperados de la agenda.";
            }
            else
            {
                txtClientePedidoCaja.Clear();
                txtTelPedidoCaja.Clear();
                dpFechaPedidoCaja.SelectedDate = null;
                txtDescuento.Text = "0";

                if (panelFleteAuditoria != null) panelFleteAuditoria.Visibility = Visibility.Collapsed;

                txtClientePedidoCaja.IsEnabled = true;
                txtTelPedidoCaja.IsEnabled = true;
                dpFechaPedidoCaja.IsEnabled = true;
                txtDescuento.IsEnabled = true;

                if (cbMetodoPago != null) cbMetodoPago.SelectedIndex = 0;

                lblInfoNuevoCliente.Text = "* Deja el combo en blanco para registrar como un NUEVO CLIENTE de mostrador.";
            }
            ActualizarTotal();
        }

        private void ListarPedidosPendientesEnTabla()
        {
            List<object> lista = new List<object>();
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string q = "SELECT Id, ClienteNombre, Descripcion, SaldoPendiente, Anticipo, PrecioTotal FROM Pedidos WHERE Estado != 'Entregado'";
                    SqlCommand cmd = new SqlCommand(q, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            decimal saldo = r["SaldoPendiente"] != DBNull.Value ? Convert.ToDecimal(r["SaldoPendiente"]) : 0;
                            decimal ant = r["Anticipo"] != DBNull.Value ? Convert.ToDecimal(r["Anticipo"]) : 0;
                            decimal tot = r["PrecioTotal"] != DBNull.Value ? Convert.ToDecimal(r["PrecioTotal"]) : 0;

                            decimal montoAMostrar = (ant == 0 && saldo == 0) ? tot : saldo;

                            lista.Add(new
                            {
                                Id = (int)r["Id"],
                                ClienteNombre = r["ClienteNombre"].ToString(),
                                Descripcion = r["Descripcion"].ToString(),
                                SaldoPendiente = montoAMostrar
                            });
                        }
                    }
                }
                dgPedidosPendientes.ItemsSource = lista;
            }
            catch { }
        }

        private void btnRefrescarPedidosAbono_Click(object sender, RoutedEventArgs e)
        {
            ListarPedidosPendientesEnTabla();
            CargarPedidosAlComboBoxDesplegable();
        }

        private void btnCargarPedidoAlTicket_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            dynamic ped = btn.DataContext;
            if (ped == null) return;

            ProductosEnTicket.Clear();
            idPedidoParaAbonar = ped.Id;
            esCobroDeAbonoExistente = true;

            ProductosEnTicket.Add(new ItemTicket
            {
                ProductoNombre = $"Abono/Liq. de Pedido: {ped.ClienteNombre}",
                Total = ped.SaldoPendiente,
                DetalleVisual = "Cobro de cuenta pendiente - Agenda",
                InsumosADescontar = new List<DetalleInsumo>()
            });

            cbMetodoPago.SelectedIndex = 0;
            chkEsPedidoApartado.IsChecked = false;
            chkEsPedidoApartado.IsEnabled = false;

            ActualizarTotal();
            MessageBox.Show($"Saldo de {ped.ClienteNombre} cargado a la caja registradora. Elige el método de pago.", "Pedido Cargado");
        }

        private void CargarEncargadosCuentasDinamicos()
        {
            try
            {
                ConexionDB db = new ConexionDB();
                string e1 = db.ObtenerEncargadoCuenta1();
                string e2 = db.ObtenerEncargadoCuenta2();
                if (cbCuentaDestino != null)
                {
                    cbCuentaDestino.Items.Clear();
                    cbCuentaDestino.Items.Add(string.IsNullOrWhiteSpace(e1) ? "Encargado 1" : e1);
                    cbCuentaDestino.Items.Add(string.IsNullOrWhiteSpace(e2) ? "Encargado 2" : e2);
                    cbCuentaDestino.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private void CargarPreciosDesdeDB()
        {
            preciosDinamicos.Clear();
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    SqlCommand cmd = new SqlCommand("SELECT Capacidad, Precio FROM PreciosRamos", con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int cap = (int)r["Capacidad"];
                            decimal precio = Convert.ToDecimal(r["Precio"]);
                            preciosDinamicos.Add(cap, precio);
                            ActualizarTextoBoton(cap, precio);
                        }
                    }
                }
            }
            catch { }
        }

        private void ActualizarTextoBoton(int capacidad, decimal precio)
        {
            string texto = $"{capacidad} pz ({precio:C0})";
            switch (capacidad)
            {
                case 6: rbRamo6.Content = texto; break;
                case 12: rbRamo12.Content = texto; break;
                case 24: rbRamo24.Content = texto; break;
                case 36: rbRamo36.Content = texto; break;
                case 50: rbRamo50.Content = texto; break;
                case 72: rbRamo72.Content = texto; break;
                case 100: rbRamo100.Content = texto; break;
                case 150: rbRamo150.Content = texto; break;
                case 200: rbRamo200.Content = texto; break;
                case 250: rbRamo250.Content = texto; break;
            }
        }

        private void CargarInsumos()
        {
            List<Producto> lista = new List<Producto>();
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "SELECT Nombre, PrecioVenta, RutaImagen FROM Productos WHERE StockActual > 0 AND Categoria = 'Venta'";
                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read()) lista.Add(new Producto
                        {
                            Nombre = r["Nombre"].ToString(),
                            PrecioVenta = Convert.ToDecimal(r["PrecioVenta"]),
                            RutaImagen = r["RutaImagen"]?.ToString()
                        });
                    }
                }
                cbInsumosRamos.ItemsSource = lista;
                cbInsumosLibre.ItemsSource = lista;
                cbInsumosEspeciales.ItemsSource = lista;
            }
            catch { }
        }

        private void Ramo_Checked(object sender, RoutedEventArgs e)
        {
            var rb = sender as RadioButton;
            if (rb == null || rb.Tag == null) return;
            capacityRamo = int.Parse(rb.Tag.ToString());
            precioRamo = preciosDinamicos.ContainsKey(capacityRamo) ? preciosDinamicos[capacityRamo] : 0;
            ActualizarProgreso();
        }

        private void btnAgregarAlRamo_Click(object sender, RoutedEventArgs e)
        {
            var flor = cbInsumosRamos.SelectedItem as Producto;
            if (flor == null || capacityRamo == 0) return;
            if (!int.TryParse(txtCantFlorRamo.Text, out int cant) || cant <= 0) return;
            if (floresAgregadas + cant > capacityRamo) { MessageBox.Show("Superas la capacidad del ramo."); return; }

            composicionRamoActual.Add(new DetalleInsumo { Nombre = flor.Nombre, Cantidad = cant });
            floresAgregadas += cant;
            ActualizarProgreso();
            txtCantFlorRamo.Text = "0";
            cbInsumosRamos.SelectedItem = null;
        }

        private void btnFinalizarRamo_Click(object sender, RoutedEventArgs e)
        {
            if (floresAgregadas != capacityRamo) { MessageBox.Show("Debes completar la cantidad de flores seleccionada."); return; }

            ProductosEnTicket.Add(new ItemTicket
            {
                ProductoNombre = $"Ramo de {capacityRamo} pz",
                Total = precioRamo,
                InsumosADescontar = new List<DetalleInsumo>(composicionRamoActual),
                DetalleVisual = string.Join(", ", composicionRamoActual.Select(x => $"{x.Cantidad} {x.Nombre}"))
            });
            LimpiarConfiguradorRamo();
        }

        private void btnAgregarVentaLibre_Click(object sender, RoutedEventArgs e)
        {
            var prod = cbInsumosLibre.SelectedItem as Producto;
            if (prod == null || !int.TryParse(txtCantLibre.Text, out int cant) || cant <= 0) return;

            ProductosEnTicket.Add(new ItemTicket
            {
                ProductoNombre = prod.Nombre,
                Total = prod.PrecioVenta * cant,
                InsumosADescontar = new List<DetalleInsumo> { new DetalleInsumo { Nombre = prod.Nombre, Cantidad = cant } },
                DetalleVisual = $"{cant} unidad(es) x {prod.PrecioVenta:C}"
            });
            cbInsumosLibre.SelectedItem = null;
            txtCantLibre.Text = "1";
            ActualizarTotal();
        }

        private void btnAgregarFlorEspecial_Click(object sender, RoutedEventArgs e)
        {
            var font = cbInsumosEspeciales.SelectedItem as Producto;
            if (font == null) return;
            if (!int.TryParse(txtCantFlorEspecial.Text, out int cant) || cant <= 0) return;

            composicionEspecialActual.Add(new DetalleInsumo { Nombre = font.Nombre, Cantidad = cant });
            lblProgresoEspecial.Text = "Flores añadidas: " + string.Join(", ", composicionEspecialActual.Select(x => $"{x.Cantidad} {x.Nombre}"));
            txtCantFlorEspecial.Text = "0";
            cbInsumosEspeciales.SelectedItem = null;
        }

        private void btnAgregarEspecial_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtNombreEspecial.Text) || !decimal.TryParse(txtPrecioEspecial.Text, out decimal precio) || precio <= 0) return;

            ProductosEnTicket.Add(new ItemTicket
            {
                ProductoNombre = txtNombreEspecial.Text.Trim(),
                Total = precio,
                InsumosADescontar = new List<DetalleInsumo>(composicionEspecialActual),
                DetalleVisual = composicionEspecialActual.Count > 0
                    ? "Especial / Insumos: " + string.Join(", ", composicionEspecialActual.Select(x => $"{x.Cantidad} {x.Nombre}"))
                    : "Servicio Especial (Sin insumos físicos)"
            });

            txtNombreEspecial.Clear();
            txtPrecioEspecial.Clear();
            composicionEspecialActual.Clear();
            lblProgresoEspecial.Text = "Flores añadidas: Ninguna";
            ActualizarTotal();
        }

        private void chkEsPedidoApartado_Checked(object sender, RoutedEventArgs e)
        {
            if (panelCamposPedidoCaja == null) return;
            panelCamposPedidoCaja.Visibility = Visibility.Visible;
            ActualizarTotal();
        }

        private void chkEsPedidoApartado_Unchecked(object sender, RoutedEventArgs e)
        {
            if (panelCamposPedidoCaja == null) return;
            panelCamposPedidoCaja.Visibility = Visibility.Collapsed;
            txtDescuento.Text = "0";
            txtDescuento.IsEnabled = true;
            cbPedidosAgendaDesplegable.SelectedItem = null;
            ActualizarTotal();
        }

        private void cbMetodoPago_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (panelCuentaTransferencia == null || panelNumeroReferencia == null) return;

            var item = cbMetodoPago.SelectedItem as ComboBoxItem;
            if (item != null && item.Content.ToString() == "Transferencia")
            {
                panelCuentaTransferencia.Visibility = Visibility.Visible;
                panelNumeroReferencia.Visibility = Visibility.Visible;
            }
            else
            {
                panelCuentaTransferencia.Visibility = Visibility.Collapsed;
                panelNumeroReferencia.Visibility = Visibility.Collapsed;
                if (txtNumeroReferencia != null) txtNumeroReferencia.Clear();
            }
            CalcularCambioMatematico();
        }

        private void txtDescuento_TextChanged(object sender, TextChangedEventArgs e)
        {
            // CORREGIDO: El descuento vuelve a ser un porcentaje limpio (%) para ventas normales
            if (cbPedidosAgendaDesplegable != null && cbPedidosAgendaDesplegable.SelectedItem != null) return;

            if (txtDescuento != null && float.TryParse(txtDescuento.Text, out float porc))
            {
                if (porc < 0) txtDescuento.Text = "0";
                if (porc > 100) txtDescuento.Text = "100";
            }
            ActualizarTotal();
        }

        private void txtPagoCon_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularCambioMatematico();
        }

        private decimal ObtenerTotalConDescuento()
        {
            decimal subtotal = ProductosEnTicket.Sum(x => x.Total);
            if (esCobroDeAbonoExistente) return subtotal;

            // CORREGIDO: Si liquidamos un pedido desde el ComboBox de la derecha, el total es el Saldo Pendiente de la agenda
            var pedidoSeleccionado = cbPedidosAgendaDesplegable.SelectedItem as PedidoComboClass;
            if (chkEsPedidoApartado != null && chkEsPedidoApartado.IsChecked == true && pedidoSeleccionado != null)
            {
                return subtotal + pedidoSeleccionado.CostoEnvio - pedidoSeleccionado.AnticipoOrigen;
            }

            // Venta libre tradicional aplicando porcentaje %
            decimal dineroDescontado = 0;
            if (txtDescuento != null && float.TryParse(txtDescuento.Text, out float porcentaje) && porcentaje > 0)
            {
                dineroDescontado = subtotal * (decimal)(porcentaje / 100.0);
            }
            return subtotal - dineroDescontado;
        }

        private void CalcularCambioMatematico()
        {
            if (txtCambio == null || txtPagoCon == null) return;

            decimal totalNeto = ObtenerTotalConDescuento();
            var itemPago = cbMetodoPago.SelectedItem as ComboBoxItem;
            string metodo = itemPago != null ? itemPago.Content.ToString() : "Efectivo";

            if (metodo != "Efectivo")
            {
                txtPagoCon.Text = totalNeto.ToString("F2");
                txtCambio.Text = "$0.00";
                return;
            }

            // CORREGIDO: Resta limpia tradicional para evitar el fallo de la foto
            if (decimal.TryParse(txtPagoCon.Text.Trim(), out decimal pago))
            {
                txtCambio.Text = (pago >= totalNeto) ? (pago - totalNeto).ToString("C") : "$0.00";
            }
            else
            {
                txtCambio.Text = "$0.00";
            }
        }

        private void btnConfirmarVenta_Click(object sender, RoutedEventArgs e)
        {
            decimal subtotalBase = ProductosEnTicket.Sum(x => x.Total);
            decimal totalNetoArreglo = ObtenerTotalConDescuento();
            if (ProductosEnTicket.Count == 0) return;

            float porcText = 0;
            if (cbPedidosAgendaDesplegable.SelectedItem == null) float.TryParse(txtDescuento.Text, out porcText);

            decimal totalDineroDescontado = 0;
            if (chkEsPedidoApartado.IsChecked == false && !esCobroDeAbonoExistente)
            {
                totalDineroDescontado = subtotalBase - totalNetoArreglo;
            }

            var itemPago = cbMetodoPago.SelectedItem as ComboBoxItem;
            string metodoPago = itemPago != null ? itemPago.Content.ToString() : "Efectivo";

            object cuentaDestino = DBNull.Value;
            object numeroRefValor = DBNull.Value;

            if (metodoPago == "Transferencia")
            {
                if (cbCuentaDestino != null && cbCuentaDestino.SelectedItem != null) cuentaDestino = cbCuentaDestino.SelectedItem.ToString();
                if (txtNumeroReferencia != null && !string.IsNullOrWhiteSpace(txtNumeroReferencia.Text)) numeroRefValor = txtNumeroReferencia.Text.Trim();
            }

            if (!decimal.TryParse(txtPagoCon.Text.Trim(), out decimal pagoRecibido) || pagoRecibido < totalNetoArreglo)
            {
                MessageBox.Show("Monto recibido insuficiente o formato de dinero incorrecto.", "Cobro Detenido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal cambioFinal = (metodoPago == "Efectivo") ? (pagoRecibido - totalNetoArreglo) : 0;

            PedidoComboClass pedidoEnlazadoCombo = cbPedidosAgendaDesplegable.SelectedItem as PedidoComboClass;
            ConexionDB db = new ConexionDB();
            btnConfirmarVenta.IsEnabled = false;

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    using (SqlTransaction tra = con.BeginTransaction())
                    {
                        try
                        {
                            string detProdNombres = string.Join(", ", ProductosEnTicket.Select(x => x.ProductoNombre));
                            string detVisualConcat = string.Join(" | ", ProductosEnTicket.Select(x => x.DetalleVisual));

                            if (esCobroDeAbonoExistente)
                            {
                                string actPedido = @"UPDATE Pedidos 
                                                     SET SaldoPendiente = SaldoPendiente - @abono, 
                                                         Estado = CASE WHEN (SaldoPendiente - @abono) <= 0 THEN 'Entregado' ELSE Estado END 
                                                     WHERE Id = @id";
                                using (SqlCommand cmdP = new SqlCommand(actPedido, con, tra))
                                {
                                    cmdP.Parameters.AddWithValue("@abono", totalNetoArreglo);
                                    cmdP.Parameters.AddWithValue("@id", idPedidoParaAbonar);
                                    cmdP.ExecuteNonQuery();
                                }
                            }
                            else if (chkEsPedidoApartado.IsChecked == true && pedidoEnlazadoCombo != null)
                            {
                                // Liquidación total de pedido existente
                                string queryActualizarExistente = @"UPDATE Pedidos 
                                                                    SET Descripcion = @des,
                                                                        PrecioTotal = @totReal,
                                                                        SaldoPendiente = 0,
                                                                        MetodoPagoLiquidacion = @metLiqui,
                                                                        Estado = 'Entregado'
                                                                    WHERE Id = @id";

                                using (SqlCommand cmdUp = new SqlCommand(queryActualizarExistente, con, tra))
                                {
                                    cmdUp.Parameters.AddWithValue("@id", pedidoEnlazadoCombo.Id);
                                    cmdUp.Parameters.AddWithValue("@des", detProdNombres + " (" + detVisualConcat + ")");
                                    cmdUp.Parameters.AddWithValue("@totReal", subtotalBase + pedidoEnlazadoCombo.CostoEnvio);
                                    cmdUp.Parameters.AddWithValue("@metLiqui", metodoPago);
                                    cmdUp.ExecuteNonQuery();
                                }
                            }
                            else if (chkEsPedidoApartado.IsChecked == true && pedidoEnlazadoCombo == null)
                            {
                                // Creación de un pedido apartado desde caja
                                string insPed = @"INSERT INTO Pedidos (ClienteNombre, Telefono, FechaEntrega, FechaRegistro, Direccion, NotaTarjeta, Estado, Descripcion, PrecioTotal, Anticipo, SaldoPendiente, MetodoPago, CostoEnvio) 
                                                 VALUES (@nom, @tel, @fec, GETDATE(), 'Mostrador Caja', '', 'Pendiente', @des, @tot, @ant, @saldo, @met, 0)";
                                using (SqlCommand cmdPed = new SqlCommand(insPed, con, tra))
                                {
                                    cmdPed.Parameters.AddWithValue("@nom", txtClientePedidoCaja.Text.Trim());
                                    cmdPed.Parameters.AddWithValue("@tel", txtTelPedidoCaja.Text.Trim());
                                    cmdPed.Parameters.AddWithValue("@fec", dpFechaPedidoCaja.SelectedDate.Value);
                                    cmdPed.Parameters.AddWithValue("@des", detProdNombres + " (" + detVisualConcat + ")");
                                    cmdPed.Parameters.AddWithValue("@tot", subtotalBase);
                                    cmdPed.Parameters.AddWithValue("@ant", pagoRecibido);
                                    cmdPed.Parameters.AddWithValue("@saldo", subtotalBase - pagoRecibido);
                                    cmdPed.Parameters.AddWithValue("@met", metodoPago);
                                    cmdPed.ExecuteNonQuery();
                                }
                            }

                            string nombreConceptoVenta = esCobroDeAbonoExistente ? ProductosEnTicket[0].ProductoNombre : $"Liquidación Pedido: {(pedidoEnlazadoCombo != null ? pedidoEnlazadoCombo.ClienteNombre : txtClientePedidoCaja.Text.Trim())} ({detProdNombres})";

                            if (!esCobroDeAbonoExistente && chkEsPedidoApartado.IsChecked == false)
                            {
                                foreach (var item in ProductosEnTicket)
                                {
                                    string q = @"INSERT INTO Ventas (Fecha, ProductoNombre, Total, Cantidad, MetodoPago, MontoRecibido, MontoCambio, CuentaTransferencia, DescuentoAplicado, NumeroReferencia) 
                                               VALUES (GETDATE(), @n, @t, 1, @metodo, @rec, @cam, @cuenta, @desc, @numRef)";
                                    using (SqlCommand cmdV = new SqlCommand(q, con, tra))
                                    {
                                        cmdV.Parameters.AddWithValue("@n", item.ProductoNombre);
                                        cmdV.Parameters.AddWithValue("@t", item.Total);
                                        cmdV.Parameters.AddWithValue("@metodo", metodoPago);
                                        cmdV.Parameters.AddWithValue("@rec", pagoRecibido);
                                        cmdV.Parameters.AddWithValue("@cam", cambioFinal);
                                        cmdV.Parameters.AddWithValue("@cuenta", cuentaDestino);
                                        cmdV.Parameters.AddWithValue("@desc", totalDineroDescontado / ProductosEnTicket.Count);
                                        cmdV.Parameters.AddWithValue("@numRef", numeroRefValor);
                                        cmdV.ExecuteNonQuery();
                                    }
                                }
                            }
                            else
                            {
                                string insVenAnt = @"INSERT INTO Ventas (Fecha, ProductoNombre, Total, Cantidad, MetodoPago, MontoRecibido, MontoCambio, CuentaTransferencia, DescuentoAplicado, NumeroReferencia) 
                                                     VALUES (GETDATE(), @n, @t, 1, @metodo, @rec, @cam, @cuenta, 0, @numRef)";
                                using (SqlCommand cmdV = new SqlCommand(insVenAnt, con, tra))
                                {
                                    cmdV.Parameters.AddWithValue("@n", nombreConceptoVenta);
                                    cmdV.Parameters.AddWithValue("@t", totalNetoArreglo);
                                    cmdV.Parameters.AddWithValue("@metodo", metodoPago);
                                    cmdV.Parameters.AddWithValue("@rec", pagoRecibido);
                                    cmdV.Parameters.AddWithValue("@cam", cambioFinal);
                                    cmdV.Parameters.AddWithValue("@cuenta", cuentaDestino);
                                    cmdV.Parameters.AddWithValue("@numRef", numeroRefValor);
                                    cmdV.ExecuteNonQuery();
                                }
                            }

                            // Descuento de Almacén Floral
                            if (!esCobroDeAbonoExistente)
                            {
                                foreach (var item in ProductosEnTicket)
                                {
                                    foreach (var insumo in item.InsumosADescontar)
                                    {
                                        using (SqlCommand cmdS = new SqlCommand("UPDATE Productos SET StockActual = StockActual - @c WHERE Nombre = @nom AND Categoria = 'Venta'", con, tra))
                                        {
                                            cmdS.Parameters.AddWithValue("@c", insumo.Quantity ?? insumo.Cantidad);
                                            cmdS.Parameters.AddWithValue("@nom", insumo.Nombre);
                                            cmdS.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }

                            tra.Commit();

                            productosParaImprimir = ProductosEnTicket.ToList();
                            ticketTotal = totalNetoArreglo;
                            ticketPagado = pagoRecibido;
                            ticketCambio = cambioFinal;
                            ticketDescuentoDinero = totalDineroDescontado;
                            ticketPorcentajeAplicado = porcText;
                            ticketReferencia = numeroRefValor != DBNull.Value ? numeroRefValor.ToString() : "";

                            MessageBoxResult result = MessageBox.Show("Operación registrada con éxito contable.\n\n¿Deseas imprimir el ticket físico en este momento?", "Venta Exitosa", MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                ImprimirTicketTermico();
                            }

                            // RESET TOTAL DE UI
                            ProductosEnTicket.Clear();
                            txtPagoCon.Clear();
                            txtDescuento.Text = "0";
                            chkEsPedidoApartado.IsChecked = false;
                            chkEsPedidoApartado.IsEnabled = true;
                            txtDescuento.IsEnabled = true;
                            esCobroDeAbonoExistente = false;
                            idPedidoParaAbonar = 0;
                            txtClientePedidoCaja.Clear();
                            txtTelPedidoCaja.Clear();
                            dpFechaPedidoCaja.SelectedDate = null;
                            cbPedidosAgendaDesplegable.SelectedItem = null;
                            if (txtNumeroReferencia != null) txtNumeroReferencia.Clear();
                            cbMetodoPago.SelectedIndex = 0;
                            ListarPedidosPendientesEnTabla();
                            CargarPedidosAlComboBoxDesplegable();
                            ActualizarTotal();
                        }
                        catch (Exception ex)
                        {
                            tra.Rollback();
                            MessageBox.Show("Transacción revertida por integridad: " + ex.Message, "Error Crítico");
                        }
                    }
                }
            }
            catch (Exception exCon)
            {
                MessageBox.Show("Error de enlace: " + exCon.Message, "Fallo");
            }
            finally
            {
                btnConfirmarVenta.IsEnabled = true;
            }
        }

        private void ImprimirTicketTermico()
        {
            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrintPage += new PrintPageEventHandler(DrawTicketPage);
                pd.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se detectó una impresora activa: " + ex.Message, "Fallo de Impresión", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void DrawTicketPage(object sender, PrintPageEventArgs e)
        {
            ConexionDB db = new ConexionDB();
            string sucursal = db.ObtenerNombreSucursal();
            DgGraphics g = e.Graphics;

            DgFont fontTitulo = new DgFont("Arial", 11, DgStyle.Bold);
            DgFont fontBold = new DgFont("Arial", 8, DgStyle.Bold);
            DgFont fontNormal = new DgFont("Arial", 8, DgStyle.Regular);
            DgBrush brush = new DgBrush(DgColor.Black);

            float y = 10;
            g.DrawString("🌸 PUNTO FLOWER 🌸", fontTitulo, brush, new DgRectangle(0, y, 220, 20), new DgStringFormat { Alignment = DgAlignment.Center });
            y += 20;
            g.DrawString(sucursal, fontBold, brush, new DgRectangle(0, y, 220, 15), new DgStringFormat { Alignment = DgAlignment.Center });
            y += 20;

            g.DrawString($"Fecha: {DateTime.Now:g}", fontNormal, brush, 5, y); y += 15;
            g.DrawString($"Atendió: {Session.UsuarioActual}", fontNormal, brush, 5, y); y += 15;

            var itemPago = cbMetodoPago.SelectedItem as ComboBoxItem;
            string metodo = itemPago != null ? itemPago.Content.ToString() : "Efectivo";
            g.DrawString($"Método Pago: {metodo}", fontNormal, brush, 5, y); y += 15;

            if (!string.IsNullOrEmpty(ticketReferencia)) { g.DrawString($"Ref/Dep: {ticketReferencia}", fontBold, brush, 5, y); y += 15; }

            g.DrawString("==================================", fontNormal, brush, 5, y); y += 15;

            foreach (var item in productosParaImprimir)
            {
                g.DrawString(item.ProductoNombre, fontBold, brush, 5, y); y += 13;
                g.DrawString($"   {item.DetalleVisual}", fontNormal, brush, 5, y); y += 13;
            }

            g.DrawString("==================================", fontNormal, brush, 5, y); y += 15;
            g.DrawString($"TOTAL LIQUIDADO: {ticketTotal:C}", fontBold, brush, 5, y); y += 15;
            g.DrawString($"EFECTIVO RECIBIDO: {ticketPagado:C}", fontNormal, brush, 5, y); y += 15;
            g.DrawString($"CAMBIO ENTREGADO: {ticketCambio:C}", fontBold, brush, 5, y); y += 25;

            g.DrawString("¡Gracias por su preferencia!", fontBold, brush, new DgRectangle(0, y, 220, 15), new DgStringFormat { Alignment = DgAlignment.Center });
        }

        private void RimujarConfiguradorRamo() => LimpiarConfiguradorRamo();

        private void LimpiarConfiguradorRamo()
        {
            composicionRamoActual.Clear(); floresAgregadas = 0; capacityRamo = 0;
            rbRamo6.IsChecked = rbRamo12.IsChecked = rbRamo24.IsChecked = rbRamo36.IsChecked = rbRamo50.IsChecked =
            rbRamo72.IsChecked = rbRamo100.IsChecked = rbRamo150.IsChecked = rbRamo200.IsChecked = rbRamo250.IsChecked = false;
            ActualizarTotal(); ActualizarProgreso();
        }

        private void ActualizarProgreso() => lblProgresoRamo.Text = $"Seleccionadas: {floresAgregadas} / {capacityRamo}";

        private void ActualizarTotal()
        {
            if (txtTotal == null) return;
            decimal totalNeto = ObtenerTotalConDescuento();
            txtTotal.Text = $"Total: {totalNeto:C}";
            CalcularCambioMatematico();
        }

        private void btnLimpiarTicket_Click(object sender, RoutedEventArgs e)
        {
            ProductosEnTicket.Clear();
            txtDescuento.Text = "0";
            chkEsPedidoApartado.IsChecked = false;
            chkEsPedidoApartado.IsEnabled = true;
            txtDescuento.IsEnabled = true;
            esCobroDeAbonoExistente = false;
            idPedidoParaAbonar = 0;
            cbPedidosAgendaDesplegable.SelectedItem = null;
            ActualizarTotal();
        }

        private void btnEliminarItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).DataContext as ItemTicket;
            if (item != null) { ProductosEnTicket.Remove(item); ActualizarTotal(); }
        }

        public class ItemTicket { public string ProductoNombre { get; set; } public decimal Total { get; set; } public string DetalleVisual { get; set; } public List<DetalleInsumo> InsumosADescontar { get; set; } }
        public class DetalleInsumo { public string Nombre { get; set; } public int? Quantity { get; set; } public int Cantidad { get; set; } }

        public class PedidoComboClass
        {
            public int Id { get; set; }
            public string ClienteNombre { get; set; }
            public string Telefono { get; set; }
            public DateTime? FechaEntrega { get; set; }
            public string MetodoPagoOrigen { get; set; }
            public decimal AnticipoOrigen { get; set; }
            public decimal CostoEnvio { get; set; }
        }
    }
}