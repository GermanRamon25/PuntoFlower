using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;
using Microsoft.Win32;

// Usamos alias para evitar conflictos con iTextSharp y WPF
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

            decimal totalVentas = 0;
            decimal totalGastosServicios = 0;
            decimal totalSurtidoProveedores = 0;
            decimal gastosAdminEfectivo = 0; // Acumulador para restar de la utilidad

            List<object> listaIngresos = new List<object>();
            List<object> listaEgresosEmpleado = new List<object>();
            List<object> listaEgresosAdmin = new List<object>();
            List<object> listaTop = new List<object>();
            List<object> listaMermas = new List<object>();

            ConexionDB db = new ConexionDB();

            using (SqlConnection con = db.OpenConnection())
            {
                // 1. Obtener Ventas (Ingresos)
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

                // 2. Obtener Surtido y Egresos del Administrador
                // A) Primero jalamos las compras directas a proveedores de DetalleCompras
                string qSurtido = "SELECT Fecha, ProductoNombre, (Cantidad * PrecioCosto) as Total FROM DetalleCompras WHERE Fecha BETWEEN @i AND @f";
                SqlCommand cmdS = new SqlCommand(qSurtido, con);
                cmdS.Parameters.AddWithValue("@i", inicio);
                cmdS.Parameters.AddWithValue("@f", fin.AddDays(1));
                using (SqlDataReader r = cmdS.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal m = (decimal)r["Total"];
                        totalSurtidoProveedores += m;
                        listaEgresosAdmin.Add(new { Fecha = r["Fecha"], Concepto = "Surtido: " + r["ProductoNombre"].ToString(), Monto = m });
                    }
                }

                // B) Jalamos también los egresos guardados bajo la firma 'Admin' desde el panel del Administrador
                string qGastosAdmin = "SELECT Fecha, Descripcion, Monto, MetodoPago FROM Gastos WHERE RegistradoPor = 'Admin' AND Fecha BETWEEN @i AND @f";
                SqlCommand cmdGA = new SqlCommand(qGastosAdmin, con);
                cmdGA.Parameters.AddWithValue("@i", inicio);
                cmdGA.Parameters.AddWithValue("@f", fin.AddDays(1));
                using (SqlDataReader r = cmdGA.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal m = (decimal)r["Monto"];
                        string metodo = r["MetodoPago"] != DBNull.Value ? r["MetodoPago"].ToString() : "Efectivo de Caja";

                        totalSurtidoProveedores += m;

                        // NUEVO: Si sacaste dinero físico de la caja chica, lo acumulamos para castigar la utilidad disponible
                        if (metodo.Equals("Efectivo de Caja", StringComparison.OrdinalIgnoreCase) || metodo.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                        {
                            gastosAdminEfectivo += m;
                        }

                        listaEgresosAdmin.Add(new { Fecha = r["Fecha"], Concepto = r["Descripcion"].ToString(), Monto = m });
                    }
                }

                // 3. Obtener Gastos de Servicios (Empleado)
                string qGastos = "SELECT Fecha, Descripcion, Monto FROM Gastos WHERE RegistradoPor = 'Empleado' AND Fecha BETWEEN @i AND @f";
                SqlCommand cmdG = new SqlCommand(qGastos, con);
                cmdG.Parameters.AddWithValue("@i", inicio);
                cmdG.Parameters.AddWithValue("@f", fin.AddDays(1));
                using (SqlDataReader r = cmdG.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal m = (decimal)r["Monto"];
                        totalGastosServicios += m;
                        listaEgresosEmpleado.Add(new { Fecha = r["Fecha"], Concepto = r["Descripcion"].ToString(), Monto = m });
                    }
                }

                // 4. Obtener el TOP 5
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
                        listaTop.Add(new { ProductoNombre = r["ProductoNombre"].ToString(), CantidadTotal = r["CantidadTotal"].ToString() + " vendidos" });
                    }
                }

                // 5. Obtener Mermas
                string qMermas = "SELECT Fecha, ProductoNombre, Cantidad, Motivo FROM Mermas WHERE Fecha BETWEEN @i AND @f ORDER BY Fecha DESC";
                SqlCommand cmdM = new SqlCommand(qMermas, con);
                cmdM.Parameters.AddWithValue("@i", inicio);
                cmdM.Parameters.AddWithValue("@f", fin.AddDays(1));
                using (SqlDataReader r = cmdM.ExecuteReader())
                {
                    while (r.Read())
                    {
                        listaMermas.Add(new
                        {
                            Fecha = r["Fecha"],
                            ProductoNombre = r["ProductoNombre"].ToString(),
                            Cantidad = r["Cantidad"].ToString(),
                            Motivo = r["Motivo"].ToString()
                        });
                    }
                }
            }

            txtRepVentas.Text = totalVentas.ToString("C");
            txtRepGastos.Text = totalGastosServicios.ToString("C");
            txtRepGastosAdmin.Text = totalSurtidoProveedores.ToString("C");

            // LÓGICA MODIFICADA: Utilidad Real = Ventas - Gastos Empleada - Gastos Admin hechos en Efectivo físico
            txtRepUtilidad.Text = (totalVentas - totalGastosServicios - gastosAdminEfectivo).ToString("C");

            dgIngresos.ItemsSource = listaIngresos;
            dgEgresosEmpleado.ItemsSource = listaEgresosEmpleado;
            dgEgresosAdmin.ItemsSource = listaEgresosAdmin;
            dgTopVentas.ItemsSource = listaTop;
            dgMermas.ItemsSource = listaMermas;
        }

        private void btnExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (dgIngresos.ItemsSource == null && dgEgresosEmpleado.ItemsSource == null && dgEgresosAdmin.ItemsSource == null)
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
                    ConexionDB db = new ConexionDB();
                    string sucursalNombre = db.ObtenerNombreSucursal();

                    iTextDocument doc = new iTextDocument(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(sfd.FileName, FileMode.Create));
                    doc.Open();

                    BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    iTextFont fontTitulo = new iTextFont(bf, 18, iTextFont.BOLD);
                    iTextFont fontSub = new iTextFont(bf, 12, iTextFont.BOLD, BaseColor.GRAY);
                    iTextFont fontMeta = new iTextFont(bf, 10, iTextFont.ITALIC, BaseColor.DARK_GRAY);
                    iTextFont fontTablaHead = new iTextFont(bf, 10, iTextFont.BOLD, BaseColor.WHITE);
                    iTextFont fontCuerpo = new iTextFont(bf, 9);

                    doc.Add(new iTextParagraph("PUNTO FLOWER - REPORTE FINANCIERO E INVENTARIO", fontTitulo));
                    doc.Add(new iTextParagraph($"Rango: {dpInicio.SelectedDate.Value:dd/MM/yyyy} al {dpFin.SelectedDate.Value:dd/MM/yyyy}", fontSub));
                    doc.Add(new iTextParagraph($"Sucursal: {sucursalNombre}", fontMeta));
                    doc.Add(new iTextParagraph($"Generado por: {Session.UsuarioActual} (ADMINISTRADOR)", fontMeta));
                    doc.Add(new iTextParagraph(" "));

                    decimal vTot = 0, gServ = 0, sProv = 0;
                    if (dgIngresos.ItemsSource != null) foreach (dynamic item in dgIngresos.ItemsSource) vTot += item.Monto;
                    if (dgEgresosEmpleado.ItemsSource != null) foreach (dynamic item in dgEgresosEmpleado.ItemsSource) gServ += item.Monto;
                    if (dgEgresosAdmin.ItemsSource != null) foreach (dynamic item in dgEgresosAdmin.ItemsSource) sProv += item.Monto;

                    // Recalcular los gastos en efectivo para la cabecera resumen del PDF impreso
                    decimal adminEfectivoPdf = 0;
                    using (SqlConnection con = db.OpenConnection())
                    {
                        string q = "SELECT ISNULL(SUM(Monto), 0) FROM Gastos WHERE RegistradoPor = 'Admin' AND (MetodoPago = 'Efectivo de Caja' OR MetodoPago = 'Efectivo') AND Fecha BETWEEN @i AND @f";
                        using (SqlCommand cmd = new SqlCommand(q, con))
                        {
                            cmd.Parameters.AddWithValue("@i", dpInicio.SelectedDate.Value);
                            cmd.Parameters.AddWithValue("@f", dpFin.SelectedDate.Value.Date.AddDays(1).AddTicks(-1));
                            adminEfectivoPdf = Convert.ToDecimal(cmd.ExecuteScalar());
                        }
                    }

                    PdfPTable tablaResumen = new PdfPTable(4);
                    tablaResumen.WidthPercentage = 100;
                    tablaResumen.AddCell(new PdfPCell(new Phrase("(+) VENTAS BRUTAS: \n" + vTot.ToString("C"), fontCuerpo)) { BackgroundColor = new BaseColor(234, 250, 241), Padding = 5 });
                    tablaResumen.AddCell(new PdfPCell(new Phrase("(-) GASTOS SERVICIOS: \n" + gServ.ToString("C"), fontCuerpo)) { BackgroundColor = new BaseColor(253, 237, 236), Padding = 5 });
                    tablaResumen.AddCell(new PdfPCell(new Phrase("(-) SURTIDO PROV.: \n" + sProv.ToString("C"), fontCuerpo)) { BackgroundColor = new BaseColor(252, 243, 207), Padding = 5 });
                    tablaResumen.AddCell(new PdfPCell(new Phrase("(=) UTILIDAD DISP.: \n" + (vTot - gServ - adminEfectivoPdf).ToString("C"), fontCuerpo)) { BackgroundColor = new BaseColor(235, 245, 251), Padding = 5 });
                    doc.Add(tablaResumen);
                    doc.Add(new iTextParagraph(" "));

                    // 1. CUADRO PDF: INGRESOS (VENTAS)
                    doc.Add(new iTextParagraph("DETALLE DE INGRESOS (VENTAS MOSTRADOR)", fontSub));
                    doc.Add(new iTextParagraph(" "));
                    PdfPTable tIngresos = new PdfPTable(3);
                    tIngresos.WidthPercentage = 100;
                    tIngresos.SetWidths(new float[] { 20f, 60f, 20f });
                    string[] headsI = { "Fecha", "Concepto / Producto", "Monto" };
                    foreach (string h in headsI) tIngresos.AddCell(new PdfPCell(new Phrase(h, fontTablaHead)) { BackgroundColor = new BaseColor(31, 97, 141), HorizontalAlignment = Element.ALIGN_CENTER });
                    if (dgIngresos.ItemsSource != null)
                    {
                        foreach (dynamic item in dgIngresos.ItemsSource)
                        {
                            tIngresos.AddCell(new Phrase(item.Fecha.ToString("d"), fontCuerpo));
                            tIngresos.AddCell(new Phrase(item.Concepto, fontCuerpo));
                            tIngresos.AddCell(new Phrase(item.Monto.ToString("C"), fontCuerpo));
                        }
                    }
                    doc.Add(tIngresos);
                    doc.Add(new iTextParagraph(" "));

                    // 2. CUADRO PDF SEPARADO: EGRESOS TIENDA (EMPLEADO)
                    doc.Add(new iTextParagraph("DETALLE DE EGRESOS TIENDA (PAGO SERVICIOS / LOCAL)", fontSub));
                    doc.Add(new iTextParagraph(" "));
                    PdfPTable tEgresosEmp = new PdfPTable(3);
                    tEgresosEmp.WidthPercentage = 100;
                    tEgresosEmp.SetWidths(new float[] { 20f, 60f, 20f });
                    string[] headsEE = { "Fecha", "Servicio / Concepto", "Monto" };
                    foreach (string h in headsEE) tEgresosEmp.AddCell(new PdfPCell(new Phrase(h, fontTablaHead)) { BackgroundColor = new BaseColor(146, 43, 33), HorizontalAlignment = Element.ALIGN_CENTER });
                    if (dgEgresosEmpleado.ItemsSource != null)
                    {
                        foreach (dynamic item in dgEgresosEmpleado.ItemsSource)
                        {
                            tEgresosEmp.AddCell(new Phrase(item.Fecha.ToString("d"), fontCuerpo));
                            tEgresosEmp.AddCell(new Phrase(item.Concepto, fontCuerpo));
                            tEgresosEmp.AddCell(new Phrase(item.Monto.ToString("C"), fontCuerpo));
                        }
                    }
                    doc.Add(tEgresosEmp);
                    doc.Add(new iTextParagraph(" "));

                    // 3. CUADRO PDF SEPARADO: EGRESOS ADMIN (SURTIDO)
                    doc.Add(new iTextParagraph("DETALLE DE EGRESOS DE ADMINISTRACIÓN (SURTIDO PROVEEDORES)", fontSub));
                    doc.Add(new iTextParagraph(" "));
                    PdfPTable tEgresosAdm = new PdfPTable(3);
                    tEgresosAdm.WidthPercentage = 100;
                    tEgresosAdm.SetWidths(new float[] { 20f, 60f, 20f });
                    string[] headsEA = { "Fecha", "Concepto Proveedor", "Monto" };
                    foreach (string h in headsEA) tEgresosAdm.AddCell(new PdfPCell(new Phrase(h, fontTablaHead)) { BackgroundColor = new BaseColor(125, 102, 8), HorizontalAlignment = Element.ALIGN_CENTER });
                    if (dgEgresosAdmin.ItemsSource != null)
                    {
                        foreach (dynamic item in dgEgresosAdmin.ItemsSource)
                        {
                            tEgresosAdm.AddCell(new Phrase(item.Fecha.ToString("d"), fontCuerpo));
                            tEgresosAdm.AddCell(new Phrase(item.Concepto, fontCuerpo));
                            tEgresosAdm.AddCell(new Phrase(item.Monto.ToString("C"), fontCuerpo));
                        }
                    }
                    doc.Add(tEgresosAdm);

                    // Historial de Mermas
                    doc.Add(new iTextParagraph(" "));
                    doc.Add(new iTextParagraph("HISTORIAL DE MERMAS (PRODUCTO DAÑADO)", fontSub));
                    doc.Add(new iTextParagraph(" "));

                    PdfPTable tablaMermasPdf = new PdfPTable(4);
                    tablaMermasPdf.WidthPercentage = 100;
                    string[] headersM = { "Fecha", "Producto", "Cant.", "Motivo" };
                    foreach (string h in headersM) tablaMermasPdf.AddCell(new PdfPCell(new Phrase(h, fontTablaHead)) { BackgroundColor = new BaseColor(146, 43, 33) });
                    if (dgMermas.ItemsSource != null)
                    {
                        foreach (dynamic m in dgMermas.ItemsSource)
                        {
                            tablaMermasPdf.AddCell(new Phrase(m.Fecha.ToString("d"), fontCuerpo));
                            tablaMermasPdf.AddCell(new Phrase(m.ProductoNombre, fontCuerpo));
                            tablaMermasPdf.AddCell(new Phrase(m.Cantidad, fontCuerpo));
                            tablaMermasPdf.AddCell(new Phrase(m.Motivo, fontCuerpo));
                        }
                    }
                    doc.Add(tablaMermasPdf);

                    doc.Close();
                    MessageBox.Show("Reporte exportado correctamente con tablas separadas por responsable.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al generar PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}