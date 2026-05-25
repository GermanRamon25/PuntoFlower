using PuntoFlower.Data;
using PuntoFlower.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

// Alias de iTextSharp para control estricto de fuentes y elementos
using iTextFont = iTextSharp.text.Font;
using iTextParagraph = iTextSharp.text.Paragraph;
using iTextDocument = iTextSharp.text.Document;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace PuntoFlower.Views
{
    public partial class InventoryView : UserControl
    {
        public InventoryView()
        {
            InitializeComponent();
            CargarDesdeSQL();

            this.IsVisibleChanged += (s, e) => {
                if ((bool)e.NewValue) CargarDesdeSQL();
            };
        }

        private void CargarDesdeSQL(string filtro = "")
        {
            List<Producto> listaVenta = new List<Producto>();
            List<Producto> listaBodega = new List<Producto>();
            ConexionDB db = new ConexionDB();

            try
            {
                using (SqlConnection conexion = db.OpenConnection())
                {
                    string query = "SELECT * FROM Productos";
                    if (!string.IsNullOrEmpty(filtro))
                        query += " WHERE Nombre LIKE @buscar OR Categoria LIKE @buscar";

                    using (SqlCommand comando = new SqlCommand(query, conexion))
                    {
                        if (!string.IsNullOrEmpty(filtro))
                            comando.Parameters.AddWithValue("@buscar", "%" + filtro + "%");

                        using (SqlDataReader reader = comando.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Producto prod = new Producto
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Nombre = reader["Nombre"].ToString(),
                                    Categoria = reader["Categoria"].ToString(),
                                    TipoVenta = reader["TipoVenta"].ToString(),
                                    StockActual = Convert.ToInt32(reader["StockActual"]),
                                    StockMinimo = Convert.ToInt32(reader["StockMinimo"]),
                                    PrecioCompra = Convert.ToDecimal(reader["PrecioCompra"]),
                                    PrecioVenta = Convert.ToDecimal(reader["PrecioVenta"]),
                                    RutaImagen = reader["RutaImagen"] != DBNull.Value ? reader["RutaImagen"].ToString() : ""
                                };

                                if (prod.Categoria == "Venta")
                                    listaVenta.Add(prod);
                                else if (prod.Categoria == "Bodega")
                                    listaBodega.Add(prod);
                            }
                        }
                    }
                }

                dgInventarioVenta.ItemsSource = null;
                dgInventarioVenta.ItemsSource = listaVenta;

                dgInventarioBodega.ItemsSource = null;
                dgInventarioBodega.ItemsSource = listaBodega;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al sincronizar las tablas de inventario segmentado: " + ex.Message, "Fallo de Enlace");
            }
        }

        private Producto ObtenerProductoSeleccionado()
        {
            if (tcInventarios.SelectedIndex == 0)
                return dgInventarioVenta.SelectedItem as Producto;
            else
                return dgInventarioBodega.SelectedItem as Producto;
        }

        private void btnMerma_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = ObtenerProductoSeleccionado();
            if (seleccionado == null)
            {
                MessageBox.Show("Por favor, selecciona una flor de la lista activa para registrar la merma.", "Atención");
                return;
            }

            string cantidadStr = Microsoft.VisualBasic.Interaction.InputBox(
                $"¿Cuántas unidades de '{seleccionado.Nombre}' ({seleccionado.Categoria}) se perdieron?", "Registro de Merma", "1");

            if (string.IsNullOrEmpty(cantidadStr)) return;

            if (int.TryParse(cantidadStr, out int cantBaja) && cantBaja > 0)
            {
                if (cantBaja > seleccionado.StockActual)
                {
                    MessageBox.Show("La cantidad de merma no puede superar el stock actual de esta área.", "Error");
                    return;
                }

                string motivo = Microsoft.VisualBasic.Interaction.InputBox(
                    "Motivo (Marchita, Tallo Roto, etc.):", "Motivo", "Marchita");

                ConexionDB db = new ConexionDB();
                try
                {
                    using (SqlConnection con = db.OpenConnection())
                    {
                        string qUpdate = "UPDATE Productos SET StockActual = StockActual - @cant WHERE Id = @id";
                        SqlCommand cmdUp = new SqlCommand(qUpdate, con);
                        cmdUp.Parameters.AddWithValue("@cant", cantBaja);
                        cmdUp.Parameters.AddWithValue("@id", seleccionado.Id);
                        cmdUp.ExecuteNonQuery();

                        string qInsert = "INSERT INTO Mermas (ProductoNombre, Cantidad, Motivo, Fecha) VALUES (@nom, @cant, @mot, GETDATE())";
                        SqlCommand cmdIn = new SqlCommand(qInsert, con);
                        cmdIn.Parameters.AddWithValue("@nom", $"{seleccionado.Nombre} ({seleccionado.Categoria})");
                        cmdIn.Parameters.AddWithValue("@cant", cantBaja);
                        cmdIn.Parameters.AddWithValue("@mot", motivo);
                        cmdIn.ExecuteNonQuery();
                    }
                    MessageBox.Show("Inventario actualizado. Merma registrada en el historial.");
                    CargarDesdeSQL();
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void btnSurtirStock_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = ObtenerProductoSeleccionado();
            if (seleccionado != null)
            {
                PuntoFlower.Views.SurtirStockWindow ventanaSurtir = new PuntoFlower.Views.SurtirStockWindow(seleccionado.Nombre);
                ventanaSurtir.Owner = Window.GetWindow(this);
                if (ventanaSurtir.ShowDialog() == true) CargarDesdeSQL();
            }
            else
            {
                MessageBox.Show("Selecciona una flor de cualquiera de las dos tablas para gestionar su traspaso interno.", "Atención");
            }
        }

        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = ObtenerProductoSeleccionado();
            if (seleccionado == null) return;

            var result = MessageBox.Show($"¿Deseas eliminar '{seleccionado.Nombre}' ({seleccionado.Categoria}) del catálogo permanentemente?", "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                ConexionDB db = new ConexionDB();
                try
                {
                    using (SqlConnection conexion = db.OpenConnection())
                    {
                        string query = "DELETE FROM Productos WHERE Id = @id";
                        SqlCommand cmd = new SqlCommand(query, conexion);
                        cmd.Parameters.AddWithValue("@id", seleccionado.Id);
                        cmd.ExecuteNonQuery();
                    }
                    CargarDesdeSQL();
                }
                catch (Exception) { MessageBox.Show("No se puede eliminar porque tiene historial de movimientos vinculados."); }
            }
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e) => CargarDesdeSQL(txtSearch.Text);
        private void txtSearch_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) CargarDesdeSQL(txtSearch.Text); }
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) { if (string.IsNullOrEmpty(txtSearch.Text)) CargarDesdeSQL(); }

        private void btnNuevaFlor_Click(object sender, RoutedEventArgs e)
        {
            NuevoProductoWindow ventana = new NuevoProductoWindow();
            ventana.Owner = Window.GetWindow(this);
            if (ventana.ShowDialog() == true) CargarDesdeSQL();
        }

        // NUEVO MÉTODO: Compila de forma matemática el PDF Ejecutivo de Inventario General
        private void btnReporte_Click(object sender, RoutedEventArgs e)
        {
            var itemsVenta = dgInventarioVenta.ItemsSource as List<Producto> ?? new List<Producto>();
            var itemsBodega = dgInventarioBodega.ItemsSource as List<Producto> ?? new List<Producto>();

            if (!itemsVenta.Any() && !itemsBodega.Any())
            {
                MessageBox.Show("No hay datos en el inventario actual para exportar un reporte.", "Aviso");
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PDF Files (*.pdf)|*.pdf";
            sfd.FileName = $"Reporte_Inventario_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ConexionDB db = new ConexionDB();
                    string sucursalNombre = db.ObtenerNombreSucursal();

                    // Cálculos Financieros Globales para los KPIs superiores
                    int totalVariedades = itemsVenta.Select(x => x.Nombre).Union(itemsBodega.Select(y => y.Nombre)).Distinct().Count();
                    int piezasTotales = itemsVenta.Sum(x => x.StockActual) + itemsBodega.Sum(y => y.StockActual);
                    decimal inversionBodega = itemsBodega.Sum(x => x.StockActual * x.PrecioCompra);
                    decimal valorVitrina = itemsVenta.Sum(x => x.StockActual * x.PrecioVenta);

                    iTextDocument doc = new iTextDocument(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(sfd.FileName, FileMode.Create));
                    doc.Open();

                    BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    iTextFont fTitulo = new iTextFont(bf, 14, iTextFont.BOLD, new BaseColor(44, 62, 80));
                    iTextFont fSub = new iTextFont(bf, 11, iTextFont.BOLD, BaseColor.DARK_GRAY);
                    iTextFont fCuerpo = new iTextFont(bf, 9);
                    iTextFont fBold = new iTextFont(bf, 9, iTextFont.BOLD);
                    iTextFont fTablaHead = new iTextFont(bf, 9, iTextFont.BOLD, BaseColor.WHITE);

                    BaseColor azulMarino = new BaseColor(44, 62, 80);

                    // Header Institucional
                    doc.Add(new iTextParagraph("PUNTO FLOWER - AUDITORÍA DE INVENTARIO GENERAL", fTitulo));
                    doc.Add(new iTextParagraph($"Sucursal: {sucursalNombre}", fBold));
                    doc.Add(new iTextParagraph($"Fecha de Emisión: {DateTime.Now:g}", fCuerpo));
                    doc.Add(new iTextParagraph($"Generado por: {Session.UsuarioActual}", fCuerpo));
                    doc.Add(new iTextParagraph("----------------------------------------------------------------------------------------------------------------------------------"));
                    doc.Add(new iTextParagraph(" "));

                    // 1. CUADRO DE RESUMEN ANALÍTICO (KPIs)
                    doc.Add(new iTextParagraph("RESUMEN VALORATIVO FINANCIERO", fSub));
                    doc.Add(new iTextParagraph(" "));

                    PdfPTable tablaKPI = new PdfPTable(4);
                    tablaKPI.WidthPercentage = 100;
                    tablaKPI.SetWidths(new float[] { 25f, 25f, 25f, 25f });

                    string[] kpiHeaders = { "Variedades Catálogo", "Piezas Totales", "Inversión Bodega", "Valor de Recuperación" };
                    foreach (string kh in kpiHeaders)
                    {
                        tablaKPI.AddCell(new PdfPCell(new Phrase(kh, fTablaHead)) { BackgroundColor = azulMarino, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5 });
                    }

                    tablaKPI.AddCell(new PdfPCell(new Phrase($"{totalVariedades} tipos", fBold)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });
                    tablaKPI.AddCell(new PdfPCell(new Phrase($"{piezasTotales} u.", fBold)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });
                    tablaKPI.AddCell(new PdfPCell(new Phrase(inversionBodega.ToString("C"), fBold)) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = new BaseColor(234, 242, 248), Padding = 6 });
                    tablaKPI.AddCell(new PdfPCell(new Phrase(valorVitrina.ToString("C"), fBold)) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = new BaseColor(234, 250, 241), Padding = 6 });

                    doc.Add(tablaKPI);
                    doc.Add(new iTextParagraph(" "));
                    doc.Add(new iTextParagraph(" "));

                    // 2. TABLA SECRETA A: BALANCE EN MOSTRADOR (VENTA)
                    doc.Add(new iTextParagraph("SECCIÓN A: MERCCANCÍA DISPONIBLE EN MOSTRADOR (VENTA)", fSub));
                    doc.Add(new iTextParagraph(" "));

                    PdfPTable tVenta = new PdfPTable(4);
                    tVenta.WidthPercentage = 100;
                    tVenta.SetWidths(new float[] { 45f, 15f, 20f, 20f });

                    string[] vHeads = { "Nombre de la Flor", "Existencias", "Precio Sugerido", "Total Vitrina" };
                    foreach (string vh in vHeads) tVenta.AddCell(new PdfPCell(new Phrase(vh, fTablaHead)) { BackgroundColor = azulMarino, Padding = 5 });

                    foreach (var p in itemsVenta)
                    {
                        tVenta.AddCell(new PdfPCell(new Phrase(p.Nombre, fCuerpo)) { Padding = 4 });
                        tVenta.AddCell(new PdfPCell(new Phrase(p.StockActual.ToString(), fBold)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 4 });
                        tVenta.AddCell(new PdfPCell(new Phrase(p.PrecioVenta.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                        tVenta.AddCell(new PdfPCell(new Phrase((p.StockActual * p.PrecioVenta).ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                    }
                    doc.Add(tVenta);
                    doc.Add(new iTextParagraph(" "));
                    doc.Add(new iTextParagraph(" "));

                    // 3. TABLA SECRETA B: BALANCE EN RESERVA (BODEGA)
                    doc.Add(new iTextParagraph("SECCIÓN B: RESERVA EN CÁMARA FRÍA (BODEGA)", fSub));
                    doc.Add(new iTextParagraph(" "));

                    PdfPTable tBodega = new PdfPTable(4);
                    tBodega.WidthPercentage = 100;
                    tBodega.SetWidths(new float[] { 45f, 15f, 20f, 20f });

                    string[] bHeads = { "Nombre de la Flor", "Existencias", "Último Costo Unitario", "Capital Congelado" };
                    foreach (string bh in bHeads) tBodega.AddCell(new PdfPCell(new Phrase(bh, fTablaHead)) { BackgroundColor = azulMarino, Padding = 5 });

                    foreach (var p in itemsBodega)
                    {
                        tBodega.AddCell(new PdfPCell(new Phrase(p.Nombre, fCuerpo)) { Padding = 4 });
                        tBodega.AddCell(new PdfPCell(new Phrase(p.StockActual.ToString(), fBold)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 4 });
                        tBodega.AddCell(new PdfPCell(new Phrase(p.PrecioCompra.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                        tBodega.AddCell(new PdfPCell(new Phrase((p.StockActual * p.PrecioCompra).ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                    }
                    doc.Add(tBodega);
                    doc.Add(new iTextParagraph(" "));
                    doc.Add(new iTextParagraph(" "));

                    // 4. SECCIÓN C: REPORTE DE HISTORIAL DE MERMAS REGISTRADAS
                    doc.Add(new iTextParagraph("SECCIÓN C: HISTORIAL DE MERMAS Y PÉRDIDAS DETECTADAS", fSub));
                    doc.Add(new iTextParagraph(" "));

                    PdfPTable tMermas = new PdfPTable(4);
                    tMermas.WidthPercentage = 100;
                    tMermas.SetWidths(new float[] { 40f, 15f, 25f, 20f });

                    string[] mHeads = { "Producto afectado", "Cantidad", "Motivo de la Pérdida", "Fecha Registro" };
                    foreach (string mh in mHeads) tMermas.AddCell(new PdfPCell(new Phrase(mh, fTablaHead)) { BackgroundColor = new BaseColor(146, 43, 33), Padding = 5 });

                    using (SqlConnection con = db.OpenConnection())
                    {
                        SqlCommand cmdM = new SqlCommand("SELECT TOP 15 ProductoNombre, Cantidad, Motivo, Fecha FROM Mermas ORDER BY Fecha DESC", con);
                        using (SqlDataReader rm = cmdM.ExecuteReader())
                        {
                            while (rm.Read())
                            {
                                tMermas.AddCell(new PdfPCell(new Phrase(rm["ProductoNombre"].ToString(), fCuerpo)) { Padding = 4 });
                                tMermas.AddCell(new PdfPCell(new Phrase(rm["Cantidad"].ToString(), fBold)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 4 });
                                tMermas.AddCell(new PdfPCell(new Phrase(rm["Motivo"].ToString(), fCuerpo)) { Padding = 4 });
                                tMermas.AddCell(new PdfPCell(new Phrase(Convert.ToDateTime(rm["Fecha"]).ToString("d"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 4 });
                            }
                        }
                    }
                    doc.Add(tMermas);

                    doc.Close();
                    MessageBox.Show("Reporte integral de inventario exportado a PDF con éxito.", "Auditoría Concluida", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al estructurar el reporte PDF: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}