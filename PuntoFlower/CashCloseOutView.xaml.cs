using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;
using System.IO;
using Microsoft.Win32;

// Alias para evitar conflictos con WPF
using iTextFont = iTextSharp.text.Font;
using iTextParagraph = iTextSharp.text.Paragraph;
using iTextDocument = iTextSharp.text.Document;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace PuntoFlower.Views
{
    public partial class CashCloseOutView : UserControl
    {
        public CashCloseOutView()
        {
            InitializeComponent();
            RealizarCorteDelDia();
        }

        private void RealizarCorteDelDia()
        {
            List<object> ventasHoy = new List<object>();
            decimal sumaRecibido = 0;
            decimal sumaCambio = 0;
            ConexionDB db = new ConexionDB();

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = @"SELECT Fecha, ProductoNombre, Total, MontoRecibido, MontoCambio 
                                   FROM Ventas 
                                   WHERE CAST(Fecha AS DATE) = CAST(GETDATE() AS DATE)";

                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            decimal recibido = r["MontoRecibido"] != DBNull.Value ? Convert.ToDecimal(r["MontoRecibido"]) : 0;
                            decimal cambio = r["MontoCambio"] != DBNull.Value ? Convert.ToDecimal(r["MontoCambio"]) : 0;
                            decimal totalVenta = Convert.ToDecimal(r["Total"]);

                            sumaRecibido += recibido;
                            sumaCambio += cambio;

                            ventasHoy.Add(new
                            {
                                Fecha = (DateTime)r["Fecha"],
                                ProductoNombre = r["ProductoNombre"].ToString(),
                                Total = totalVenta,
                                MontoRecibido = recibido,
                                MontoCambio = cambio
                            });
                        }
                    }
                }

                dgCorte.ItemsSource = ventasHoy;
                txtTotalRecibido.Text = sumaRecibido.ToString("C");
                txtTotalCambio.Text = sumaCambio.ToString("C");

                decimal enCaja = sumaRecibido - sumaCambio;
                txtEfectivoReal.Text = enCaja.ToString("C");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al realizar el corte: " + ex.Message);
            }
        }

        private void btnFinalizarCorte_Click(object sender, RoutedEventArgs e)
        {
            if (dgCorte.ItemsSource == null)
            {
                MessageBox.Show("No hay movimientos registrados hoy para realizar un corte.", "Aviso");
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PDF Files (*.pdf)|*.pdf";
            sfd.FileName = $"Corte_Caja_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    iTextDocument doc = new iTextDocument(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(sfd.FileName, FileMode.Create));
                    doc.Open();

                    // Fuentes
                    BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    iTextFont fTitulo = new iTextFont(bf, 16, iTextFont.BOLD);
                    iTextFont fCuerpo = new iTextFont(bf, 10);
                    iTextFont fBold = new iTextFont(bf, 10, iTextFont.BOLD);

                    // 1. Encabezado
                    doc.Add(new iTextParagraph("PUNTO FLOWER - COMPROBANTE DE CORTE DE CAJA", fTitulo));
                    doc.Add(new iTextParagraph($"Fecha de Corte: {DateTime.Now:g}", fCuerpo));
                    doc.Add(new iTextParagraph("----------------------------------------------------------------------------------------------------------------------------------"));
                    doc.Add(new iTextParagraph(" "));

                    // 2. Bloque de Totales
                    PdfPTable tablaResumen = new PdfPTable(1);
                    tablaResumen.WidthPercentage = 40;
                    tablaResumen.HorizontalAlignment = Element.ALIGN_LEFT;

                    tablaResumen.AddCell(new PdfPCell(new Phrase("RESUMEN DE EFECTIVO", fBold)) { GrayFill = 0.9f });
                    tablaResumen.AddCell(new Phrase($"Total Recibido: {txtTotalRecibido.Text}", fCuerpo));
                    tablaResumen.AddCell(new Phrase($"Total Cambio: {txtTotalCambio.Text}", fCuerpo));

                    PdfPCell cellFinal = new PdfPCell(new Phrase($"EFECTIVO FINAL EN CAJA: {txtEfectivoReal.Text}", fBold));
                    cellFinal.BackgroundColor = new BaseColor(235, 245, 251);
                    tablaResumen.AddCell(cellFinal);

                    doc.Add(tablaResumen);
                    doc.Add(new iTextParagraph(" "));

                    // 3. Tabla de Auditoría
                    PdfPTable tablaVentas = new PdfPTable(4);
                    tablaVentas.WidthPercentage = 100;
                    tablaVentas.SetWidths(new float[] { 15f, 45f, 20f, 20f });

                    string[] headers = { "Hora", "Producto", "Pago Cliente", "Cambio" };
                    foreach (string h in headers)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(h, fBold)) { HorizontalAlignment = Element.ALIGN_CENTER, GrayFill = 0.8f };
                        tablaVentas.AddCell(cell);
                    }

                    foreach (dynamic item in dgCorte.ItemsSource)
                    {
                        tablaVentas.AddCell(new Phrase(item.Fecha.ToString("t"), fCuerpo));
                        tablaVentas.AddCell(new Phrase(item.ProductoNombre, fCuerpo));
                        tablaVentas.AddCell(new Phrase(item.MontoRecibido.ToString("C"), fCuerpo));
                        tablaVentas.AddCell(new Phrase(item.MontoCambio.ToString("C"), fCuerpo));
                    }

                    doc.Add(tablaVentas);
                    doc.Add(new iTextParagraph(" "));
                    doc.Add(new iTextParagraph(" "));
                    doc.Add(new iTextParagraph("Firma del Cajero: ___________________________", fCuerpo));

                    doc.Close();
                    MessageBox.Show("Corte de caja exportado y guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al generar el PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}