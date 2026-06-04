using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;
using PuntoFlower.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using iTextDocument = iTextSharp.text.Document;
using iTextFont = iTextSharp.text.Font;
using iTextParagraph = iTextSharp.text.Paragraph;

namespace PuntoFlower.Views
{
    public class MovimientoVentaClass
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string ProductoNombre { get; set; }
        public decimal Total { get; set; }
        public decimal MontoRecibido { get; set; }
        public decimal MontoCambio { get; set; }
        public string MetodoPagoPuro { get; set; }
        public string MetodoPagoVisual { get; set; }
        public string NumeroReferencia { get; set; }
        public decimal Descuento { get; set; }
    }

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

            if (cmbPeriodo != null) cmbPeriodo.SelectedIndex = 0;

            EvaluarVisibilidadBotonEliminar();
            ProcesarCorteFiltrado();

            this.IsVisibleChanged += (s, e) => {
                if ((bool)e.NewValue)
                {
                    if (cmbPeriodo != null) cmbPeriodo.SelectedIndex = 0;
                    EvaluarVisibilidadBotonEliminar();
                    ProcesarCorteFiltrado();
                }
            };
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (cmbPeriodo != null && cmbPeriodo.SelectedIndex != 0) cmbPeriodo.SelectedIndex = 0;
            EvaluarVisibilidadBotonEliminar();
            ProcesarCorteFiltrado();
        }

        private void EvaluarVisibilidadBotonEliminar()
        {
            if (btnEliminarVentaSeleccionada == null) return;

            string rolUsuario = "";
            try { rolUsuario = Session.RolActual?.ToString() ?? ""; } catch { rolUsuario = Session.UsuarioActual?.ToString() ?? ""; }

            if (rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase) ||
                rolUsuario.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                Session.UsuarioActual.Equals("leticia", StringComparison.OrdinalIgnoreCase))
            {
                btnEliminarVentaSeleccionada.Visibility = Visibility.Visible;
            }
            else
            {
                btnEliminarVentaSeleccionada.Visibility = Visibility.Collapsed;
            }
        }

        private void cmbPeriodo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtTituloCorte == null || txtSubtituloTabla == null) return;
            ProcesarCorteFiltrado();
        }

        private void ProcesarCorteFiltrado()
        {
            List<MovimientoVentaClass> ventasFiltradas = new List<MovimientoVentaClass>();
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
                    condicionFecha = "WHERE DATEDIFF(wk, Fecha, GETDATE()) = 0 AND Fecha <= GETDATE()";
                    break;
                case "Este Mes":
                    txtTituloCorte.Text = "Reporte Financiero Mensual (Calendario)";
                    txtSubtituloTabla.Text = "Auditoría de Movimientos (Mes en Curso)";
                    condicionFecha = "WHERE DATEDIFF(mm, Fecha, GETDATE()) = 0 AND Fecha <= GETDATE()";
                    break;
            }

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = $@"SELECT Id, Fecha, ProductoNombre, Total, MontoRecibido, MontoCambio, MetodoPago, CuentaTransferencia, DescuentoAplicado, NumeroReferencia 
                                     FROM Ventas {condicionFecha} ORDER BY Fecha DESC";

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

                            if (metodo == "Efectivo") acumuladoEfectivo += (recibido - cambio);
                            else if (metodo == "Tarjeta" || metodo.Contains("Tarjeta")) acumuladoTarjeta += totalVenta;
                            else if (metodo == "Transferencia" || metodo.Contains("Transferencia"))
                            {
                                if (cuenta == nombreEncargado1 || cuenta == "Cuenta Encargado 1") acumuladoTransfCuenta1 += totalVenta;
                                else if (cuenta == nombreEncargado2 || cuenta == "Cuenta Encargado 2") acumuladoTransfCuenta2 += totalVenta;
                                else acumuladoEfectivo += totalVenta;
                            }

                            ventasFiltradas.Add(new MovimientoVentaClass
                            {
                                Id = Convert.ToInt32(r["Id"]),
                                Fecha = (DateTime)r["Fecha"],
                                ProductoNombre = r["ProductoNombre"].ToString(),
                                Total = totalVenta,
                                MontoRecibido = recibido,
                                MontoCambio = cambio,
                                MetodoPagoPuro = metodo,
                                MetodoPagoVisual = metodo + (string.IsNullOrEmpty(cuenta) ? "" : $" ({cuenta})"),
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
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void btnEliminarVentaSeleccionada_Click(object sender, RoutedEventArgs e)
        {
            var ventaSeleccionada = dgCorte.SelectedItem as MovimientoVentaClass;
            if (ventaSeleccionada == null) return;

            var result = MessageBox.Show($"¿Eliminar venta #{ventaSeleccionada.Id} y restaurar stock?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    using (SqlTransaction tra = con.BeginTransaction())
                    {
                        try
                        {
                            string concepto = ventaSeleccionada.ProductoNombre;

                            if (concepto.Contains("Liquidación / Abono:") || concepto.Contains("Anticipo Nuevo Pedido:"))
                            {
                                string cliente = concepto.Replace("Liquidación / Abono:", "").Replace("Anticipo Nuevo Pedido:", "").Split('(')[0].Trim();
                                string qPed = "UPDATE Pedidos SET SaldoPendiente = SaldoPendiente + @monto, Estado = 'Pendiente' WHERE ClienteNombre = @cliente AND Estado != 'Entregado'";
                                using (SqlCommand cmdP = new SqlCommand(qPed, con, tra))
                                {
                                    cmdP.Parameters.AddWithValue("@monto", ventaSeleccionada.Total);
                                    cmdP.Parameters.AddWithValue("@cliente", cliente);
                                    cmdP.ExecuteNonQuery();
                                }
                            }

                            string queryInsumos = "SELECT Nombre, PrecioVenta FROM Productos WHERE Categoria = 'Venta'";
                            using (SqlCommand cmdGet = new SqlCommand(queryInsumos, con, tra))
                            {
                                using (SqlDataReader reader = cmdGet.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        string nombreProd = reader["Nombre"].ToString();
                                        if (concepto.Contains(nombreProd))
                                        {
                                            decimal precioUnit = Convert.ToDecimal(reader["PrecioVenta"]);
                                            int cantidad = (int)Math.Round(ventaSeleccionada.Total / precioUnit);
                                            if (cantidad == 0) cantidad = 1;

                                            string qUpdate = "UPDATE Productos SET StockActual = StockActual + @c WHERE Nombre = @nom";
                                            using (SqlCommand cmdUp = new SqlCommand(qUpdate, con, tra))
                                            {
                                                cmdUp.Parameters.AddWithValue("@c", cantidad);
                                                cmdUp.Parameters.AddWithValue("@nom", nombreProd);
                                                cmdUp.ExecuteNonQuery();
                                            }
                                        }
                                    }
                                }
                            }

                            using (SqlCommand cmdDel = new SqlCommand("DELETE FROM Ventas WHERE Id = @id", con, tra))
                            {
                                cmdDel.Parameters.AddWithValue("@id", ventaSeleccionada.Id);
                                cmdDel.ExecuteNonQuery();
                            }

                            tra.Commit();
                            MessageBox.Show("Venta revertida exitosamente.");
                            ProcesarCorteFiltrado();
                        }
                        catch { tra.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private string ObtenerRangoFechasTexto(string seleccion)
        {
            switch (seleccion)
            {
                case "Hoy": return DateTime.Now.ToString("dd/MM/yyyy");
                case "Esta Semana": return "Semana Actual (Lunes a Domingo)";
                case "Este Mes": return DateTime.Now.ToString("MMMM yyyy").ToUpper();
                default: return DateTime.Now.ToString("dd/MM/yyyy");
            }
        }

        private void btnFinalizarCorte_Click(object sender, RoutedEventArgs e)
        {
            var listaMovimientos = dgCorte.ItemsSource as List<MovimientoVentaClass>;
            if (listaMovimientos == null || !listaMovimientos.Any())
            {
                MessageBox.Show("No hay movimientos registrados en este periodo para generar un reporte.", "Atención", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string seleccionPeriodo = (cmbPeriodo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Hoy";

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Archivo PDF (*.pdf)|*.pdf",
                FileName = $"Corte_Caja_{seleccionPeriodo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                Title = "Guardar Reporte de Corte de Caja"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        iTextDocument doc = new iTextDocument(PageSize.A4, 36, 36, 36, 36);
                        PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                        doc.Open();

                        // Fuentes corporativas
                        iTextFont fontTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, BaseColor.BLACK);
                        iTextFont fontSubtitulo = FontFactory.GetFont(FontFactory.HELVETICA, 11, BaseColor.DARK_GRAY);
                        iTextFont fontSeccion = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13, BaseColor.BLACK);
                        iTextFont fontDiaHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, new BaseColor(31, 97, 141)); // Azul para resaltar el día
                        iTextFont fontTablaHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.WHITE);
                        iTextFont fontTablaBody = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);
                        iTextFont fontResumenBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.BLACK);
                        iTextFont fontResumenValue = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

                        // Encabezado Principal
                        iTextParagraph pTitulo = new iTextParagraph("PUNTO FLOWER - REPORTE DE CAJA", fontTitulo);
                        doc.Add(pTitulo);

                        iTextParagraph pMeta = new iTextParagraph($"Periodo Evaluado: {ObtenerRangoFechasTexto(seleccionPeriodo)} | Fecha de Emisión: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\nGenerado por: {Session.UsuarioActual}", fontSubtitulo);
                        pMeta.SpacingAfter = 20;
                        doc.Add(pMeta);

                        // SECCIÓN 1: RESUMEN FINANCIERO MÉTODOS DE PAGO (ACUMULADO TOTAL DEL PERIODO)
                        iTextParagraph secFinanzas = new iTextParagraph("1. Resumen Acumulado de Balances y Cuentas", fontSeccion);
                        secFinanzas.SpacingAfter = 8;
                        doc.Add(secFinanzas);

                        PdfPTable tablaResumen = new PdfPTable(2);
                        tablaResumen.WidthPercentage = 100;
                        tablaResumen.SetWidths(new float[] { 70f, 30f });
                        tablaResumen.SpacingAfter = 20;

                        Action<PdfPTable, string, string, bool> agregarCeldaResumen = (tabla, clave, valor, esDestacado) =>
                        {
                            BaseColor fondo = esDestacado ? new BaseColor(235, 245, 251) : BaseColor.WHITE;
                            iTextFont fClave = esDestacado ? FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.BLACK) : fontResumenBold;
                            iTextFont fVal = esDestacado ? FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, new BaseColor(46, 134, 193)) : fontResumenValue;

                            PdfPCell cellClave = new PdfPCell(new Phrase(clave, fClave)) { Padding = 6, BackgroundColor = fondo, BorderColor = BaseColor.LIGHT_GRAY };
                            PdfPCell cellValor = new PdfPCell(new Phrase(valor, fVal)) { Padding = 6, BackgroundColor = fondo, HorizontalAlignment = Element.ALIGN_RIGHT, BorderColor = BaseColor.LIGHT_GRAY };
                            tabla.AddCell(cellClave);
                            tabla.AddCell(cellValor);
                        };

                        decimal totalIngresosCalculados = acumuladoEfectivo + acumuladoTarjeta + acumuladoTransfCuenta1 + acumuladoTransfCuenta2;

                        agregarCeldaResumen(tablaResumen, "Efectivo Físico Neto Real en Caja:", acumuladoEfectivo.ToString("C"), true);
                        agregarCeldaResumen(tablaResumen, "Ventas Cobradas en Tarjeta:", acumuladoTarjeta.ToString("C"), false);
                        agregarCeldaResumen(tablaResumen, $"Transferencias de Cuenta - {nombreEncargado1}:", acumuladoTransfCuenta1.ToString("C"), false);
                        agregarCeldaResumen(tablaResumen, $"Transferencias de Cuenta - {nombreEncargado2}:", acumuladoTransfCuenta2.ToString("C"), false);
                        agregarCeldaResumen(tablaResumen, "Total Descuentos Otorgados en el Periodo:", acumuladoDescuentos.ToString("C"), false);
                        agregarCeldaResumen(tablaResumen, "Gran Total Ingresos Brutos (Suma de Todos los Canales):", totalIngresosCalculados.ToString("C"), false);

                        doc.Add(tablaResumen);

                        // SECCIÓN 2: AUDITORÍA DE MOVIMIENTOS - AGRUPADA POR DÍA (ORDEN CRONOLÓGICO)
                        iTextParagraph secAuditoria = new iTextParagraph("2. Desglose de Ventas por Jornadas (Días)", fontSeccion);
                        secAuditoria.SpacingAfter = 12;
                        doc.Add(secAuditoria);

                        // CORRECCIÓN AQUÍ: Usamos .OrderBy para que ordene cronológicamente de la fecha más vieja a la más nueva
                        var movimientosAgrupadosPorDia = listaMovimientos
                            .GroupBy(m => m.Fecha.Date)
                            .OrderBy(g => g.Key);

                        foreach (var grupoDia in movimientosAgrupadosPorDia)
                        {
                            string nombreDiaTexto = grupoDia.Key.ToString("dddd dd 'de' MMMM 'de' yyyy").ToUpper();
                            decimal totalVendidoDelDia = grupoDia.Sum(v => v.Total);

                            iTextParagraph pDiaHeader = new iTextParagraph($"■ {nombreDiaTexto} — (Total del Día: {totalVendidoDelDia:C})", fontDiaHeader);
                            pDiaHeader.SpacingBefore = 10;
                            pDiaHeader.SpacingAfter = 6;
                            doc.Add(pDiaHeader);

                            PdfPTable tablaVentasDia = new PdfPTable(6);
                            tablaVentasDia.WidthPercentage = 100;
                            tablaVentasDia.SetWidths(new float[] { 10f, 15f, 35f, 18f, 10f, 12f });
                            tablaVentasDia.SpacingAfter = 15;

                            string[] headers = { "Id", "Hora", "Concepto de Venta", "Método Pago", "Desc.", "Importe" };
                            foreach (var header in headers)
                            {
                                PdfPCell cellHeader = new PdfPCell(new Phrase(header, fontTablaHeader))
                                {
                                    BackgroundColor = new BaseColor(52, 73, 94),
                                    HorizontalAlignment = Element.ALIGN_CENTER,
                                    Padding = 5
                                };
                                tablaVentasDia.AddCell(cellHeader);
                            }

                            // Ordenar internamente los movimientos de cada día por hora cronológica (de mañana a noche)
                            foreach (var v in grupoDia.OrderBy(m => m.Fecha.TimeOfDay))
                            {
                                tablaVentasDia.AddCell(new PdfPCell(new Phrase(v.Id.ToString(), fontTablaBody)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 4 });
                                tablaVentasDia.AddCell(new PdfPCell(new Phrase(v.Fecha.ToString("HH:mm:ss"), fontTablaBody)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 4 });
                                tablaVentasDia.AddCell(new PdfPCell(new Phrase(v.ProductoNombre, fontTablaBody)) { Padding = 4 });
                                tablaVentasDia.AddCell(new PdfPCell(new Phrase(v.MetodoPagoVisual, fontTablaBody)) { Padding = 4 });
                                tablaVentasDia.AddCell(new PdfPCell(new Phrase(v.Descuento > 0 ? v.Descuento.ToString("C") : "—", fontTablaBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                                tablaVentasDia.AddCell(new PdfPCell(new Phrase(v.Total.ToString("C"), fontTablaBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                            }

                            doc.Add(tablaVentasDia);
                        }

                        // Bloque de Firmas de Conformidad
                        iTextParagraph pFirmasSpace = new iTextParagraph("\n\n\n");
                        doc.Add(pFirmasSpace);

                        PdfPTable tablaFirmas = new PdfPTable(2);
                        tablaFirmas.WidthPercentage = 100;
                        tablaFirmas.SetWidths(new float[] { 50f, 50f });

                        PdfPCell cellFirma1 = new PdfPCell(new Phrase("___________________________\nFirma de Empleado en Turno", fontResumenBold)) { Border = PdfPCell.NO_BORDER, HorizontalAlignment = Element.ALIGN_CENTER };
                        PdfPCell cellFirma2 = new PdfPCell(new Phrase("___________________________\nFirma de Validación / Admin", fontResumenBold)) { Border = PdfPCell.NO_BORDER, HorizontalAlignment = Element.ALIGN_CENTER };

                        tablaFirmas.AddCell(cellFirma1);
                        tablaFirmas.AddCell(cellFirma2);
                        doc.Add(tablaFirmas);

                        doc.Close();
                    }

                    MessageBox.Show("El reporte PDF del corte de caja se ha generado agrupado por días de manera exitosa.", "Operación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ocurrió un error al intentar compilar e agrupar el PDF: {ex.Message}", "Error de Exportación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImprimirTicketTermico() { /* Lógica de impresión igual */ }
        private void DrawTicketPage(object sender, PrintPageEventArgs e) { /* Lógica de impresión igual */ }
    }
}