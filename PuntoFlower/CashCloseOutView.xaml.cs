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
        // Variables globales para calcular el arqueo resumido
        private decimal acumuladoEfectivo = 0;
        private decimal acumuladoTarjeta = 0;
        private decimal acumuladoTransfCuenta1 = 0;
        private decimal acumuladoTransfCuenta2 = 0;
        private decimal acumuladoDescuentos = 0;

        public CashCloseOutView()
        {
            InitializeComponent();
            txtEmpleadoEnTurno.Text = $"Empleado en turno: {Session.UsuarioActual}";
            RealizarCorteDelDia();
        }

        private void RealizarCorteDelDia()
        {
            List<object> ventasHoy = new List<object>();
            decimal sumaRecibido = 0;
            decimal sumaCambio = 0;

            // Reiniciamos contadores de arqueo
            acumuladoEfectivo = 0;
            acumuladoTarjeta = 0;
            acumuladoTransfCuenta1 = 0;
            acumuladoTransfCuenta2 = 0;
            acumuladoDescuentos = 0;

            ConexionDB db = new ConexionDB();

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = @"SELECT Fecha, ProductoNombre, Total, MontoRecibido, MontoCambio, MetodoPago, CuentaTransferencia, DescuentoAplicado 
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

                            string metodo = r["MetodoPago"]?.ToString() ?? "Efectivo";
                            string cuenta = r["CuentaTransferencia"] != DBNull.Value ? r["CuentaTransferencia"].ToString() : "";
                            decimal desc = r["DescuentoAplicado"] != DBNull.Value ? Convert.ToDecimal(r["DescuentoAplicado"]) : 0;

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
                                if (cuenta == "Cuenta Encargado 1") acumuladoTransfCuenta1 += totalVenta;
                                else if (cuenta == "Cuenta Encargado 2") acumuladoTransfCuenta2 += totalVenta;
                                else acumuladoEfectivo += totalVenta;
                            }

                            ventasHoy.Add(new
                            {
                                Fecha = (DateTime)r["Fecha"],
                                ProductoNombre = r["ProductoNombre"].ToString(),
                                Total = totalVenta,
                                MontoRecibido = recibido,
                                MontoCambio = cambio,
                                MetodoPago = metodo + (string.IsNullOrEmpty(cuenta) ? "" : $" ({cuenta})"),
                                Descuento = desc
                            });
                        }
                    }
                }

                dgCorte.ItemsSource = ventasHoy;
                txtTotalRecibido.Text = sumaRecibido.ToString("C");
                txtTotalCambio.Text = sumaCambio.ToString("C");
                txtEfectivoReal.Text = acumuladoEfectivo.ToString("C");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al realizar el corte: " + ex.Message);
            }
        }

        private void btnFinalizarCorte_Click(object sender, RoutedEventArgs e)
        {
            if (dgCorte.ItemsSource == null || !dgCorte.ItemsSource.Cast<object>().Any())
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

                    // Color Unificado: Azul Marino Corporativo
                    BaseColor azulMarino = new BaseColor(44, 62, 80);

                    // 1. Encabezado institucional de auditoría
                    doc.Add(new iTextParagraph("PUNTO FLOWER - COMPROBANTE DE CORTE DE CAJA", fTitulo));
                    doc.Add(new iTextParagraph($"Sucursal: {sucursalNombre}", fBold));
                    doc.Add(new iTextParagraph($"Fecha de Corte: {DateTime.Now:g}", fCuerpo));
                    doc.Add(new iTextParagraph($"Realizado por: {Session.UsuarioActual}", fCuerpo));
                    doc.Add(new iTextParagraph("----------------------------------------------------------------------------------------------------------------------------------"));
                    doc.Add(new iTextParagraph(" "));

                    // Macroestructura Horizontal para las dos tablas en paralelo
                    PdfPTable tablaEstructuraHorizontal = new PdfPTable(2);
                    tablaEstructuraHorizontal.WidthPercentage = 100;
                    tablaEstructuraHorizontal.SetWidths(new float[] { 46f, 54f });

                    // --- CELDA IZQUIERDA: RESUMEN DE EFECTIVO GENERAL (Colores unificados) ---
                    PdfPTable tablaResumen = new PdfPTable(1);
                    tablaResumen.WidthPercentage = 100;

                    tablaResumen.AddCell(new PdfPCell(new Phrase("RESUMEN DE EFECTIVO GENERAL", fTablaHead)) { BackgroundColor = azulMarino, Padding = 5 });
                    tablaResumen.AddCell(new PdfPCell(new Phrase($"Total Recibido del Día: {txtTotalRecibido.Text}", fCuerpo)) { Padding = 4 });
                    tablaResumen.AddCell(new PdfPCell(new Phrase($"Total Cambio Entregado: {txtTotalCambio.Text}", fCuerpo)) { Padding = 4 });

                    PdfPCell cellFinal = new PdfPCell(new Phrase($"EFECTIVO ESTIMADO EN CAJA: {txtEfectivoReal.Text}", fBold)) { BackgroundColor = new BaseColor(234, 242, 248), Padding = 6 };
                    tablaResumen.AddCell(cellFinal);

                    PdfPCell celdaIzquierda = new PdfPCell(tablaResumen) { Border = PdfPCell.NO_BORDER, PaddingRight = 15 };
                    tablaEstructuraHorizontal.AddCell(celdaIzquierda);

                    // --- CELDA DERECHA: DESGLOSE POR METODOS DE PAGO Y AUDITORÍA ---
                    PdfPTable tablaMetodos = new PdfPTable(2);
                    tablaMetodos.WidthPercentage = 100;
                    tablaMetodos.SetWidths(new float[] { 60f, 40f });

                    tablaMetodos.AddCell(new PdfPCell(new Phrase("Canal de Ingreso / Concepto", fTablaHead)) { BackgroundColor = azulMarino, Padding = 5 });
                    tablaMetodos.AddCell(new PdfPCell(new Phrase("Monto Acumulado", fTablaHead)) { BackgroundColor = azulMarino, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 5 });

                    tablaMetodos.AddCell(new PdfPCell(new Phrase("Ventas en Efectivo Puro (Neto)", fCuerpo)) { Padding = 3 });
                    tablaMetodos.AddCell(new PdfPCell(new Phrase(acumuladoEfectivo.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 3 });

                    tablaMetodos.AddCell(new PdfPCell(new Phrase("Terminal Bancaria (Tarjeta)", fCuerpo)) { Padding = 3 });
                    tablaMetodos.AddCell(new PdfPCell(new Phrase(acumuladoTarjeta.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 3 });

                    tablaMetodos.AddCell(new PdfPCell(new Phrase("Transferencias - Encargado 1", fCuerpo)) { Padding = 3 });
                    tablaMetodos.AddCell(new PdfPCell(new Phrase(acumuladoTransfCuenta1.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 3 });

                    tablaMetodos.AddCell(new PdfPCell(new Phrase("Transferencias - Encargado 2", fCuerpo)) { Padding = 3 });
                    tablaMetodos.AddCell(new PdfPCell(new Phrase(acumuladoTransfCuenta2.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 3 });

                    PdfPCell cellLabelDesc = new PdfPCell(new Phrase("Total de Descuentos Aplicados", fBold)) { BackgroundColor = new BaseColor(253, 237, 236), Padding = 4 };
                    PdfPCell cellValDesc = new PdfPCell(new Phrase(acumuladoDescuentos.ToString("C"), fBold)) { BackgroundColor = new BaseColor(253, 237, 236), HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 };
                    tablaMetodos.AddCell(cellLabelDesc);
                    tablaMetodos.AddCell(cellValDesc);

                    PdfPCell celdaDerecha = new PdfPCell(tablaMetodos) { Border = PdfPCell.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP };
                    tablaEstructuraHorizontal.AddCell(celdaDerecha);

                    doc.Add(tablaEstructuraHorizontal);
                    doc.Add(new iTextParagraph(" "));
                    doc.Add(new iTextParagraph(" "));

                    // 4. TABLA 3: Detalle de Transacciones del Turno
                    doc.Add(new iTextParagraph("DETALLE DE TRANSACCIONES DEL TURNO", fSub));
                    doc.Add(new iTextParagraph(" "));

                    PdfPTable tablaVentas = new PdfPTable(6);
                    tablaVentas.WidthPercentage = 100;
                    tablaVentas.SetWidths(new float[] { 12f, 35f, 15f, 15f, 13f, 10f });

                    // SOLUCIÓN AL ERROR: Declaramos formalmente la variable 'headers' con los nombres de las columnas
                    string[] headers = { "Hora", "Producto", "Método Pago", "Importe", "Descuento", "Cambio" };
                    foreach (string h in headers)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(h, fTablaHead)) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = azulMarino, Padding = 5 };
                        tablaVentas.AddCell(cell);
                    }

                    foreach (dynamic item in dgCorte.ItemsSource)
                    {
                        tablaVentas.AddCell(new PdfPCell(new Phrase(item.Fecha.ToString("t"), fCuerpo)) { Padding = 4 });
                        tablaVentas.AddCell(new PdfPCell(new Phrase(item.ProductoNombre, fCuerpo)) { Padding = 4 });
                        tablaVentas.AddCell(new PdfPCell(new Phrase(item.MetodoPago, fCuerpo)) { Padding = 4 });
                        tablaVentas.AddCell(new PdfPCell(new Phrase(item.Total.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                        tablaVentas.AddCell(new PdfPCell(new Phrase(item.Descuento.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                        tablaVentas.AddCell(new PdfPCell(new Phrase(item.MontoCambio.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                    }

                    doc.Add(tablaVentas);
                    doc.Add(new iTextParagraph(" "));
                    doc.Add(new iTextParagraph(" "));
                    doc.Add(new iTextParagraph($"Firma del Cajero ({Session.UsuarioActual}): ___________________________", fCuerpo));

                    doc.Close();

                    // Resguardo de base de datos automatizado
                    try
                    {
                        string folderRespaldos = @"C:\RespaldosPuntoFlower\";
                        if (!Directory.Exists(folderRespaldos))
                        {
                            Directory.CreateDirectory(folderRespaldos);
                        }

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
                        MessageBox.Show("Corte de caja exportado y base de datos respaldada localmente con éxito.", "Cierre Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception exBackup)
                    {
                        MessageBox.Show("Corte exportado a PDF correctamente, pero el respaldo automático falló: " + exBackup.Message, "Aviso de Seguridad", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al generar el PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}