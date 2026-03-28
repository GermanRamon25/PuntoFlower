using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;
using Microsoft.Win32;

// Usamos alias para evitar conflictos con WPF
using iTextFont = iTextSharp.text.Font;
using iTextParagraph = iTextSharp.text.Paragraph;
using iTextDocument = iTextSharp.text.Document;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace PuntoFlower.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            dpInicio.SelectedDate = DateTime.Now.AddDays(-7);
            dpFin.SelectedDate = DateTime.Now;
        }

        private void btnGenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            if (dpInicio.SelectedDate == null || dpFin.SelectedDate == null) return;

            DateTime inicio = dpInicio.SelectedDate.Value;
            DateTime fin = dpFin.SelectedDate.Value;

            decimal totalVentas = 0, totalGastos = 0;
            List<object> listaIngresos = new List<object>();
            List<object> listaEgresos = new List<object>();
            List<object> listaTop = new List<object>();

            ConexionDB db = new ConexionDB();

            using (SqlConnection con = db.OpenConnection())
            {
                // 1. Obtener Ventas
                string qVentas = "SELECT Fecha, ProductoNombre, Total FROM Ventas WHERE Fecha BETWEEN @i AND @f";
                SqlCommand cmdV = new SqlCommand(qVentas, con);
                cmdV.Parameters.AddWithValue("@i", inicio);
                cmdV.Parameters.AddWithValue("@f", fin.AddDays(1));
                using (SqlDataReader r = cmdV.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal m = (decimal)r["Total"];
                        totalVentas += m;
                        listaIngresos.Add(new { Fecha = r["Fecha"], Concepto = r["ProductoNombre"].ToString(), Monto = m });
                    }
                }

                // 2. Obtener Surtido
                string qSurtido = "SELECT Fecha, ProductoNombre, (Cantidad * PrecioCosto) as Total FROM DetalleCompras WHERE Fecha BETWEEN @i AND @f";
                SqlCommand cmdS = new SqlCommand(qSurtido, con);
                cmdS.Parameters.AddWithValue("@i", inicio);
                cmdS.Parameters.AddWithValue("@f", fin.AddDays(1));
                using (SqlDataReader r = cmdS.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal m = (decimal)r["Total"];
                        totalGastos += m;
                        listaEgresos.Add(new { Fecha = r["Fecha"], Concepto = "Surtido: " + r["ProductoNombre"].ToString(), Monto = m });
                    }
                }

                // 3. Obtener Gastos
                string qGastos = "SELECT Fecha, Descripcion, Monto FROM Gastos WHERE Fecha BETWEEN @i AND @f";
                SqlCommand cmdG = new SqlCommand(qGastos, con);
                cmdG.Parameters.AddWithValue("@i", inicio);
                cmdG.Parameters.AddWithValue("@f", fin.AddDays(1));
                using (SqlDataReader r = cmdG.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal m = (decimal)r["Monto"];
                        totalGastos += m;
                        listaEgresos.Add(new { Fecha = r["Fecha"], Concepto = r["Descripcion"].ToString(), Monto = m });
                    }
                }

                // 4. NUEVO: Obtener el TOP 5 de productos más vendidos
                string qTop = @"SELECT TOP 5 ProductoNombre, SUM(Cantidad) as CantidadTotal 
                               FROM Ventas 
                               WHERE Fecha BETWEEN @i AND @f 
                               GROUP BY ProductoNombre 
                               ORDER BY CantidadTotal DESC";
                SqlCommand cmdTop = new SqlCommand(qTop, con);
                cmdTop.Parameters.AddWithValue("@i", inicio);
                cmdTop.Parameters.AddWithValue("@f", fin.AddDays(1));
                using (SqlDataReader r = cmdTop.ExecuteReader())
                {
                    while (r.Read())
                    {
                        listaTop.Add(new
                        {
                            ProductoNombre = r["ProductoNombre"].ToString(),
                            CantidadTotal = r["CantidadTotal"].ToString() + " vendidos"
                        });
                    }
                }
            }

            txtRepVentas.Text = totalVentas.ToString("C");
            txtRepGastos.Text = totalGastos.ToString("C");
            txtRepUtilidad.Text = (totalVentas - totalGastos).ToString("C");

            dgIngresos.ItemsSource = listaIngresos;
            dgEgresos.ItemsSource = listaEgresos;
            dgTopVentas.ItemsSource = listaTop;
        }

        private void btnExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (dgIngresos.ItemsSource == null && dgEgresos.ItemsSource == null)
            {
                MessageBox.Show("Primero genera un reporte para poder exportarlo.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PDF Files (*.pdf)|*.pdf";
            sfd.FileName = $"Reporte_PuntoFlower_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    iTextDocument doc = new iTextDocument(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(sfd.FileName, FileMode.Create));
                    doc.Open();

                    BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    iTextFont fontTitulo = new iTextFont(bf, 18, iTextFont.BOLD);
                    iTextFont fontSub = new iTextFont(bf, 12, iTextFont.BOLD, BaseColor.GRAY);
                    iTextFont fontTablaHead = new iTextFont(bf, 10, iTextFont.BOLD, BaseColor.WHITE);
                    iTextFont fontCuerpo = new iTextFont(bf, 9);

                    doc.Add(new iTextParagraph("PUNTO FLOWER - REPORTE DE ESTADO DE CUENTA", fontTitulo));
                    doc.Add(new iTextParagraph($"Rango: {dpInicio.SelectedDate.Value:dd/MM/yyyy} al {dpFin.SelectedDate.Value:dd/MM/yyyy}", fontSub));
                    doc.Add(new iTextParagraph($"Generado el: {DateTime.Now:g}", fontCuerpo));
                    doc.Add(new iTextParagraph(" "));

                    PdfPTable tablaResumen = new PdfPTable(3);
                    tablaResumen.WidthPercentage = 100;
                    tablaResumen.AddCell(new PdfPCell(new Phrase("(+) VENTAS: " + txtRepVentas.Text, fontCuerpo)) { BackgroundColor = new BaseColor(234, 250, 241) });
                    tablaResumen.AddCell(new PdfPCell(new Phrase("(-) SALIDAS: " + txtRepGastos.Text, fontCuerpo)) { BackgroundColor = new BaseColor(253, 237, 236) });
                    tablaResumen.AddCell(new PdfPCell(new Phrase("(=) UTILIDAD: " + txtRepUtilidad.Text, fontCuerpo)) { BackgroundColor = new BaseColor(235, 245, 251) });
                    doc.Add(tablaResumen);
                    doc.Add(new iTextParagraph(" "));

                    // Detalles
                    PdfPTable tablaDetalles = new PdfPTable(4);
                    tablaDetalles.WidthPercentage = 100;
                    tablaDetalles.SetWidths(new float[] { 15f, 50f, 15f, 20f });

                    string[] headers = { "Fecha", "Concepto", "Tipo", "Monto" };
                    foreach (string h in headers)
                    {
                        PdfPCell headerCell = new PdfPCell(new Phrase(h, fontTablaHead)) { BackgroundColor = new BaseColor(44, 62, 80), HorizontalAlignment = Element.ALIGN_CENTER };
                        tablaDetalles.AddCell(headerCell);
                    }

                    foreach (dynamic item in dgIngresos.ItemsSource)
                    {
                        tablaDetalles.AddCell(new Phrase(item.Fecha.ToString("d"), fontCuerpo));
                        tablaDetalles.AddCell(new Phrase(item.Concepto, fontCuerpo));
                        tablaDetalles.AddCell(new Phrase("INGRESO", fontCuerpo));
                        tablaDetalles.AddCell(new Phrase(item.Monto.ToString("C"), fontCuerpo));
                    }

                    foreach (dynamic item in dgEgresos.ItemsSource)
                    {
                        tablaDetalles.AddCell(new Phrase(item.Fecha.ToString("d"), fontCuerpo));
                        tablaDetalles.AddCell(new Phrase(item.Concepto, fontCuerpo));
                        tablaDetalles.AddCell(new Phrase("EGRESO", fontCuerpo));
                        tablaDetalles.AddCell(new Phrase(item.Monto.ToString("C"), fontCuerpo));
                    }

                    doc.Add(tablaDetalles);
                    doc.Close();

                    MessageBox.Show("Reporte exportado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al generar PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}