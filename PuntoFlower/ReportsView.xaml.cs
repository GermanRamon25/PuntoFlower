using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;
using Microsoft.Win32;

// Alias para evitar conflictos entre iTextSharp y WPF
using iTextFont = iTextSharp.text.Font;
using iTextParagraph = iTextSharp.text.Paragraph;
using iTextDocument = iTextSharp.text.Document;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace PuntoFlower.Views
{
    public partial class ReportsView : UserControl
    {
        public class ReportRow
        {
            public string Fecha { get; set; }
            public string Concepto { get; set; }
            public string MetodoPago { get; set; }
            public string Referencia { get; set; }
            public string Monto { get; set; }
            public string Extra { get; set; }
        }

        public ReportsView()
        {
            InitializeComponent();
            dpInicio.SelectedDate = DateTime.Now.AddDays(-7);
            dpFin.SelectedDate = DateTime.Now;
        }

        private void btnGenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            if (dpInicio.SelectedDate == null || dpFin.SelectedDate == null) return;

            DateTime inicio = dpInicio.SelectedDate.Value.Date;
            DateTime fin = dpFin.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);

            decimal totalVentas = 0;
            decimal totalGastosServicios = 0;
            decimal totalSurtidoProveedores = 0;
            decimal gastosAdminEfectivo = 0;

            List<ReportRow> listaIngresos = new List<ReportRow>();
            List<ReportRow> listaEgresosEmpleado = new List<ReportRow>();
            List<ReportRow> listaEgresosAdmin = new List<ReportRow>();
            List<ReportRow> listaTop = new List<ReportRow>();
            List<ReportRow> listaMermas = new List<ReportRow>();

            ConexionDB db = new ConexionDB();

            using (SqlConnection con = db.OpenConnection())
            {
                // 1. Obtener Ventas y Salidas (BLINDADO POR SUCURSAL)
                string qVentas = "SELECT Fecha, ProductoNombre, Total, MetodoPago, CuentaTransferencia, NumeroReferencia, MontoRecibido FROM Ventas WHERE Fecha BETWEEN @i AND @f AND Sucursal = @s ORDER BY Fecha DESC";
                SqlCommand cmdV = new SqlCommand(qVentas, con);
                cmdV.Parameters.AddWithValue("@i", inicio);
                cmdV.Parameters.AddWithValue("@f", fin);
                cmdV.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                using (SqlDataReader r = cmdV.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal m = (decimal)r["Total"];
                        decimal recibido = r["MontoRecibido"] != DBNull.Value ? Convert.ToDecimal(r["MontoRecibido"]) : 0;
                        DateTime fMov = (DateTime)r["Fecha"];

                        string metodo = r["MetodoPago"]?.ToString() ?? "Efectivo";
                        string cuenta = r["CuentaTransferencia"] != DBNull.Value ? r["CuentaTransferencia"].ToString().Trim() : "";
                        string referencia = r["NumeroReferencia"] != DBNull.Value ? r["NumeroReferencia"].ToString().Trim() : "";

                        string metodoVisual = metodo + (string.IsNullOrEmpty(cuenta) ? "" : $" ({cuenta})");
                        string referenciaVisual = string.IsNullOrEmpty(referencia) ? "—" : referencia;

                        if (recibido < 0)
                        {
                            listaEgresosEmpleado.Add(new ReportRow
                            {
                                Fecha = fMov.ToString("dd/MM/yyyy"),
                                Concepto = $"[SALIDA CAJA] {r["ProductoNombre"]}",
                                Monto = Math.Abs(m).ToString("C")
                            });
                        }
                        else
                        {
                            totalVentas += m;
                            listaIngresos.Add(new ReportRow
                            {
                                Fecha = fMov.ToString("dd/MM/yyyy HH:mm"),
                                Concepto = r["ProductoNombre"].ToString(),
                                MetodoPago = metodoVisual,
                                Referencia = referenciaVisual,
                                Monto = m.ToString("C")
                            });
                        }
                    }
                }

                // 2. Obtener Surtido y Egresos del Administrador
                // A) Compras a proveedores (BLINDADO POR SUCURSAL)
                string qSurtido = "SELECT Fecha, ProductoNombre, (Cantidad * PrecioCosto) as Total FROM DetalleCompras WHERE Fecha BETWEEN @i AND @f AND Sucursal = @s ORDER BY Fecha DESC";
                SqlCommand cmdS = new SqlCommand(qSurtido, con);
                cmdS.Parameters.AddWithValue("@i", inicio);
                cmdS.Parameters.AddWithValue("@f", fin);
                cmdS.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                using (SqlDataReader r = cmdS.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal m = (decimal)r["Total"];
                        totalSurtidoProveedores += m;
                        DateTime fMov = (DateTime)r["Fecha"];
                        listaEgresosAdmin.Add(new ReportRow
                        {
                            Fecha = fMov.ToString("dd/MM/yyyy"),
                            Concepto = "Surtido: " + r["ProductoNombre"].ToString(),
                            Monto = m.ToString("C")
                        });
                    }
                }

                // B) Gastos generales del Administrador (BLINDADO POR SUCURSAL)
                string qGastosAdmin = "SELECT Fecha, Descripcion, Monto, MetodoPago FROM Gastos WHERE RegistradoPor = 'Admin' AND Fecha BETWEEN @i AND @f AND Sucursal = @s ORDER BY Fecha DESC";
                SqlCommand cmdGA = new SqlCommand(qGastosAdmin, con);
                cmdGA.Parameters.AddWithValue("@i", inicio);
                cmdGA.Parameters.AddWithValue("@f", fin);
                cmdGA.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                using (SqlDataReader r = cmdGA.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal m = (decimal)r["Monto"];
                        string metodo = r["MetodoPago"] != DBNull.Value ? r["MetodoPago"].ToString() : "Efectivo de Caja";
                        DateTime fMov = (DateTime)r["Fecha"];

                        totalSurtidoProveedores += m;

                        if (metodo.Equals("Efectivo de Caja", StringComparison.OrdinalIgnoreCase) || metodo.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                        {
                            gastosAdminEfectivo += m;
                        }

                        listaEgresosAdmin.Add(new ReportRow
                        {
                            Fecha = fMov.ToString("dd/MM/yyyy"),
                            Concepto = r["Descripcion"].ToString(),
                            Monto = m.ToString("C")
                        });
                    }
                }

                // 3. Obtener Gastos de Servicios Empleado (BLINDADO POR SUCURSAL)
                string qGastos = "SELECT Fecha, Descripcion, Monto FROM Gastos WHERE RegistradoPor = 'Empleado' AND Fecha BETWEEN @i AND @f AND Sucursal = @s ORDER BY Fecha DESC";
                SqlCommand cmdG = new SqlCommand(qGastos, con);
                cmdG.Parameters.AddWithValue("@i", inicio);
                cmdG.Parameters.AddWithValue("@f", fin);
                cmdG.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                using (SqlDataReader r = cmdG.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal m = (decimal)r["Monto"];
                        totalGastosServicios += m;
                        DateTime fMov = (DateTime)r["Fecha"];

                        listaEgresosEmpleado.Add(new ReportRow
                        {
                            Fecha = fMov.ToString("dd/MM/yyyy"),
                            Concepto = r["Descripcion"].ToString(),
                            Monto = m.ToString("C")
                        });
                    }
                }

                // 4. Obtener el TOP 5 (BLINDADO POR SUCURSAL)
                string qTop = @"SELECT TOP 5 ProductoNombre, SUM(Cantidad) as CantidadTotal 
                               FROM Ventas 
                               WHERE Fecha BETWEEN @i AND @f AND Sucursal = @s
                               GROUP BY ProductoNombre 
                               ORDER BY CantidadTotal DESC";
                SqlCommand cmdTop = new SqlCommand(qTop, con);
                cmdTop.Parameters.AddWithValue("@i", inicio);
                cmdTop.Parameters.AddWithValue("@f", fin);
                cmdTop.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                using (SqlDataReader r = cmdTop.ExecuteReader())
                {
                    while (r.Read())
                    {
                        listaTop.Add(new ReportRow
                        {
                            Concepto = r["ProductoNombre"].ToString(),
                            Extra = r["CantidadTotal"].ToString() + " vendidos"
                        });
                    }
                }

                // 5. Obtener Mermas (BLINDADO POR SUCURSAL)
                string qMermas = "SELECT Fecha, ProductoNombre, Cantidad, Motivo FROM Mermas WHERE Fecha BETWEEN @i AND @f AND Sucursal = @s ORDER BY Fecha DESC";
                SqlCommand cmdM = new SqlCommand(qMermas, con);
                cmdM.Parameters.AddWithValue("@i", inicio);
                cmdM.Parameters.AddWithValue("@f", fin);
                cmdM.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                using (SqlDataReader r = cmdM.ExecuteReader())
                {
                    while (r.Read())
                    {
                        DateTime fMov = (DateTime)r["Fecha"];
                        listaMermas.Add(new ReportRow
                        {
                            Fecha = fMov.ToString("dd/MM/yyyy"),
                            Concepto = r["ProductoNombre"].ToString(),
                            Monto = r["Cantidad"].ToString(),
                            Extra = r["Motivo"].ToString()
                        });
                    }
                }
            }

            decimal totalSalidasCajaEfectivo = 0;
            foreach (var item in listaEgresosEmpleado)
            {
                totalSalidasCajaEfectivo += decimal.Parse(item.Monto, System.Globalization.NumberStyles.Currency);
            }

            txtRepVentas.Text = totalVentas.ToString("C");
            txtRepGastos.Text = totalSalidasCajaEfectivo.ToString("C");
            txtRepGastosAdmin.Text = totalSurtidoProveedores.ToString("C");
            txtRepUtilidad.Text = (totalVentas - totalSalidasCajaEfectivo - gastosAdminEfectivo).ToString("C");

            dgIngresos.ItemsSource = listaIngresos;
            dgEgresosEmpleado.ItemsSource = listaEgresosEmpleado;

            dgEgresosAdmin.ItemsSource = listaEgresosAdmin;
            dgEgresosAdmin.Items.SortDescriptions.Clear();
            dgEgresosAdmin.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Fecha", System.ComponentModel.ListSortDirection.Descending));
            dgEgresosAdmin.Items.Refresh();

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
            sfd.FileName = $"Reporte_General_{ConexionGlobal.SucursalActual.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ConexionDB db = new ConexionDB();
                    string sucursalNombre = ConexionGlobal.SucursalActual; // Utilizamos directamente la variable blindada

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
                    if (dgIngresos.ItemsSource != null) foreach (ReportRow item in dgIngresos.ItemsSource) vTot += decimal.Parse(item.Monto, System.Globalization.NumberStyles.Currency);
                    if (dgEgresosEmpleado.ItemsSource != null) foreach (ReportRow item in dgEgresosEmpleado.ItemsSource) gServ += decimal.Parse(item.Monto, System.Globalization.NumberStyles.Currency);
                    if (dgEgresosAdmin.ItemsSource != null) foreach (ReportRow item in dgEgresosAdmin.ItemsSource) sProv += decimal.Parse(item.Monto, System.Globalization.NumberStyles.Currency);

                    decimal adminEfectivoPdf = 0;
                    using (SqlConnection con = db.OpenConnection())
                    {
                        // BLINDAJE: Calcular egresos en efectivo del admin limitados a la sucursal activa
                        string q = "SELECT ISNULL(SUM(Monto), 0) FROM Gastos WHERE RegistradoPor = 'Admin' AND (MetodoPago = 'Efectivo de Caja' OR MetodoPago = 'Efectivo') AND Fecha BETWEEN @i AND @f AND Sucursal = @s";
                        using (SqlCommand cmd = new SqlCommand(q, con))
                        {
                            cmd.Parameters.AddWithValue("@i", dpInicio.SelectedDate.Value.Date);
                            cmd.Parameters.AddWithValue("@f", dpFin.SelectedDate.Value.Date.AddDays(1).AddTicks(-1));
                            cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                            adminEfectivoPdf = Convert.ToDecimal(cmd.ExecuteScalar());
                        }
                    }

                    PdfPTable tablaResumen = new PdfPTable(4);
                    tablaResumen.WidthPercentage = 100;
                    tablaResumen.AddCell(new PdfPCell(new Phrase("(+) VENTAS BRUTAS: \n" + vTot.ToString("C"), fontCuerpo)) { BackgroundColor = new BaseColor(234, 250, 241), Padding = 5 });
                    tablaResumen.AddCell(new PdfPCell(new Phrase("(-) GASTOS Y SALIDAS: \n" + gServ.ToString("C"), fontCuerpo)) { BackgroundColor = new BaseColor(253, 237, 236), Padding = 5 });
                    tablaResumen.AddCell(new PdfPCell(new Phrase("(-) SURTIDO PROV. / ADMIN: \n" + sProv.ToString("C"), fontCuerpo)) { BackgroundColor = new BaseColor(252, 243, 207), Padding = 5 });
                    tablaResumen.AddCell(new PdfPCell(new Phrase("(=) UTILIDAD DISP.: \n" + (vTot - gServ - adminEfectivoPdf).ToString("C"), fontCuerpo)) { BackgroundColor = new BaseColor(235, 245, 251), Padding = 5 });
                    doc.Add(tablaResumen);
                    doc.Add(new iTextParagraph(" "));

                    // 1. CUADRO PDF: INGRESOS
                    doc.Add(new iTextParagraph("DETALLE DE INGRESOS (VENTAS MOSTRADOR CON MÉTODO DE PAGO)", fontSub));
                    doc.Add(new iTextParagraph(" "));
                    PdfPTable tIngresos = new PdfPTable(5);
                    tIngresos.WidthPercentage = 100;
                    tIngresos.SetWidths(new float[] { 18f, 37f, 18f, 12f, 15f });

                    string[] headsI = { "Fecha / Hora", "Concepto / Producto", "Método Pago", "Referencia", "Monto" };
                    foreach (string h in headsI) tIngresos.AddCell(new PdfPCell(new Phrase(h, fontTablaHead)) { BackgroundColor = new BaseColor(31, 97, 141), HorizontalAlignment = Element.ALIGN_CENTER });

                    if (dgIngresos.ItemsSource != null)
                    {
                        foreach (ReportRow item in dgIngresos.ItemsSource)
                        {
                            tIngresos.AddCell(new PdfPCell(new Phrase(item.Fecha, fontCuerpo)) { HorizontalAlignment = Element.ALIGN_CENTER });
                            tIngresos.AddCell(new PdfPCell(new Phrase(item.Concepto, fontCuerpo)));
                            tIngresos.AddCell(new PdfPCell(new Phrase(item.MetodoPago, fontCuerpo)));
                            tIngresos.AddCell(new PdfPCell(new Phrase(item.Referencia, fontCuerpo)) { HorizontalAlignment = Element.ALIGN_CENTER });
                            tIngresos.AddCell(new PdfPCell(new Phrase(item.Monto, fontCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                        }
                    }
                    doc.Add(tIngresos);
                    doc.Add(new iTextParagraph(" "));

                    // 2. CUADRO PDF: EGRESOS TIENDA (EMPLEADO Y SALIDAS DE EFECTIVO)
                    doc.Add(new iTextParagraph("DETALLE DE EGRESOS TIENDA Y SALIDAS DE EFECTIVO", fontSub));
                    doc.Add(new iTextParagraph(" "));
                    PdfPTable tEgresosEmp = new PdfPTable(3);
                    tEgresosEmp.WidthPercentage = 100;
                    tEgresosEmp.SetWidths(new float[] { 20f, 60f, 20f });
                    string[] headsEE = { "Fecha", "Servicio / Concepto de Salida", "Monto" };
                    foreach (string h in headsEE) tEgresosEmp.AddCell(new PdfPCell(new Phrase(h, fontTablaHead)) { BackgroundColor = new BaseColor(146, 43, 33), HorizontalAlignment = Element.ALIGN_CENTER });
                    if (dgEgresosEmpleado.ItemsSource != null)
                    {
                        foreach (ReportRow item in dgEgresosEmpleado.ItemsSource)
                        {
                            tEgresosEmp.AddCell(new Phrase(item.Fecha, fontCuerpo));
                            tEgresosEmp.AddCell(new Phrase(item.Concepto, fontCuerpo));
                            tEgresosEmp.AddCell(new PdfPCell(new Phrase(item.Monto, fontCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                        }
                    }
                    doc.Add(tEgresosEmp);
                    doc.Add(new iTextParagraph(" "));

                    // 3. CUADRO PDF: EGRESOS ADMIN
                    doc.Add(new iTextParagraph("DETALLE DE EGRESOS DE ADMINISTRACIÓN (SURTIDO / FLETES / GASTOS)", fontSub));
                    doc.Add(new iTextParagraph(" "));
                    PdfPTable tEgresosAdm = new PdfPTable(3);
                    tEgresosAdm.WidthPercentage = 100;
                    tEgresosAdm.SetWidths(new float[] { 20f, 60f, 20f });
                    string[] headsEA = { "Fecha", "Concepto Administración", "Monto" };
                    foreach (string h in headsEA) tEgresosAdm.AddCell(new PdfPCell(new Phrase(h, fontTablaHead)) { BackgroundColor = new BaseColor(125, 102, 8), HorizontalAlignment = Element.ALIGN_CENTER });
                    if (dgEgresosAdmin.ItemsSource != null)
                    {
                        foreach (ReportRow item in dgEgresosAdmin.ItemsSource)
                        {
                            tEgresosAdm.AddCell(new Phrase(item.Fecha, fontCuerpo));
                            tEgresosAdm.AddCell(new Phrase(item.Concepto, fontCuerpo));
                            tEgresosAdm.AddCell(new PdfPCell(new Phrase(item.Monto, fontCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT });
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
                        foreach (ReportRow m in dgMermas.ItemsSource)
                        {
                            tablaMermasPdf.AddCell(new Phrase(m.Fecha, fontCuerpo));
                            tablaMermasPdf.AddCell(new Phrase(m.Concepto, fontCuerpo));
                            tablaMermasPdf.AddCell(new Phrase(m.Monto, fontCuerpo));
                            tablaMermasPdf.AddCell(new Phrase(m.Extra, fontCuerpo));
                        }
                    }
                    doc.Add(tablaMermasPdf);

                    doc.Close();
                    MessageBox.Show("Reporte exportado correctamente con vinculación exacta a las tablas de la interfaz.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al generar PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}