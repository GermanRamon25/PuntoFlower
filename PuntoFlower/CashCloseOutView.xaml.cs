using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;
using PuntoFlower.Data;
using PuntoFlower.Models;
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
        public int? ProductoId { get; set; }
        public int Cantidad { get; set; }
    }

    public partial class CashCloseOutView : UserControl
    {
        private decimal acumuladoEfectivo = 0;
        private decimal acumuladoTarjeta = 0;
        private decimal acumuladoDescuentos = 0;
        private decimal acumuladoGastosEfectivo = 0;

        private Dictionary<string, decimal> balanceTransferenciasPorEncargado = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        private List<string> listaEncargadosIdentificados = new List<string>();

        private string nombreEncargado1 = "Encargado 1";
        private string nombreEncargado2 = "Encargado 2";

        // Variable bandera para evitar bucles infinitos al cargar el componente
        private bool esCargaInicial = true;

        public CashCloseOutView()
        {
            InitializeComponent();
            txtEmpleadoEnTurno.Text = $"Empleado en turno: {Session.UsuarioActual}";

            EvaluarVisibilidadBotonEliminar();

            this.IsVisibleChanged += (s, e) => {
                if ((bool)e.NewValue)
                {
                    EvaluarVisibilidadBotonEliminar();

                    // Al regresar a la pestaña, volvemos a leer el fondo desde la base de datos
                    CargarFondoCajaDesdeBD();
                    ProcesarCorteFiltrado();
                }
            };
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (dpDesdeCorte != null)
                dpDesdeCorte.SelectedDate = DateTime.Today;

            if (dpHastaCorte != null)
                dpHastaCorte.SelectedDate = DateTime.Today;

            EvaluarVisibilidadBotonEliminar();

            // Primera carga del fondo al iniciar la vista
            CargarFondoCajaDesdeBD();
            ProcesarCorteFiltrado();
        }

        // Método auxiliar para recuperar de la BD y pintar en el TextBox sin disparar eventos erróneos
        private void CargarFondoCajaDesdeBD()
        {
            if (txtFondoCaja == null) return;
            esCargaInicial = true;
            ConexionDB db = new ConexionDB();
            decimal fondoGuardado = db.ObtenerFondoCaja();
            txtFondoCaja.Text = fondoGuardado.ToString("F2");
            esCargaInicial = false;
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

        private void btnGenerarCorte_Click(object sender, RoutedEventArgs e)
        {
            ProcesarCorteFiltrado();
        }

        private void ProcesarCorteFiltrado()
        {
            List<MovimientoVentaClass> ventasFiltradas = new List<MovimientoVentaClass>();
            decimal sumaRecibido = 0;
            decimal sumaCambio = 0;

            acumuladoEfectivo = 0;
            acumuladoTarjeta = 0;
            acumuladoDescuentos = 0;
            acumuladoGastosEfectivo = 0;

            balanceTransferenciasPorEncargado.Clear();
            listaEncargadosIdentificados.Clear();

            ConexionDB db = new ConexionDB();
            nombreEncargado1 = db.ObtenerEncargadoCuenta1();
            nombreEncargado2 = db.ObtenerEncargadoCuenta2();

            if (!string.IsNullOrWhiteSpace(nombreEncargado1))
            {
                string[] depto = nombreEncargado1.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var n in depto)
                {
                    string limpio = n.Trim();
                    if (!string.IsNullOrWhiteSpace(limpio) && !listaEncargadosIdentificados.Contains(limpio, StringComparer.OrdinalIgnoreCase))
                    {
                        listaEncargadosIdentificados.Add(limpio);
                        balanceTransferenciasPorEncargado[limpio] = 0;
                    }
                }
            }

            DateTime fechaDesde = dpDesdeCorte?.SelectedDate ?? DateTime.Today;
            DateTime fechaHasta = dpHastaCorte?.SelectedDate ?? DateTime.Today;

            if (txtTituloCorte != null)
                txtTituloCorte.Text = $"Corte: {fechaDesde:dd/MM/yyyy} al {fechaHasta:dd/MM/yyyy}";

            fechaHasta = fechaHasta.Date.AddDays(1).AddTicks(-1);

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = @"SELECT Id, Fecha, ProductoNombre, Total, MontoRecibido, MontoCambio, MetodoPago, 
                                            CuentaTransferencia, DescuentoAplicado, NumeroReferencia, ProductoId, Cantidad 
                                     FROM Ventas 
                                     WHERE Fecha BETWEEN @desde AND @hasta
                                     ORDER BY Fecha DESC";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@desde", fechaDesde);
                    cmd.Parameters.AddWithValue("@hasta", fechaHasta);

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            decimal recibido = r["MontoRecibido"] != DBNull.Value ? Convert.ToDecimal(r["MontoRecibido"]) : 0;
                            decimal cambio = r["MontoCambio"] != DBNull.Value ? Convert.ToDecimal(r["MontoCambio"]) : 0;
                            decimal totalVenta = Convert.ToDecimal(r["Total"]);
                            string metodo = r["MetodoPago"]?.ToString() ?? "Efectivo";
                            string cuenta = r["CuentaTransferencia"] != DBNull.Value ? r["CuentaTransferencia"].ToString().Trim() : "";
                            decimal desc = r["DescuentoAplicado"] != DBNull.Value ? Convert.ToDecimal(r["DescuentoAplicado"]) : 0;
                            string referencia = r["NumeroReferencia"] != DBNull.Value ? r["NumeroReferencia"].ToString() : "";

                            if (recibido < 0)
                            {
                                acumuladoGastosEfectivo += (recibido * -1);
                            }
                            else
                            {
                                sumaRecibido += (recibido - cambio);
                            }

                            if (cambio > 0) sumaCambio += cambio;
                            acumuladoDescuentos += desc;

                            if (metodo == "Efectivo")
                            {
                                acumuladoEfectivo += (recibido - cambio);
                            }
                            else if (metodo == "Tarjeta" || metodo.Contains("Tarjeta"))
                            {
                                acumuladoTarjeta += totalVenta;
                            }
                            else if (metodo == "Transferencia" || metodo.Contains("Transferencia"))
                            {
                                if (!string.IsNullOrEmpty(cuenta))
                                {
                                    if (balanceTransferenciasPorEncargado.ContainsKey(cuenta))
                                    {
                                        balanceTransferenciasPorEncargado[cuenta] += totalVenta;
                                    }
                                    else
                                    {
                                        balanceTransferenciasPorEncargado[cuenta] = totalVenta;
                                        if (!listaEncargadosIdentificados.Contains(cuenta, StringComparer.OrdinalIgnoreCase))
                                        {
                                            listaEncargadosIdentificados.Add(cuenta);
                                        }
                                    }
                                }
                                else
                                {
                                    acumuladoEfectivo += totalVenta;
                                }
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
                                Descuento = desc,
                                ProductoId = r["ProductoId"] != DBNull.Value ? (int?)Convert.ToInt32(r["ProductoId"]) : null,
                                Cantidad = r["Cantidad"] != DBNull.Value ? Convert.ToInt32(r["Cantidad"]) : 1
                            });
                        }
                    }
                }
                dgCorte.ItemsSource = ventasFiltradas;
                txtTotalRecibido.Text = sumaRecibido.ToString("C");
                txtTotalGastosTurno.Text = acumuladoGastosEfectivo.ToString("C");
                txtTotalCambio.Text = sumaCambio.ToString("C");

                // SE LEE EL FONDO DIRECTO DE LA INTERFAZ (EL CUAL YA SE CARGÓ DE LA BD)
                decimal fondoActual = 0;
                if (txtFondoCaja != null)
                {
                    string txtLimpio = txtFondoCaja.Text.Replace("$", "").Replace(",", "").Trim();
                    decimal.TryParse(txtLimpio, out fondoActual);
                }

                decimal efectivoTotalConFondo = acumuladoEfectivo + fondoActual;
                txtEfectivoReal.Text = efectivoTotalConFondo.ToString("C");
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

                            if (ventaSeleccionada.ProductoId.HasValue && ventaSeleccionada.ProductoId.Value > 0)
                            {
                                string qUpdateStockById = "UPDATE Productos SET StockActual = StockActual + @c WHERE Id = @prodId";
                                using (SqlCommand cmdUpId = new SqlCommand(qUpdateStockById, con, tra))
                                {
                                    cmdUpId.Parameters.AddWithValue("@c", ventaSeleccionada.Cantidad);
                                    cmdUpId.Parameters.AddWithValue("@prodId", ventaSeleccionada.ProductoId.Value);
                                    cmdUpId.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                string queryInsumos = "SELECT Id, Nombre, PrecioVenta FROM Productos WHERE Categoria = 'Venta'";
                                List<Tuple<int, string, decimal>> listaProductosConfig = new List<Tuple<int, string, decimal>>();

                                using (SqlCommand cmdGet = new SqlCommand(queryInsumos, con, tra))
                                {
                                    using (SqlDataReader reader = cmdGet.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            listaProductosConfig.Add(new Tuple<int, string, decimal>(
                                                Convert.ToInt32(reader["Id"]),
                                                reader["Nombre"].ToString(),
                                                Convert.ToDecimal(reader["PrecioVenta"])
                                            ));
                                        }
                                    }
                                }

                                foreach (var prod in listaProductosConfig)
                                {
                                    if (concepto.ToUpper().Contains(prod.Item2.ToUpper()) || prod.Item2.ToUpper().Contains(concepto.ToUpper()))
                                    {
                                        int piezasRegresar = ventaSeleccionada.Cantidad;
                                        if (piezasRegresar <= 0 && prod.Item3 > 0)
                                            piezasRegresar = (int)Math.Round(ventaSeleccionada.Total / prod.Item3);

                                        if (piezasRegresar <= 0) piezasRegresar = 1;

                                        string qUpdateStockByName = "UPDATE Productos SET StockActual = StockActual + @c WHERE Id = @nomId";
                                        using (SqlCommand cmdUpName = new SqlCommand(qUpdateStockByName, con, tra))
                                        {
                                            cmdUpName.Parameters.AddWithValue("@c", piezasRegresar);
                                            cmdUpName.Parameters.AddWithValue("@nomId", prod.Item1);
                                            cmdUpName.ExecuteNonQuery();
                                        }
                                        break;
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

        private void btnFinalizarCorte_Click(object sender, RoutedEventArgs e)
        {
            var listaMovimientos = dgCorte.ItemsSource as List<MovimientoVentaClass>;
            if (listaMovimientos == null || !listaMovimientos.Any())
            {
                MessageBox.Show("No hay movimientos registrados en este periodo para generar un reporte.", "Atención", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DateTime fechaDesde = dpDesdeCorte?.SelectedDate ?? DateTime.Today;
            DateTime fechaHasta = dpHastaCorte?.SelectedDate ?? DateTime.Today;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Archivo PDF (*.pdf)|*.pdf",
                FileName = $"Corte_Caja_{fechaDesde:yyyyMMdd}_A_{fechaHasta:yyyyMMdd}.pdf",
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

                        iTextFont fontTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, BaseColor.BLACK);
                        iTextFont fontSubtitulo = FontFactory.GetFont(FontFactory.HELVETICA, 11, BaseColor.DARK_GRAY);
                        iTextFont fontSeccion = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13, BaseColor.BLACK);
                        iTextFont fontDiaHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, new BaseColor(31, 97, 141));
                        iTextFont fontTablaHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.WHITE);
                        iTextFont fontTablaBody = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);
                        // NUEVO: Fuente en cursiva/itálica para resaltar el fondo virtual en la tabla de abajo
                        iTextFont fontTablaVirtual = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, new BaseColor(46, 134, 193));
                        iTextFont fontResumenBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.BLACK);
                        iTextFont fontResumenValue = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

                        iTextParagraph pTitulo = new iTextParagraph("PUNTO FLOWER - REPORTE DE CAJA", fontTitulo);
                        doc.Add(pTitulo);

                        iTextParagraph pMeta = new iTextParagraph($"Rango Evaluado: Desde {fechaDesde:dd/MM/yyyy} Hasta {fechaHasta:dd/MM/yyyy} | Fecha de Emisión: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\nGenerado por: {Session.UsuarioActual}", fontSubtitulo);
                        pMeta.SpacingAfter = 20;
                        doc.Add(pMeta);

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

                        // CAPTURAMOS EL FONDO DE CAJA ACTUAL DE LA INTERFAZ PARA EL PDF
                        decimal fondoCajaPDF = 0;
                        if (txtFondoCaja != null)
                        {
                            string txtLimpio = txtFondoCaja.Text.Replace("$", "").Replace(",", "").Trim();
                            decimal.TryParse(txtLimpio, out fondoCajaPDF);
                        }

                        decimal totalVentasBrutasEfectivo = acumuladoEfectivo + acumuladoGastosEfectivo;

                        decimal totalTransferenciasAcumuladas = 0;
                        foreach (var b in balanceTransferenciasPorEncargado.Values) totalTransferenciasAcumuladas += b;

                        decimal efectivoNetoTotalConFondo = acumuladoEfectivo + fondoCajaPDF;
                        decimal totalIngresosCalculados = totalVentasBrutasEfectivo + acumuladoTarjeta + totalTransferenciasAcumuladas;

                        // Sección Superior: Totales Acumulados
                        agregarCeldaResumen(tablaResumen, "Fondo Inversión / Dinero Base de Caja Inicial (+):", fondoCajaPDF.ToString("C"), false);
                        agregarCeldaResumen(tablaResumen, "Ingresos Brutos en Efectivo (Ventas de Mostrador):", totalVentasBrutasEfectivo.ToString("C"), false);
                        agregarCeldaResumen(tablaResumen, "Gastos de Turno Pagados en Efectivo (-):", acumuladoGastosEfectivo.ToString("C"), false);
                        agregarCeldaResumen(tablaResumen, "TOTAL ABSOLUTO FÍSICO NETO EN CAJA CON FONDO (=):", efectivoNetoTotalConFondo.ToString("C"), true);
                        agregarCeldaResumen(tablaResumen, "Ventas Cobradas en Tarjeta:", acumuladoTarjeta.ToString("C"), false);

                        foreach (var encargado in listaEncargadosIdentificados)
                        {
                            decimal montoEncargado = balanceTransferenciasPorEncargado.ContainsKey(encargado) ? balanceTransferenciasPorEncargado[encargado] : 0;
                            agregarCeldaResumen(tablaResumen, $"Transferencias de Cuenta - {encargado}:", montoEncargado.ToString("C"), false);
                        }

                        agregarCeldaResumen(tablaResumen, "Total Descuentos Otorgados en el Periodo:", acumuladoDescuentos.ToString("C"), false);
                        agregarCeldaResumen(tablaResumen, "Gran Total Ingresos Brutos Combinados (Todos los Canales):", totalIngresosCalculados.ToString("C"), false);

                        doc.Add(tablaResumen);

                        iTextParagraph secAuditoria = new iTextParagraph("2. Desglose de Ventas por Jornadas (Días)", fontSeccion);
                        secAuditoria.SpacingAfter = 12;
                        doc.Add(secAuditoria);

                        var movimientosAgrupadosPorDia = listaMovimientos
                            .GroupBy(m => m.Fecha.Date)
                            .OrderBy(g => g.Key);

                        foreach (var grupoDia in movimientosAgrupadosPorDia)
                        {
                            string nombreDiaTexto = grupoDia.Key.ToString("dddd dd 'de' MMMM 'de' yyyy").ToUpper();
                            decimal totalVendidoDelDia = grupoDia.Sum(v => v.Total);

                            // Ajuste contable del header por día: sumamos el fondo de caja al encabezado de la jornada si aplica
                            decimal totalDiaConFondo = totalVendidoDelDia + fondoCajaPDF;

                            iTextParagraph pDiaHeader = new iTextParagraph($"■ {nombreDiaTexto} — (Balance de Jornada + Fondo Inicial: {totalDiaConFondo:C})", fontDiaHeader);
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

                            // ==========================================================
                            // INYECCIÓN VISUAL DEL FONDO DE CAJA COMO PRIMERA FILA DEL DESGLOSE
                            // ==========================================================
                            if (fondoCajaPDF > 0)
                            {
                                BaseColor fondoVirtualColor = new BaseColor(244, 246, 247); // Gris muy tenue para distinguirlo de las ventas puras

                                PdfPCell cellId = new PdfPCell(new Phrase("INICIO", fontTablaVirtual)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 4, BackgroundColor = fondoVirtualColor };
                                PdfPCell cellHora = new PdfPCell(new Phrase("00:00:00", fontTablaVirtual)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 4, BackgroundColor = fondoVirtualColor };
                                PdfPCell cellConcepto = new PdfPCell(new Phrase("APERTURA: FONDO BASE DE CAJA DE RELEVACIÓN", fontTablaVirtual)) { Padding = 4, BackgroundColor = fondoVirtualColor };
                                PdfPCell cellMetodo = new PdfPCell(new Phrase("Efectivo Fijo", fontTablaVirtual)) { Padding = 4, BackgroundColor = fondoVirtualColor };
                                PdfPCell cellDesc = new PdfPCell(new Phrase("—", fontTablaVirtual)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4, BackgroundColor = fondoVirtualColor };
                                PdfPCell cellImporte = new PdfPCell(new Phrase(fondoCajaPDF.ToString("C"), fontTablaVirtual)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4, BackgroundColor = fondoVirtualColor };

                                tablaVentasDia.AddCell(cellId);
                                tablaVentasDia.AddCell(cellHora);
                                tablaVentasDia.AddCell(cellConcepto);
                                tablaVentasDia.AddCell(cellMetodo);
                                tablaVentasDia.AddCell(cellDesc);
                                tablaVentasDia.AddCell(cellImporte);
                            }

                            // Renderizado ordenado del resto de las transacciones del día
                            foreach (var v in grupoDia.OrderBy(m => m.Fecha.TimeOfDay))
                            {
                                string importeTexto = v.Total < 0 ? $"-{Math.Abs(v.Total):C}" : v.Total.ToString("C");

                                string metodoPagoCelda = v.MetodoPagoVisual;
                                if ((v.MetodoPagoPuro == "Transferencia" || v.MetodoPagoPuro.Contains("Transferencia")) &&
                                    !string.IsNullOrEmpty(v.NumeroReferencia) && v.NumeroReferencia != "—")
                                {
                                    metodoPagoCelda += $"\n[Ref: {v.NumeroReferencia}]";
                                }

                                tablaVentasDia.AddCell(new PdfPCell(new Phrase(v.Id.ToString(), fontTablaBody)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 4 });
                                tablaVentasDia.AddCell(new PdfPCell(new Phrase(v.Fecha.ToString("HH:mm:ss"), fontTablaBody)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 4 });
                                tablaVentasDia.AddCell(new PdfPCell(new Phrase(v.ProductoNombre, fontTablaBody)) { Padding = 4 });
                                tablaVentasDia.AddCell(new PdfPCell(new Phrase(metodoPagoCelda, fontTablaBody)) { Padding = 4 });
                                tablaVentasDia.AddCell(new PdfPCell(new Phrase(v.Descuento > 0 ? v.Descuento.ToString("C") : "—", fontTablaBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                                tablaVentasDia.AddCell(new PdfPCell(new Phrase(importeTexto, fontTablaBody)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                            }

                            doc.Add(tablaVentasDia);
                        }

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
                    MessageBox.Show($"Ocurrió un error al intentar compilar el PDF: {ex.Message}", "Error de Exportación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImprimirTicketTermico() { /* Lógica de impresión igual */ }
        private void DrawTicketPage(object sender, PrintPageEventArgs e) { /* Lógica de impresión igual */ }

        // ========================================================
        // MODIFICADO: PERSISTENCIA EN BD EN TIEMPO REAL
        // ========================================================
        private void txtFondoCaja_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtEfectivoReal == null || txtFondoCaja == null || esCargaInicial) return;

            string textoLimpio = txtFondoCaja.Text.Trim();
            decimal fondoIngresado = 0;

            if (!string.IsNullOrEmpty(textoLimpio))
            {
                textoLimpio = textoLimpio.Replace("$", "").Replace(",", "");
                decimal.TryParse(textoLimpio, out fondoIngresado);
            }

            // GUARDAR AUTOMÁTICAMENTE EN LA BASE DE DATOS
            ConexionDB db = new ConexionDB();
            db.GuardarFondoCaja(fondoIngresado);

            // Actualizar la interfaz
            decimal efectivoTotalConFondo = acumuladoEfectivo + fondoIngresado;
            txtEfectivoReal.Text = efectivoTotalConFondo.ToString("C");
        }
    }
}