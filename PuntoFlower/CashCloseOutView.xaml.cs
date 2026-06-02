using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;
using System.IO;
using System.Linq;
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
        private decimal acumuladoEfectivo = 0;
        private decimal acumuladoTarjeta = 0;
        private decimal acumuladoTransfCuenta1 = 0;
        private decimal acumuladoTransfCuenta2 = 0;
        private decimal acumuladoDescuentos = 0;

        private string nombreEncargado1 = "Encargado 1";
        private string nombreEncargado2 = "Encargado 2";

        public CashCloseOutView()
        {
            InitializeComponent();
            txtEmpleadoEnTurno.Text = $"Empleado en turno: {Session.UsuarioActual}";
        }

        private void cmbPeriodo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtTituloCorte == null || txtSubtituloTabla == null) return;
            ProcesarCorteFiltrado();
        }

        private void ProcesarCorteFiltrado()
        {
            List<object> ventasFiltradas = new List<object>();
            decimal sumaRecibido = 0;
            decimal sumaCambio = 0;

            acumuladoEfectivo = 0;
            acumuladoTarjeta = 0;
            acumuladoTransfCuenta1 = 0;
            acumuladoTransfCuenta2 = 0;
            acumuladoDescuentos = 0;

            ConexionDB db = new ConexionDB();
            nombreEncargado1 = db.ObtenerEncargadoCuenta1();
            nombreEncargado2 = db.ObtenerEncargadoCuenta2();

            string condicionFecha = "";
            string seleccion = (cmbPeriodo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Hoy";

            // NUEVA LÓGICA RECOMENDADA: Filtros basados en Calendario Comercial
            switch (seleccion)
            {
                case "Hoy":
                    txtTituloCorte.Text = "Corte de Caja Diario";
                    txtSubtituloTabla.Text = "Auditoría de Movimientos (Hoy)";
                    condicionFecha = "WHERE CAST(Fecha AS DATE) = CAST(GETDATE() AS DATE)";
                    break;

                case "Esta Semana":
                    txtTituloCorte.Text = "Reporte Financiero Semanal (Natural)";
                    txtSubtituloTabla.Text = "Auditoría de Movimientos (Lunes a Domingo Actual)";
                    // DATEDIFF con wk toma el inicio de la semana configurado en el servidor (Lunes por defecto)
                    condicionFecha = "WHERE DATEDIFF(wk, Fecha, GETDATE()) = 0 AND Fecha <= GETDATE()";
                    break;

                case "Este Mes":
                    txtTituloCorte.Text = "Reporte Financiero Mensual (Calendario)";
                    txtSubtituloTabla.Text = "Auditoría de Movimientos (Mes en Curso)";
                    // DATEDIFF con mm asegura que solo tome los días que compartan el mismo mes y año actual
                    condicionFecha = "WHERE DATEDIFF(mm, Fecha, GETDATE()) = 0 AND Fecha <= GETDATE()";
                    break;
            }

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = $@"SELECT Fecha, ProductoNombre, Total, MontoRecibido, MontoCambio, MetodoPago, CuentaTransferencia, DescuentoAplicado, NumeroReferencia 
                                     FROM Ventas 
                                     {condicionFecha}
                                     ORDER BY Fecha DESC";

                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            decimal recibido = r["MontoRecibido"] != DBNull.Value ? Convert.ToDecimal(r["MontoRecibido"]) : 0;
                            decimal cambio = r["MontoCambio"] != DBNull.Value ? Convert.ToDecimal(r["MontoCambio"]) : 0;
                            decimal totalVenta = Convert.ToDecimal(r["Total"]);

                            string metodo = r["MetodoPago"]?.ToString() ?? "Efectivo";
                            string cuenta = r["CuentaTransferencia"] != DBNull.Value ? r["CuentaTransferencia"].ToString() : "";
                            decimal desc = r["DescuentoAplicado"] != DBNull.Value ? Convert.ToDecimal(r["DescuentoAplicado"]) : 0;
                            string referencia = r["NumeroReferencia"] != DBNull.Value ? r["NumeroReferencia"].ToString() : "";

                            sumaRecibido += recibido;
                            sumaCambio += cambio;
                            acumuladoDescuentos += desc;

                            if (metodo == "Efectivo")
                            {
                                acumuladoEfectivo += (recibido - cambio);
                            }
                            else if (metodo == "Tarjeta")
                            {
                                acumuladoTarjeta += totalVenta;
                            }
                            else if (metodo == "Transferencia")
                            {
                                if (cuenta == nombreEncargado1 || cuenta == "Cuenta Encargado 1")
                                    acumuladoTransfCuenta1 += totalVenta;
                                else if (cuenta == nombreEncargado2 || cuenta == "Cuenta Encargado 2")
                                    acumuladoTransfCuenta2 += totalVenta;
                                else
                                    acumuladoEfectivo += totalVenta;
                            }

                            ventasFiltradas.Add(new
                            {
                                Fecha = (DateTime)r["Fecha"],
                                ProductoNombre = r["ProductoNombre"].ToString(),
                                Total = totalVenta,
                                MontoRecibido = recibido,
                                MontoCambio = cambio,
                                MetodoPago = metodo + (string.IsNullOrEmpty(cuenta) ? "" : $" ({cuenta})"),
                                NumeroReferencia = string.IsNullOrEmpty(referencia) ? "—" : referencia,
                                Descuento = desc
                            });
                        }
                    }
                }

                dgCorte.ItemsSource = ventasFiltradas;
                txtTotalRecibido.Text = sumaRecibido.ToString("C");
                txtTotalCambio.Text = sumaCambio.ToString("C");
                txtEfectivoReal.Text = acumuladoEfectivo.ToString("C");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al realizar el filtro de caja: " + ex.Message, "Error Operativo");
            }
        }

        private void btnFinalizarCorte_Click(object sender, RoutedEventArgs e)
        {
            if (dgCorte.ItemsSource == null || !dgCorte.ItemsSource.Cast<object>().Any())
            {
                MessageBox.Show("No hay movimientos registrados en este periodo para realizar una exportación.", "Aviso");
                return;
            }

            string periodoSeleccionado = (cmbPeriodo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Hoy";

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PDF Files (*.pdf)|*.pdf";
            sfd.FileName = $"Reporte_Caja_{periodoSeleccionado.Replace(" ", "")}_{DateTime.Now:yyyyMMdd}.pdf";

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
                    iTextFont fTitulo = new iTextFont(bf, 15, iTextFont.BOLD);
                    iTextFont fSub = new iTextFont(bf, 11, iTextFont.BOLD, BaseColor.DARK_GRAY);
                    iTextFont fCuerpo = new iTextFont(bf, 9);
                    iTextFont fBold = new iTextFont(bf, 9, iTextFont.BOLD);
                    iTextFont fTablaHead = new iTextFont(bf, 9, iTextFont.BOLD, BaseColor.WHITE);

                    BaseColor azulMarino = new BaseColor(44, 62, 80);

                    doc.Add(new iTextParagraph($"PUNTO FLOWER - REPORTE DE AUDITORÍA ({periodoSeleccionado.ToUpper()})", fTitulo));
                    doc.Add(new iTextParagraph($"Sucursal: {sucursalNombre}", fBold));
                    doc.Add(new iTextParagraph($"Fecha de Emisión: {DateTime.Now:g}", fCuerpo));
                    doc.Add(new iTextParagraph($"Generado por: {Session.UsuarioActual}", fCuerpo));
                    doc.Add(new iTextParagraph("----------------------------------------------------------------------------------------------------------------------------------"));
                    doc.Add(new iTextParagraph(" "));

                    PdfPTable tablaEstructuraHorizontal = new PdfPTable(2);
                    tablaEstructuraHorizontal.WidthPercentage = 100;
                    tablaEstructuraHorizontal.SetWidths(new float[] { 46f, 54f });

                    PdfPTable tablaResumen = new PdfPTable(1);
                    tablaResumen.WidthPercentage = 100;
                    tablaResumen.AddCell(new PdfPCell(new Phrase("RESUMEN DE EFECTIVO ESTIMADO", fTablaHead)) { BackgroundColor = azulMarino, Padding = 5 });
                    tablaResumen.AddCell(new PdfPCell(new Phrase($"Total Recibido en Periodo: {txtTotalRecibido.Text}", fCuerpo)) { Padding = 4 });
                    tablaResumen.AddCell(new PdfPCell(new Phrase($"Total Cambio Entregado: {txtTotalCambio.Text}", fCuerpo)) { Padding = 4 });

                    PdfPCell cellFinal = new PdfPCell(new Phrase($"EFECTIVO NETO ESTIMADO: {txtEfectivoReal.Text}", fBold)) { BackgroundColor = new BaseColor(234, 242, 248), Padding = 6 };
                    tablaResumen.AddCell(cellFinal);

                    PdfPCell celdaIzquierda = new PdfPCell(tablaResumen) { Border = PdfPCell.NO_BORDER, PaddingRight = 15 };
                    tablaEstructuraHorizontal.AddCell(celdaIzquierda);

                    PdfPTable tablaMetodos = new PdfPTable(2);
                    tablaMetodos.WidthPercentage = 100;
                    tablaMetodos.SetWidths(new float[] { 60f, 40f });

                    tablaMetodos.AddCell(new PdfPCell(new Phrase("Canal de Ingreso / Concepto", fTablaHead)) { BackgroundColor = azulMarino, Padding = 5 });
                    tablaMetodos.AddCell(new PdfPCell(new Phrase("Monto Acumulado", fTablaHead)) { BackgroundColor = azulMarino, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 5 });

                    tablaMetodos.AddCell(new PdfPCell(new Phrase("Ventas en Efectivo Neto", fCuerpo)) { Padding = 3 });
                    tablaMetodos.AddCell(new PdfPCell(new Phrase(acumuladoEfectivo.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 3 });

                    tablaMetodos.AddCell(new PdfPCell(new Phrase("Terminal Bancaria (Tarjeta)", fCuerpo)) { Padding = 3 });
                    tablaMetodos.AddCell(new PdfPCell(new Phrase(acumuladoTarjeta.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 3 });

                    tablaMetodos.AddCell(new PdfPCell(new Phrase($"Transferencias - {nombreEncargado1}", fCuerpo)) { Padding = 3 });
                    tablaMetodos.AddCell(new PdfPCell(new Phrase(acumuladoTransfCuenta1.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 3 });

                    tablaMetodos.AddCell(new PdfPCell(new Phrase($"Transferencias - {nombreEncargado2}", fCuerpo)) { Padding = 3 });
                    tablaMetodos.AddCell(new PdfPCell(new Phrase(acumuladoTransfCuenta2.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 3 });

                    PdfPCell cellLabelDesc = new PdfPCell(new Phrase("Total de Descuentos Aplicados", fBold)) { BackgroundColor = new BaseColor(253, 237, 236), Padding = 4 };
                    PdfPCell cellValDesc = new PdfPCell(new Phrase(acumuladoDescuentos.ToString("C"), fBold)) { BackgroundColor = new BaseColor(253, 237, 236), HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 };
                    tablaMetodos.AddCell(cellLabelDesc);
                    tablaMetodos.AddCell(cellValDesc);

                    PdfPCell celdaDerecha = new PdfPCell(tablaMetodos) { Border = PdfPCell.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP };
                    tablaEstructuraHorizontal.AddCell(celdaDerecha);

                    doc.Add(tablaEstructuraHorizontal);
                    doc.Add(new iTextParagraph(" "));

                    doc.Add(new iTextParagraph($"HISTORIAL DE MOVIMIENTOS ({periodoSeleccionado.ToUpper()})", fSub));
                    doc.Add(new iTextParagraph(" "));

                    PdfPTable tablaVentas = new PdfPTable(7);
                    tablaVentas.WidthPercentage = 100;
                    tablaVentas.SetWidths(new float[] { 14f, 24f, 15f, 15f, 12f, 10f, 10f });

                    string[] headers = { "Fecha/Hora", "Producto", "Método Pago", "Ref/Depósito", "Importe", "Descuento", "Cambio" };
                    foreach (string h in headers)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(h, fTablaHead)) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = azulMarino, Padding = 5 };
                        tablaVentas.AddCell(cell);
                    }

                    foreach (dynamic item in dgCorte.ItemsSource)
                    {
                        tablaVentas.AddCell(new PdfPCell(new Phrase(item.Fecha.ToString("dd/MM HH:mm"), fCuerpo)) { Padding = 4 });
                        tablaVentas.AddCell(new PdfPCell(new Phrase(item.ProductoNombre, fCuerpo)) { Padding = 4 });
                        tablaVentas.AddCell(new PdfPCell(new Phrase(item.MetodoPago, fCuerpo)) { Padding = 4 });
                        tablaVentas.AddCell(new PdfPCell(new Phrase(item.NumeroReferencia, fCuerpo)) { Padding = 4, HorizontalAlignment = Element.ALIGN_CENTER });
                        tablaVentas.AddCell(new PdfPCell(new Phrase(item.Total.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                        tablaVentas.AddCell(new PdfPCell(new Phrase(item.Descuento.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                        tablaVentas.AddCell(new PdfPCell(new Phrase(item.MontoCambio.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                    }

                    doc.Add(tablaVentas);
                    doc.Add(new iTextParagraph(" "));
                    doc.Add(new iTextParagraph(" "));
                    doc.Add(new iTextParagraph($"Firma de Supervisor / Auditor: ___________________________", fCuerpo));

                    doc.Close();

                    try
                    {
                        string folderRespaldos = @"C:\RespaldosPuntoFlower\";
                        if (!Directory.Exists(folderRespaldos)) Directory.CreateDirectory(folderRespaldos);

                        using (SqlConnection conRespaldo = db.OpenConnection())
                        {
                            string sqlBackup = $@"BACKUP DATABASE PuntoFlowerDB 
                                                 TO DISK = '{folderRespaldos}PuntoFlower_Cierre_Auto.bak' 
                                                 WITH INIT;";
                            using (SqlCommand cmdBackup = new SqlCommand(sqlBackup, conRespaldo))
                            {
                                cmdBackup.ExecuteNonQuery();
                            }
                        }
                        MessageBox.Show("Reporte exportado y base de datos respaldada localmente con éxito.", "Auditoría Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception exBackup)
                    {
                        MessageBox.Show("Reporte exportado correctamente, pero el respaldo automático falló: " + exBackup.Message, "Aviso de Resguardo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al generar el documento PDF: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}