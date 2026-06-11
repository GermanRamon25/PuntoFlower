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
    // CLASES DE SOPORTE INTEGRADAS CORRECTAMENTE PARA EVITAR ERRORES DE ÁMBITO
    public class PedidoComboClass
    {
        public int Id { get; set; }
        public string ClienteNombre { get; set; }
        public string Telefono { get; set; }
        public DateTime FechaEntrega { get; set; }
        public decimal Anticipo { get; set; }
        public decimal CostoEnvio { get; set; }
        public string MetodoPagoOriginal { get; set; }
    }

    public class ItemTicket
    {
        public string ProductoNombre { get; set; }
        public decimal Total { get; set; }
        public string DetalleVisual { get; set; }
        public List<DetalleInsumo> InsumosADescontar { get; set; }
    }

    public class DetalleInsumo
    {
        public string Nombre { get; set; }
        public int? Quantity { get; set; }
        public int Cantidad { get; set; }
    }

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

        private void CargarPedidosAlComboBoxDesplegable()
        {
            List<PedidoComboClass> listaCombo = new List<PedidoComboClass>();
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string q = "SELECT Id, ClienteNombre, Telefono, FechaEntrega, Anticipo, CostoEnvio, MetodoPago FROM Pedidos WHERE Estado = 'Pendiente' ORDER BY FechaEntrega ASC";
                    SqlCommand cmd = new SqlCommand(q, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            listaCombo.Add(new PedidoComboClass
                            {
                                Id = (int)r["Id"],
                                ClienteNombre = r["ClienteNombre"].ToString(),
                                Telefono = r["Telefono"].ToString(),
                                FechaEntrega = Convert.ToDateTime(r["FechaEntrega"]),
                                Anticipo = r["Anticipo"] != DBNull.Value ? Convert.ToDecimal(r["Anticipo"]) : 0,
                                CostoEnvio = r["CostoEnvio"] != DBNull.Value ? Convert.ToDecimal(r["CostoEnvio"]) : 0,
                                MetodoPagoOriginal = r["MetodoPago"] != DBNull.Value ? r["MetodoPago"].ToString() : "Efectivo"
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
            var ped = cbPedidosAgendaDesplegable.SelectedItem as PedidoComboClass;
            if (ped != null)
            {
                txtClientePedidoCaja.Text = ped.ClienteNombre;
                txtTelPedidoCaja.Text = ped.Telefono;
                dpFechaPedidoCaja.SelectedDate = ped.FechaEntrega;
                txtFleteNuevoPedido.Text = ped.CostoEnvio.ToString("F2");
                txtPagoCon.Text = ped.Anticipo.ToString("F2");

                if (cbMetodoPago != null)
                {
                    string metodoOriginal = ped.MetodoPagoOriginal.Trim();
                    bool encontrado = false;

                    for (int i = 0; i < cbMetodoPago.Items.Count; i++)
                    {
                        var item = cbMetodoPago.Items[i] as ComboBoxItem;
                        if (item != null && (item.Content.ToString().Equals(metodoOriginal, StringComparison.OrdinalIgnoreCase) ||
                            metodoOriginal.Contains(item.Content.ToString()) || item.Content.ToString().Contains(metodoOriginal)))
                        {
                            cbMetodoPago.SelectedIndex = i;
                            encontrado = true;
                            break;
                        }
                    }
                    if (!encontrado) cbMetodoPago.SelectedIndex = 0;

                    cbMetodoPago.UpdateLayout();
                }
            }
            else
            {
                txtClientePedidoCaja.Clear();
                txtTelPedidoCaja.Clear();
                dpFechaPedidoCaja.SelectedDate = null;
                txtFleteNuevoPedido.Text = "0";
                txtPagoCon.Clear();
                if (cbMetodoPago != null) cbMetodoPago.SelectedIndex = 0;
            }
            ActualizarTotal();
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
                ProductoNombre = $"Liquidación / Abono: {ped.ClienteNombre}",
                Total = ped.SaldoPendiente,
                DetalleVisual = "Cobrar saldo remanente de la agenda",
                InsumosADescontar = new List<DetalleInsumo>()
            });

            cbMetodoPago.SelectedIndex = 0;
            chkEsPedidoApartado.IsChecked = false;
            chkEsPedidoApartado.IsEnabled = false;
            txtDescuento.Text = "0";

            ActualizarTotal();
        }

        private void CargarEncargadosCuentasDinamicos()
        {
            try
            {
                if (cbCuentaDestino == null) return;
                cbCuentaDestino.Items.Clear();

                ConexionDB db = new ConexionDB();
                string e1 = db.ObtenerEncargadoCuenta1();

                if (!string.IsNullOrWhiteSpace(e1))
                {
                    string[] listaEncargados = e1.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var encargado in listaEncargados)
                    {
                        if (!string.IsNullOrWhiteSpace(encargado))
                        {
                            cbCuentaDestino.Items.Add(encargado.Trim());
                        }
                    }
                }

                if (cbCuentaDestino.Items.Count == 0)
                {
                    cbCuentaDestino.Items.Add("Encargado 1");
                }

                cbCuentaDestino.SelectedIndex = 0;
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
                            int cat = (int)r["Capacidad"];
                            decimal precio = Convert.ToDecimal(r["Precio"]);
                            preciosDinamicos.Add(cat, precio);
                            ActualizarTextoBoton(cat, precio);
                        }
                    }
                }
            }
            catch { }
        }

        private void ActualizarTextoBoton(int capacity, decimal precio)
        {
            string texto = $"{capacity} pz ({precio:C0})";
            switch (capacity)
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

        // AUXILIAR DE CONTROL SECUNDARIO: Lee existencias directo de la BD antes de meter al carrito
        private int ObtenerStockFisicoReal(string nombreProducto)
        {
            int stock = 0;
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string q = "SELECT ISNULL(StockActual, 0) FROM Productos WHERE Nombre = @nom AND Categoria = 'Venta'";
                    using (SqlCommand cmd = new SqlCommand(q, con))
                    {
                        cmd.Parameters.AddWithValue("@nom", nombreProducto);
                        stock = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch { }
            return stock;
        }

        private void btnAgregarAlRamo_Click(object sender, RoutedEventArgs e)
        {
            var flor = cbInsumosRamos.SelectedItem as Producto;
            if (flor == null || capacityRamo == 0) return;
            if (!int.TryParse(txtCantFlorRamo.Text, out int cant) || cant <= 0) return;
            if (floresAgregadas + cant > capacityRamo) { MessageBox.Show("Superas la capacidad del ramo.", "Límite Ramo"); return; }

            // VALIDACIÓN INYECTADA (Configurador de Ramos)
            int stockReal = ObtenerStockFisicoReal(flor.Nombre);
            int yaAgregadoAlTicket = ProductosEnTicket.SelectMany(x => x.InsumosADescontar).Where(i => i.Nombre == flor.Nombre).Sum(i => i.Cantidad) +
                                     composicionRamoActual.Where(i => i.Nombre == flor.Nombre).Sum(i => i.Cantidad);

            if ((yaAgregadoAlTicket + cant) > stockReal)
            {
                MessageBox.Show($"¡Inventario Insuficiente en Mostrador!\n\nFlor: {flor.Nombre}\nExistencia actual: {stockReal} pz.\nYa comprometido en venta: {yaAgregadoAlTicket} pz.\n\nNo se pueden colocar números negativos en stock.", "Stock Agotado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

            // VALIDACIÓN INYECTADA (Venta de Flores por Unidad / Sueltas)
            int stockReal = ObtenerStockFisicoReal(prod.Nombre);
            int yaAgregadoAlTicket = ProductosEnTicket.SelectMany(x => x.InsumosADescontar).Where(i => i.Nombre == prod.Nombre).Sum(i => i.Cantidad);

            if ((yaAgregadoAlTicket + cant) > stockReal)
            {
                MessageBox.Show($"¡Inventario Insuficiente en Mostrador!\n\nFlor: {prod.Nombre}\nExistencia actual: {stockReal} pz.\nYa en carrito: {yaAgregadoAlTicket} pz.\n\nModifica la cantidad para evitar números negativos.", "Stock Agotado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
            var flor = cbInsumosEspeciales.SelectedItem as Producto;
            if (flor == null) return;
            if (!int.TryParse(txtCantFlorEspecial.Text, out int cant) || cant <= 0) return;

            // VALIDACIÓN INYECTADA (Arreglos Diseños Especiales)
            int stockReal = ObtenerStockFisicoReal(flor.Nombre);
            int yaAgregadoAlTicket = ProductosEnTicket.SelectMany(x => x.InsumosADescontar).Where(i => i.Nombre == flor.Nombre).Sum(i => i.Cantidad) +
                                     composicionEspecialActual.Where(i => i.Nombre == flor.Nombre).Sum(i => i.Cantidad);

            if ((yaAgregadoAlTicket + cant) > stockReal)
            {
                MessageBox.Show($"¡Inventario Insuficiente en Mostrador!\n\nFlor: {flor.Nombre}\nExistencia actual: {stockReal} pz.\nComprometido actualmente: {yaAgregadoAlTicket} pz.\n\nSelecciona una cantidad menor.", "Stock Agotado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            composicionEspecialActual.Add(new DetalleInsumo { Nombre = flor.Nombre, Cantidad = cant });
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
            txtDescuento.IsEnabled = true;
            if (txtFleteNuevoPedido != null) txtFleteNuevoPedido.Text = "0";
            if (cbPedidosAgendaDesplegable != null) cbPedidosAgendaDesplegable.SelectedItem = null;
            ActualizarTotal();
        }

        private void cbMetodoPago_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (panelCuentaTransferencia == null || panelNumeroReferencia == null) return;

            var item = cbMetodoPago.SelectedItem as ComboBoxItem;
            if (item != null && item.Content.ToString().Equals("Transferencia", StringComparison.OrdinalIgnoreCase))
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
            if (txtDescuento != null && float.TryParse(txtDescuento.Text, out float porc))
            {
                if (porc < 0) txtDescuento.Text = "0";
                if (porc > 100) txtDescuento.Text = "100";
            }
            ActualizarTotal();
        }

        private void txtFactorManualRapido_TextChanged(object sender, TextChangedEventArgs e) { }

        private void txtFleteNuevoPedido_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarTotal();
        }

        private void txtFleteManualRapido_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarTotal();
        }

        private void txtPagoCon_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularCambioMatematico();
        }

        private decimal ObtenerTotalConDescuento()
        {
            decimal subtotal = ProductosEnTicket.Sum(x => x.Total);
            decimal fleteTotalAcumulado = 0;

            if (chkEsPedidoApartado != null && chkEsPedidoApartado.IsChecked == true && txtFleteNuevoPedido != null)
            {
                decimal.TryParse(txtFleteNuevoPedido.Text.Trim(), out decimal fleteAgenda);
                fleteTotalAcumulado += fleteAgenda;
            }

            if (txtFleteManualRapido != null && decimal.TryParse(txtFleteManualRapido.Text.Trim(), out decimal fleteManual))
            {
                if (fleteManual > 0) fleteTotalAcumulado += fleteManual;
            }

            decimal dineroDescontated = 0;
            if (txtDescuento != null && float.TryParse(txtDescuento.Text, out float porcentaje) && porcentaje > 0)
            {
                dineroDescontated = subtotal * (decimal)(porcentaje / 100.0);
            }

            return (subtotal - dineroDescontated) + fleteTotalAcumulado;
        }

        private void CalcularCambioMatematico()
        {
            if (txtCambio == null || txtPagoCon == null) return;

            decimal totalNeto = ObtenerTotalConDescuento();
            var itemPago = cbMetodoPago.SelectedItem as ComboBoxItem;
            string metodo = itemPago != null ? itemPago.Content.ToString() : "Efectivo";

            if (metodo != "Efectivo")
            {
                if (chkEsPedidoApartado != null && chkEsPedidoApartado.IsChecked == true)
                {
                    txtCambio.Text = "$0.00";
                }
                else
                {
                    txtPagoCon.Text = totalNeto.ToString("F2");
                    txtCambio.Text = "$0.00";
                }
                return;
            }

            if (decimal.TryParse(txtPagoCon.Text.Trim(), out decimal pago))
            {
                if (chkEsPedidoApartado != null && chkEsPedidoApartado.IsChecked == true)
                {
                    txtCambio.Text = (pago > totalNeto) ? (pago - totalNeto).ToString("C") : "$0.00";
                }
                else
                {
                    txtCambio.Text = (pago >= totalNeto) ? (pago - totalNeto).ToString("C") : "$0.00";
                }
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

            if (!decimal.TryParse(txtPagoCon.Text.Trim(), out decimal pagoRecibido) || pagoRecibido < 0)
            {
                MessageBox.Show("Por favor, introduce el monto recibido o anticipo válido.", "Cobro Detenido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (chkEsPedidoApartado.IsChecked == false && esCobroDeAbonoExistente == false)
            {
                if (pagoRecibido < totalNetoArreglo)
                {
                    MessageBox.Show($"La cantidad introducida ({pagoRecibido:C}) es menor al costo total del arreglo ({totalNetoArreglo:C}). Para ventas directas de mostrador se debe liquidar el monto completo.", "Cobro Insuficiente", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            float porcText = 0;
            float.TryParse(txtDescuento.Text, out porcText);

            decimal totalDineroDescontado = subtotalBase - (totalNetoArreglo - (chkEsPedidoApartado.IsChecked == true ? ProductosEnTicket.Sum(x => x.Total) : 0));
            if (txtFleteManualRapido != null && decimal.TryParse(txtFleteManualRapido.Text.Trim(), out decimal fManual) && fManual > 0)
            {
                totalDineroDescontado += fManual;
            }

            if (totalDineroDescontado < 0 || chkEsPedidoApartado.IsChecked == true || esCobroDeAbonoExistente)
            {
                if (porcText > 0) totalDineroDescontado = subtotalBase * (decimal)(porcText / 100.0);
                else totalDineroDescontado = 0;
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

            PedidoComboClass pedidoEnlazadoCombo = cbPedidosAgendaDesplegable.SelectedItem as PedidoComboClass;
            decimal cambioFinal = (metodoPago == "Efectivo" && pagoRecibido > totalNetoArreglo) ? (pagoRecibido - totalNetoArreglo) : 0;

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
                            // =========================================================================
                            // BLINDAJE TRANSACCIONAL INYECTADO: Doble verificación antes de descontar stock
                            // =========================================================================
                            if (!esCobroDeAbonoExistente)
                            {
                                // Consolidamos los totales requeridos por tipo de flor en el carrito actual
                                var totalesRequeridos = ProductosEnTicket
                                    .SelectMany(x => x.InsumosADescontar)
                                    .GroupBy(i => i.Nombre)
                                    .Select(g => new { Nombre = g.Key, CantidadRequerida = g.Sum(i => i.Quantity ?? i.Cantidad) });

                                foreach (var req in totalesRequeridos)
                                {
                                    // Consultamos directo en la transacción con bloqueo para evitar colisiones de red
                                    string qCheck = "SELECT ISNULL(StockActual, 0) FROM Productos WITH (UPDLOCK) WHERE Nombre = @nom AND Categoria = 'Venta'";
                                    int stockTransaccional = 0;
                                    using (SqlCommand cmdCheck = new SqlCommand(qCheck, con, tra))
                                    {
                                        cmdCheck.Parameters.AddWithValue("@nom", req.Nombre);
                                        stockTransaccional = Convert.ToInt32(cmdCheck.ExecuteScalar());
                                    }

                                    if (req.CantidadRequerida > stockTransaccional)
                                    {
                                        // Si otra caja nos ganó el producto y bajó de lo requerido, aborta el cobro inmediatamente
                                        throw new Exception($"¡Venta Cancelada por falta de Stock simultáneo!\n\nLa flor '{req.Nombre}' ya fue vendida en otra caja. Existencia real: {stockTransaccional} pz. Requerido en nota: {req.CantidadRequerida} pz.");
                                    }
                                }
                            }
                            // =========================================================================

                            string detProdNombres = string.Join(", ", ProductosEnTicket.Select(x => x.ProductoNombre));
                            string detVisualConcat = string.Join(" | ", ProductosEnTicket.Select(x => x.DetalleVisual));

                            if (txtFleteManualRapido != null && decimal.TryParse(txtFleteManualRapido.Text.Trim(), out decimal fExt) && fExt > 0)
                            {
                                detVisualConcat += $" | Con Envío Rápido de: {fExt:C}";
                            }

                            int? firstProductoIdDB = null;
                            int totalPiezasContadas = 1;

                            var primerItemCarrito = ProductosEnTicket.FirstOrDefault();
                            if (primerItemCarrito != null)
                            {
                                var primerInsumo = primerItemCarrito.InsumosADescontar.FirstOrDefault();
                                if (primerInsumo != null)
                                {
                                    totalPiezasContadas = primerInsumo.Quantity ?? primerInsumo.Cantidad;
                                    if (totalPiezasContadas <= 0) totalPiezasContadas = 1;

                                    string qFindId = "SELECT TOP 1 Id FROM Productos WHERE Nombre = @fn AND Categoria = 'Venta'";
                                    using (SqlCommand cmdFind = new SqlCommand(qFindId, con, tra))
                                    {
                                        cmdFind.Parameters.AddWithValue("@fn", primerInsumo.Nombre);
                                        object resId = cmdFind.ExecuteScalar();
                                        if (resId != null && resId != DBNull.Value) firstProductoIdDB = Convert.ToInt32(resId);
                                    }
                                }
                            }

                            if (esCobroDeAbonoExistente)
                            {
                                string actPedido = @"UPDATE Pedidos 
                                                     SET SaldoPendiente = CASE WHEN (SaldoPendiente - @abono) <= 0 THEN 0 ELSE (SaldoPendiente - @abono) END, 
                                                         Estado = CASE WHEN (SaldoPendiente - @abono) <= 0 THEN 'Entregado' ELSE 'Pendiente' END 
                                                     WHERE Id = @id";
                                using (SqlCommand cmdP = new SqlCommand(actPedido, con, tra))
                                {
                                    cmdP.Parameters.AddWithValue("@abono", totalNetoArreglo);
                                    cmdP.Parameters.AddWithValue("@id", idPedidoParaAbonar);
                                    cmdP.ExecuteNonQuery();
                                }

                                string nombreConceptoVenta = $"Liquidación / Abono: {txtClientePedidoCaja.Text.Trim()}";
                                string qV = @"INSERT INTO Ventas (Fecha, ProductoNombre, Total, Cantidad, MetodoPago, MontoRecibido, MontoCambio, CuentaTransferencia, DescuentoAplicado, NumeroReferencia, ProductoId) 
                                           VALUES (GETDATE(), @n, @t, @cant, @metodo, @rec, @cam, @cuenta, @desc, @numRef, @pId)";
                                using (SqlCommand cmdV = new SqlCommand(qV, con, tra))
                                {
                                    cmdV.Parameters.AddWithValue("@n", nombreConceptoVenta);
                                    cmdV.Parameters.AddWithValue("@t", totalNetoArreglo);
                                    cmdV.Parameters.AddWithValue("@cant", totalPiezasContadas);
                                    cmdV.Parameters.AddWithValue("@metodo", metodoPago);
                                    cmdV.Parameters.AddWithValue("@rec", pagoRecibido);
                                    cmdV.Parameters.AddWithValue("@cam", cambioFinal);
                                    cmdV.Parameters.AddWithValue("@cuenta", cuentaDestino);
                                    cmdV.Parameters.AddWithValue("@desc", totalDineroDescontado);
                                    cmdV.Parameters.AddWithValue("@numRef", numeroRefValor);
                                    cmdV.Parameters.AddWithValue("@pId", (object)firstProductoIdDB ?? DBNull.Value);
                                    cmdV.ExecuteNonQuery();
                                }
                            }
                            else if (chkEsPedidoApartado.IsChecked == true && pedidoEnlazadoCombo != null)
                            {
                                decimal abonoRealEfectivo = pagoRecibido - cambioFinal;
                                decimal saldoRestanteCalculado = totalNetoArreglo - abonoRealEfectivo;
                                string estadoFinalCalculado = (saldoRestanteCalculado <= 0) ? "Entregado" : "Pendiente";

                                string queryActualizarExistente = @"UPDATE Pedidos 
                                                                    SET Descripcion = @des,
                                                                        PrecioTotal = @tot,
                                                                        SaldoPendiente = CASE WHEN @saldoCalc <= 0 THEN 0 ELSE @saldoCalc END,
                                                                        Estado = @estFinal
                                                                    WHERE Id = @id";

                                using (SqlCommand cmdUp = new SqlCommand(queryActualizarExistente, con, tra))
                                {
                                    cmdUp.Parameters.AddWithValue("@id", pedidoEnlazadoCombo.Id);
                                    cmdUp.Parameters.AddWithValue("@des", detProdNombres + " (" + detVisualConcat + ")");
                                    cmdUp.Parameters.AddWithValue("@tot", totalNetoArreglo);
                                    cmdUp.Parameters.AddWithValue("@saldoCalc", saldoRestanteCalculado);
                                    cmdUp.Parameters.AddWithValue("@estFinal", estadoFinalCalculado);
                                    cmdUp.ExecuteNonQuery();
                                }

                                string conceptoAnticipo = $"Liquidación Pedido: {pedidoEnlazadoCombo.ClienteNombre} ({detProdNombres})";
                                string qAnt = @"INSERT INTO Ventas (Fecha, ProductoNombre, Total, Cantidad, MetodoPago, MontoRecibido, MontoCambio, CuentaTransferencia, DescuentoAplicado, NumeroReferencia, ProductoId) 
                                           VALUES (GETDATE(), @n, @t, @cant, @metodo, @rec, @cam, @cuenta, @desc, @numRef, @pId)";
                                using (SqlCommand cmdAnt = new SqlCommand(qAnt, con, tra))
                                {
                                    cmdAnt.Parameters.AddWithValue("@n", conceptoAnticipo);
                                    cmdAnt.Parameters.AddWithValue("@t", abonoRealEfectivo);
                                    cmdAnt.Parameters.AddWithValue("@cant", totalPiezasContadas);
                                    cmdAnt.Parameters.AddWithValue("@metodo", metodoPago);
                                    cmdAnt.Parameters.AddWithValue("@rec", pagoRecibido);
                                    cmdAnt.Parameters.AddWithValue("@cam", cambioFinal);
                                    cmdAnt.Parameters.AddWithValue("@cuenta", cuentaDestino);
                                    cmdAnt.Parameters.AddWithValue("@desc", totalDineroDescontado);
                                    cmdAnt.Parameters.AddWithValue("@numRef", numeroRefValor);
                                    cmdAnt.Parameters.AddWithValue("@pId", (object)firstProductoIdDB ?? DBNull.Value);
                                    cmdAnt.ExecuteNonQuery();
                                }
                            }
                            else if (chkEsPedidoApartado.IsChecked == true && pedidoEnlazadoCombo != null) { } // Removido duplicado pasivo
                            else if (chkEsPedidoApartado.IsChecked == true && pedidoEnlazadoCombo == null)
                            {
                                decimal.TryParse(txtFleteNuevoPedido.Text.Trim(), out decimal fleteNeto);
                                decimal saldoPendienteNuevo = totalNetoArreglo - pagoRecibido;
                                string estadoNuevoCalculado = (saldoPendienteNuevo <= 0) ? "Entregado" : "Pendiente";

                                string insPed = @"INSERT INTO Pedidos (ClienteNombre, Telefono, FechaEntrega, FechaRegistro, Direccion, NotaTarjeta, Estado, Descripcion, PrecioTotal, Anticipo, SaldoPendiente, MetodoPago, CostoEnvio) 
                                                 VALUES (@nom, @tel, @fec, GETDATE(), 'Mostrador Caja', '', @estFinal, @des, @tot, @ant, @saldo, @met, @flete)";
                                using (SqlCommand cmdPed = new SqlCommand(insPed, con, tra))
                                {
                                    cmdPed.Parameters.AddWithValue("@nom", txtClientePedidoCaja.Text.Trim());
                                    cmdPed.Parameters.AddWithValue("@tel", txtTelPedidoCaja.Text.Trim());
                                    cmdPed.Parameters.AddWithValue("@fec", dpFechaPedidoCaja.SelectedDate.HasValue ? dpFechaPedidoCaja.SelectedDate.Value : DateTime.Now.AddDays(1));
                                    cmdPed.Parameters.AddWithValue("@des", detProdNombres + " (" + detVisualConcat + ")");
                                    cmdPed.Parameters.AddWithValue("@tot", totalNetoArreglo);
                                    cmdPed.Parameters.AddWithValue("@ant", pagoRecibido);
                                    cmdPed.Parameters.AddWithValue("@saldo", saldoPendienteNuevo <= 0 ? 0 : saldoPendienteNuevo);
                                    cmdPed.Parameters.AddWithValue("@estFinal", estadoNuevoCalculado);
                                    cmdPed.Parameters.AddWithValue("@met", metodoPago);
                                    cmdPed.Parameters.AddWithValue("@flete", fleteNeto);
                                    cmdPed.ExecuteNonQuery();
                                }

                                string nombreConceptoVenta = $"Abono/Anticipo Pedido: {txtClientePedidoCaja.Text.Trim()} ({detProdNombres})";
                                string qV = @"INSERT INTO Ventas (Fecha, ProductoNombre, Total, Cantidad, MetodoPago, MontoRecibido, MontoCambio, CuentaTransferencia, DescuentoAplicado, NumeroReferencia, ProductoId) 
                                           VALUES (GETDATE(), @n, @t, @cant, @metodo, @rec, @cam, @cuenta, @desc, @numRef, @pId)";
                                using (SqlCommand cmdV = new SqlCommand(qV, con, tra))
                                {
                                    cmdV.Parameters.AddWithValue("@n", nombreConceptoVenta);
                                    cmdV.Parameters.AddWithValue("@t", pagoRecibido);
                                    cmdV.Parameters.AddWithValue("@cant", totalPiezasContadas);
                                    cmdV.Parameters.AddWithValue("@metodo", metodoPago);
                                    cmdV.Parameters.AddWithValue("@rec", pagoRecibido);
                                    cmdV.Parameters.AddWithValue("@cam", cambioFinal);
                                    cmdV.Parameters.AddWithValue("@cuenta", cuentaDestino);
                                    cmdV.Parameters.AddWithValue("@desc", totalDineroDescontado);
                                    cmdV.Parameters.AddWithValue("@numRef", numeroRefValor);
                                    cmdV.Parameters.AddWithValue("@pId", (object)firstProductoIdDB ?? DBNull.Value);
                                    cmdV.ExecuteNonQuery();
                                }
                            }
                            else if (chkEsPedidoApartado.IsChecked == false && esCobroDeAbonoExistente == false)
                            {
                                string nombreConceptoVenta = $"Venta Mostrador: {txtClientePedidoCaja.Text.Trim()} ({detProdNombres})";

                                string qV = @"INSERT INTO Ventas (Fecha, ProductoNombre, Total, Cantidad, MetodoPago, MontoRecibido, MontoCambio, CuentaTransferencia, DescuentoAplicado, NumeroReferencia, ProductoId) 
                                           VALUES (GETDATE(), @n, @t, @cant, @metodo, @rec, @cam, @cuenta, @desc, @numRef, @pId)";
                                using (SqlCommand cmdV = new SqlCommand(qV, con, tra))
                                {
                                    cmdV.Parameters.AddWithValue("@n", nombreConceptoVenta);
                                    cmdV.Parameters.AddWithValue("@t", totalNetoArreglo);
                                    cmdV.Parameters.AddWithValue("@cant", totalPiezasContadas);
                                    cmdV.Parameters.AddWithValue("@metodo", metodoPago);
                                    cmdV.Parameters.AddWithValue("@rec", pagoRecibido);
                                    cmdV.Parameters.AddWithValue("@cam", cambioFinal);
                                    cmdV.Parameters.AddWithValue("@cuenta", cuentaDestino);
                                    cmdV.Parameters.AddWithValue("@desc", totalDineroDescontado);
                                    cmdV.Parameters.AddWithValue("@numRef", numeroRefValor);
                                    cmdV.Parameters.AddWithValue("@pId", (object)firstProductoIdDB ?? DBNull.Value);
                                    cmdV.ExecuteNonQuery();
                                }
                            }

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

                            ProductosEnTicket.Clear();
                            txtPagoCon.Clear();
                            txtDescuento.Text = "0";
                            if (txtFleteManualRapido != null) txtFleteManualRapido.Text = "0";
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
                            MessageBox.Show("Transacción revertida por integridad: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Stop);
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
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
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
            g.DrawString($"TOTAL EN OPERACIÓN: {ticketTotal:C}", fontBold, brush, 5, y); y += 15;
            g.DrawString($"EFECTIVO DISPUESTO: {ticketPagado:C}", fontNormal, brush, 5, y); y += 15;
            g.DrawString($"CAMBIO ENTREGADO: {ticketCambio:C}", fontBold, brush, 5, y); y += 25;

            g.DrawString("¡Gracias por su preferencia!", fontBold, brush, new DgRectangle(0, y, 220, 15), new DgStringFormat { Alignment = DgAlignment.Center });
        }

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
            txtTotal.Text = $"Total del Arreglo: {totalNeto:C}";
            CalcularCambioMatematico();
        }

        private void btnLimpiarTicket_Click(object sender, RoutedEventArgs e)
        {
            ProductosEnTicket.Clear();
            txtDescuento.Text = "0";
            if (txtFleteManualRapido != null) txtFleteManualRapido.Text = "0";
            chkEsPedidoApartado.IsChecked = false;
            chkEsPedidoApartado.IsEnabled = true;
            txtDescuento.IsEnabled = true;
            esCobroDeAbonoExistente = false;
            idPedidoParaAbonar = 0;
            ActualizarTotal();
        }

        private void btnItemEliminar_Click(object sender, RoutedEventArgs e) { }

        private void btnEliminarItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).DataContext as ItemTicket;
            if (item != null) { ProductosEnTicket.Remove(item); ActualizarTotal(); }
        }
    }
}