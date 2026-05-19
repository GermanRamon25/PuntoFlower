using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;
using PuntoFlower.Data;
using PuntoFlower.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Drawing.Printing; // Nota la P mayúscula en Printing
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DgAlignment = System.Drawing.StringAlignment;
using DgBrush = System.Drawing.SolidBrush;
using DgColor = System.Drawing.Color;
// 2. ALIAS PARA SOLUCIONAR AMBIGÜEDADES DE DIBUJO E IMPRESIÓN TÉRMICA (CORREGIDA LA 'P' MAYÚSCULA)
using DgFont = System.Drawing.Font;
using DgGraphics = System.Drawing.Graphics;
using DgRectangle = System.Drawing.RectangleF;
using DgStringFormat = System.Drawing.StringFormat;
using DgStyle = System.Drawing.FontStyle;
using iTextDocument = iTextSharp.text.Document;
// 1. ALIAS DE ITEXTSHARP (Para la lógica de exportación si se llega a requerir)
using iTextFont = iTextSharp.text.Font;
using iTextParagraph = iTextSharp.text.Paragraph;

namespace PuntoFlower.Views
{
    public partial class SalesView : UserControl
    {
        public ObservableCollection<ItemTicket> ProductosEnTicket { get; set; }
        private List<DetalleInsumo> composicionRamoActual = new List<DetalleInsumo>();
        private int capacidadRamo = 0;
        private decimal precioRamo = 0;
        private int floresAgregadas = 0;
        private Dictionary<int, decimal> preciosDinamicos = new Dictionary<int, decimal>();

        // Variables temporales para el hilo de impresión física
        private List<ItemTicket> productosParaImprimir = new List<ItemTicket>();
        private decimal ticketTotal = 0;
        private decimal ticketPagado = 0;
        private decimal ticketCambio = 0;

        public SalesView()
        {
            InitializeComponent();
            ProductosEnTicket = new ObservableCollection<ItemTicket>();
            lstVenta.ItemsSource = ProductosEnTicket;
            CargarPreciosDesdeDB();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarInsumos();
            CargarPreciosDesdeDB();
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
                    SqlCommand cmd = new SqlCommand("SELECT Nombre, PrecioVenta, RutaImagen FROM Productos WHERE StockActual > 0", con);
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
            }
            catch { }
        }

        private void Ramo_Checked(object sender, RoutedEventArgs e)
        {
            var rb = sender as RadioButton;
            if (rb == null || rb.Tag == null) return;

            capacidadRamo = int.Parse(rb.Tag.ToString());
            precioRamo = preciosDinamicos.ContainsKey(capacidadRamo) ? preciosDinamicos[capacidadRamo] : 0;

            ActualizarProgreso();
        }

        private void btnAgregarAlRamo_Click(object sender, RoutedEventArgs e)
        {
            var flor = cbInsumosRamos.SelectedItem as Producto;
            if (flor == null || capacidadRamo == 0) return;
            if (!int.TryParse(txtCantFlorRamo.Text, out int cant) || cant <= 0) return;
            if (floresAgregadas + cant > capacidadRamo) { MessageBox.Show("Superas la capacidad del ramo."); return; }

            composicionRamoActual.Add(new DetalleInsumo { Nombre = flor.Nombre, Cantidad = cant });
            floresAgregadas += cant;
            ActualizarProgreso();
            txtCantFlorRamo.Text = "0";
            cbInsumosRamos.SelectedItem = null;
        }

        private void btnFinalizarRamo_Click(object sender, RoutedEventArgs e)
        {
            if (floresAgregadas != capacidadRamo) { MessageBox.Show("Debes completar la cantidad de flores seleccionada."); return; }

            ProductosEnTicket.Add(new ItemTicket
            {
                ProductoNombre = $"Ramo de {capacidadRamo} pz",
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

        private void btnAgregarEspecial_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtNombreEspecial.Text) || !decimal.TryParse(txtPrecioEspecial.Text, out decimal precio)) return;

            ProductosEnTicket.Add(new ItemTicket
            {
                ProductoNombre = txtNombreEspecial.Text,
                Total = precio,
                InsumosADescontar = new List<DetalleInsumo>(),
                DetalleVisual = "Arreglo Personalizado / Servicio Especial"
            });
            txtNombreEspecial.Clear(); txtPrecioEspecial.Clear();
            ActualizarTotal();
        }

        private void btnConfirmarVenta_Click(object sender, RoutedEventArgs e)
        {
            decimal total = ProductosEnTicket.Sum(x => x.Total);
            if (total <= 0) return;

            if (!decimal.TryParse(txtPagoCon.Text, out decimal pagoRecibido) || pagoRecibido < total)
            {
                MessageBox.Show("Monto recibido insuficiente.", "Cobro Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal cambioFinal = pagoRecibido - total;
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
                            foreach (var item in ProductosEnTicket)
                            {
                                string q = "INSERT INTO Ventas (Fecha, ProductoNombre, Total, Cantidad, MetodoPago, MontoRecibido, MontoCambio) " +
                                           "VALUES (GETDATE(), @n, @t, 1, 'Efectivo', @rec, @cam)";
                                using (SqlCommand cmdV = new SqlCommand(q, con, tra))
                                {
                                    cmdV.Parameters.AddWithValue("@n", item.ProductoNombre);
                                    cmdV.Parameters.AddWithValue("@t", item.Total);
                                    cmdV.Parameters.AddWithValue("@rec", pagoRecibido);
                                    cmdV.Parameters.AddWithValue("@cam", cambioFinal);
                                    cmdV.ExecuteNonQuery();
                                }

                                foreach (var insumo in item.InsumosADescontar)
                                {
                                    using (SqlCommand cmdS = new SqlCommand("UPDATE Productos SET StockActual = StockActual - @c WHERE Nombre = @nom", con, tra))
                                    {
                                        cmdS.Parameters.AddWithValue("@c", insumo.Cantidad);
                                        cmdS.Parameters.AddWithValue("@nom", insumo.Nombre);
                                        cmdS.ExecuteNonQuery();
                                    }
                                }
                            }

                            tra.Commit();

                            // Respaldamos los datos para mandarlos a la mini-printer antes de borrar la lista visual
                            productosParaImprimir = ProductosEnTicket.ToList();
                            ticketTotal = total;
                            ticketPagado = pagoRecibido;
                            ticketCambio = cambioFinal;

                            MessageBoxResult result = MessageBox.Show("Venta registrada con éxito.\n\n¿Deseas imprimir el ticket en la máquina física?", "Venta Exitosa", MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                ImprimirTicketTermico();
                            }

                            ProductosEnTicket.Clear();
                            txtPagoCon.Clear();
                            txtCambio.Text = "$0.00";
                            ActualizarTotal();
                        }
                        catch (Exception ex)
                        {
                            tra.Rollback();
                            MessageBox.Show("Error interno en la base de datos local. Venta revertida: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception exCon)
            {
                MessageBox.Show("Error de enlace local: " + exCon.Message, "Fallo de Servidor");
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
                MessageBox.Show("No se detectó una impresora térmica activa o lista: " + ex.Message, "Fallo de Impresión", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        // Subrutina encargada del dibujo utilizando los alias corregidos (DgFont, DgColor, DgBrush, etc.)
        private void DrawTicketPage(object sender, PrintPageEventArgs e)
        {
            ConexionDB db = new ConexionDB();
            string sucursal = db.ObtenerNombreSucursal();

            DgGraphics g = e.Graphics;

            // Usamos alias definidos arriba para resolver la ambigüedad con iTextSharp y WPF
            DgFont fontTitulo = new DgFont("Arial", 11, DgStyle.Bold);
            DgFont fontBold = new DgFont("Arial", 8, DgStyle.Bold);
            DgFont fontNormal = new DgFont("Arial", 8, DgStyle.Regular);

            DgBrush brush = new DgBrush(DgColor.Black);

            float y = 10;

            g.DrawString("🌸 PUNTO FLOWER 🌸", fontTitulo, brush, new DgRectangle(0, y, 220, 20), new DgStringFormat { Alignment = DgAlignment.Center });
            y += 20;

            // CORREGIDO: Se agregó de forma explícita el alias DgStringFormat para limpiar el error de compilación
            g.DrawString(sucursal, fontBold, brush, new DgRectangle(0, y, 220, 15), new DgStringFormat { Alignment = DgAlignment.Center });
            y += 20;

            g.DrawString($"Fecha: {DateTime.Now:g}", fontNormal, brush, 5, y);
            y += 15;
            g.DrawString($"Atendió: {Session.UsuarioActual}", fontNormal, brush, 5, y);
            y += 15;
            g.DrawString("==================================", fontNormal, brush, 5, y);
            y += 15;

            foreach (var item in productosParaImprimir)
            {
                g.DrawString(item.ProductoNombre, fontBold, brush, 5, y);
                y += 13;
                g.DrawString($"   {item.DetalleVisual}", fontNormal, brush, 5, y);
                y += 13;
                g.DrawString($"   Total: {item.Total:C}", fontNormal, brush, 5, y);
                y += 15;
            }

            g.DrawString("==================================", fontNormal, brush, 5, y);
            y += 15;

            g.DrawString($"TOTAL COMPRA: {ticketTotal:C}", fontBold, brush, 5, y);
            y += 15;
            g.DrawString($"RECIBIDO: {ticketPagado:C}", fontNormal, brush, 5, y);
            y += 15;
            g.DrawString($"CAMBIO: {ticketCambio:C}", fontBold, brush, 5, y);
            y += 25;

            g.DrawString("¡Gracias por su preferencia!", fontBold, brush, new DgRectangle(0, y, 220, 15), new DgStringFormat { Alignment = DgAlignment.Center });
        }

        private void txtPagoCon_TextChanged(object sender, TextChangedEventArgs e)
        {
            decimal total = ProductosEnTicket.Sum(x => x.Total);
            if (decimal.TryParse(txtPagoCon.Text, out decimal pago))
            {
                decimal cambio = pago - total;
                txtCambio.Text = (cambio >= 0) ? cambio.ToString("C") : "$0.00";
            }
            else { txtCambio.Text = "$0.00"; }
        }

        private void LimpiarConfiguradorRamo()
        {
            composicionRamoActual.Clear(); floresAgregadas = 0; capacidadRamo = 0;

            rbRamo6.IsChecked = rbRamo12.IsChecked = rbRamo24.IsChecked = rbRamo36.IsChecked = rbRamo50.IsChecked =
            rbRamo72.IsChecked = rbRamo100.IsChecked = rbRamo150.IsChecked = rbRamo200.IsChecked = rbRamo250.IsChecked = false;

            ActualizarTotal(); ActualizarProgreso();
        }

        private void ActualizarProgreso() => lblProgresoRamo.Text = $"Seleccionadas: {floresAgregadas} / {capacidadRamo}";
        private void ActualizarTotal() => txtTotal.Text = $"Total: {ProductosEnTicket.Sum(x => x.Total):C}";
        private void btnLimpiarTicket_Click(object sender, RoutedEventArgs e) { ProductosEnTicket.Clear(); ActualizarTotal(); }

        private void btnEliminarItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).DataContext as ItemTicket;
            if (item != null) { ProductosEnTicket.Remove(item); ActualizarTotal(); }
        }

        public class ItemTicket { public string ProductoNombre { get; set; } public decimal Total { get; set; } public string DetalleVisual { get; set; } public List<DetalleInsumo> InsumosADescontar { get; set; } }
        public class DetalleInsumo { public string Nombre { get; set; } public int Cantidad { get; set; } }
    }
}