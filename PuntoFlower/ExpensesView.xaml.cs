using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PuntoFlower.Data;
using PuntoFlower.Models;

// Alias para iTextSharp (Uso seguro sin fallos de casteo)
using iTextFont = iTextSharp.text.Font;
using iTextParagraph = iTextSharp.text.Paragraph;
using iTextDocument = iTextSharp.text.Document;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace PuntoFlower.Views
{
    public partial class ExpensesView : UserControl
    {
        private bool esPerfilEmpleado = false;

        public ExpensesView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            EvaluarPerfilYRestringirAcceso();
            CargarGastosDeLaBase();
        }

        private void EvaluarPerfilYRestringirAcceso()
        {
            string rolUsuario = "";
            try { rolUsuario = Session.RolActual?.ToString() ?? ""; } catch { rolUsuario = Session.UsuarioActual?.ToString() ?? ""; }

            if (rolUsuario.Equals("Empleado", StringComparison.OrdinalIgnoreCase) ||
                rolUsuario.Equals("User", StringComparison.OrdinalIgnoreCase) ||
                (!rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
                 !rolUsuario.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
                 !Session.UsuarioActual.Equals("leticia", StringComparison.OrdinalIgnoreCase)))
            {
                esPerfilEmpleado = true;

                if (tiComprasProveedores != null) tiComprasProveedores.Visibility = Visibility.Collapsed;

                if (cbiTarjeta != null) cbiTarjeta.Visibility = Visibility.Collapsed;
                if (cbiTransferencia != null) cbiTransferencia.Visibility = Visibility.Collapsed;
                if (cbMetodoGasto != null)
                {
                    cbMetodoGasto.SelectedIndex = 0;
                    cbMetodoGasto.IsEnabled = false;
                }

                if (cbCategoria != null) cbCategoria.SelectedIndex = 0;
                if (panelCategoria != null) panelCategoria.Visibility = Visibility.Collapsed;

                if (lblTituloGastos != null) lblTituloGastos.Text = "Registro Operativo de Gastos de Servicios";
            }
            else
            {
                esPerfilEmpleado = false;
                if (tiComprasProveedores != null) tiComprasProveedores.Visibility = Visibility.Visible;
                if (cbiTarjeta != null) cbiTarjeta.Visibility = Visibility.Visible;
                if (cbiTransferencia != null) cbiTransferencia.Visibility = Visibility.Visible;
                if (cbMetodoGasto != null) cbMetodoGasto.IsEnabled = true;
                if (panelCategoria != null) panelCategoria.Visibility = Visibility.Visible;
                if (lblTituloGastos != null) lblTituloGastos.Text = "Gastos y Surtido de Mercancía";
            }
        }

        private void cbConceptoServicio_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (panelOtroConcepto == null) return;

            string seleccion = (cbConceptoServicio.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (seleccion == "Otro Servicio / Gasto")
            {
                panelOtroConcepto.Visibility = Visibility.Visible;
                txtDesc.Focus();
            }
            else
            {
                panelOtroConcepto.Visibility = Visibility.Collapsed;
                txtDesc.Clear();
            }
        }

        private void cmbPeriodoGastos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgGastos == null) return;
            CargarGastosDeLaBase();
        }

        private void CargarGastosDeLaBase()
        {
            List<object> historialGastos = new List<object>();
            ConexionDB db = new ConexionDB();

            string condicionFecha = "";
            string seleccion = (cmbPeriodoGastos?.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Este Mes";

            switch (seleccion)
            {
                case "Hoy":
                    condicionFecha = "WHERE CAST(Fecha AS DATE) = CAST(GETDATE() AS DATE)";
                    break;
                case "Esta Semana":
                    condicionFecha = "WHERE DATEDIFF(wk, Fecha, GETDATE()) = 0 AND Fecha <= GETDATE()";
                    break;
                case "Este Mes":
                    condicionFecha = "WHERE DATEDIFF(mm, Fecha, GETDATE()) = 0 AND Fecha <= GETDATE()";
                    break;
                case "Historial Completo":
                    condicionFecha = "";
                    break;
            }

            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = $@"SELECT Fecha, Descripcion, Categoria, MetodoPago, Monto 
                                     FROM Gastos 
                                     {condicionFecha}
                                     ORDER BY Fecha DESC";

                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            historialGastos.Add(new
                            {
                                Fecha = (DateTime)r["Fecha"],
                                Descripcion = r["Descripcion"] != DBNull.Value ? r["Descripcion"].ToString() : "",
                                Categoria = r["Categoria"] != DBNull.Value ? r["Categoria"].ToString() : "General",
                                MetodoPago = r["MetodoPago"] != DBNull.Value ? r["MetodoPago"].ToString() : "Efectivo",
                                Monto = r["Monto"] != DBNull.Value ? Convert.ToDecimal(r["Monto"]) : 0m
                            });
                        }
                    }
                }
                dgGastos.ItemsSource = historialGastos;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar el historial de egresos: " + ex.Message, "Fallo de Lectura");
            }
        }

        private void btnRegistrarGasto_Click(object sender, RoutedEventArgs e)
        {
            string descripcionFinal = "";
            string seleccionCombo = (cbConceptoServicio.SelectedItem as ComboBoxItem)?.Content.ToString();
            string SampleCategory = (cbCategoria.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Renta / Servicios";
            string metodoPago = (cbMetodoGasto.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Efectivo";

            if (seleccionCombo == "Otro Servicio / Gasto")
            {
                descripcionFinal = txtDesc.Text.Trim();
            }
            else
            {
                descripcionFinal = seleccionCombo;
            }

            if (string.IsNullOrEmpty(descripcionFinal))
            {
                MessageBox.Show("Por favor, introduce o selecciona el concepto del gasto.", "Campo Obligatorio", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtMonto.Text.Trim(), out decimal montoValidado) || montoValidado <= 0)
            {
                MessageBox.Show("Por favor, introduce un importe numérico válido y mayor a cero.", "Monto Incorrecto", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    using (SqlTransaction transaccion = con.BeginTransaction())
                    {
                        try
                        {
                            // 1. REGISTRO EN BITÁCORA DE GASTOS: Guarda el monto en positivo de forma normal
                            string queryGasto = @"INSERT INTO Gastos (Descripcion, Monto, Fecha, Categoria, MetodoPago) 
                                                 VALUES (@desc, @monto, GETDATE(), @cat, @metodo)";

                            using (SqlCommand cmdGasto = new SqlCommand(queryGasto, con, transaccion))
                            {
                                cmdGasto.Parameters.AddWithValue("@desc", descripcionFinal);
                                cmdGasto.Parameters.AddWithValue("@monto", montoValidado);
                                cmdGasto.Parameters.AddWithValue("@cat", SampleCategory);
                                cmdGasto.Parameters.AddWithValue("@metodo", metodoPago);
                                cmdGasto.ExecuteNonQuery();
                            }

                            // 2. REFLEJO EN FLUJO DE CAJA: Si es en Efectivo, inyecta un contra-registro negativo en Ventas
                            if (metodoPago == "Efectivo")
                            {
                                // Ponemos el monto en negativo en la columna Total y MontoRecibido para que la fórmula del Corte Diario lo reste
                                string queryCajaSalida = @"INSERT INTO Ventas (Fecha, ProductoNombre, Cantidad, Total, MetodoPago, MontoRecibido, MontoCambio, DescuentoAplicado) 
                                                          VALUES (GETDATE(), @conceptoSalida, 1, @montoNegativo, 'Efectivo', @montoNegativo, 0, 0)";

                                using (SqlCommand cmdCaja = new SqlCommand(queryCajaSalida, con, transaccion))
                                {
                                    cmdCaja.Parameters.AddWithValue("@conceptoSalida", $"Salida de Caja (Gasto): {descripcionFinal}");
                                    cmdCaja.Parameters.AddWithValue("@montoNegativo", montoValidado * -1);
                                    cmdCaja.ExecuteNonQuery();
                                }
                            }

                            transaccion.Commit();
                        }
                        catch
                        {
                            transaccion.Rollback();
                            throw;
                        }
                    }
                }

                string msgExito = $"¡Gasto por '{descripcionFinal}' guardado exitosamente!";
                if (metodoPago == "Efectivo") msgExito += "\nEl importe fue descontado del corte de caja diario y del acumulado mensual de ventas.";

                MessageBox.Show(msgExito, "Egreso Confirmado", MessageBoxButton.OK, MessageBoxImage.Information);

                txtMonto.Clear();
                txtDesc.Clear();
                if (cbConceptoServicio != null) cbConceptoServicio.SelectedIndex = 0;

                CargarGastosDeLaBase();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar el gasto e impactar la caja: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnImprimirPDF_Click(object sender, RoutedEventArgs e)
        {
            if (dgGastos.ItemsSource == null || !dgGastos.ItemsSource.Cast<object>().Any())
            {
                MessageBox.Show("No hay egresos registrados en la lista para exportar un reporte.", "Aviso");
                return;
            }

            string periodoSeleccionado = (cmbPeriodoGastos.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Este Mes";

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PDF Files (*.pdf)|*.pdf";
            sfd.FileName = $"Reporte_Egresos_{periodoSeleccionado.Replace(" ", "")}_{DateTime.Now:yyyyMMdd}.pdf";

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ConexionDB db = new ConexionDB();
                    string sucursalNombre = db.ObtenerNombreSucursal();

                    decimal totalGeneral = 0;
                    decimal totalEfectivoPuro = 0;

                    foreach (dynamic item in dgGastos.ItemsSource)
                    {
                        totalGeneral += item.Monto;
                        if (item.MetodoPago == "Efectivo") totalEfectivoPuro += item.Monto;
                    }

                    iTextDocument doc = new iTextDocument(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(sfd.FileName, FileMode.Create));
                    doc.Open();

                    iTextFont fTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 15, BaseColor.BLACK);
                    iTextFont fSub = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.DARK_GRAY);
                    iTextFont fCuerpo = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);
                    iTextFont fBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.BLACK);
                    iTextFont fTablaHead = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.WHITE);

                    BaseColor azulMarino = new BaseColor(44, 62, 80);

                    doc.Add(new iTextParagraph($"PUNTO FLOWER - REPORTE DE AUDITORÍA DE EGRESOS ({periodoSeleccionado.ToUpper()})", fTitulo));
                    doc.Add(new iTextParagraph($"Sucursal: {sucursalNombre}", fBold));
                    doc.Add(new iTextParagraph($"Fecha de Emisión: {DateTime.Now:g}", fCuerpo));
                    doc.Add(new iTextParagraph($"Generado por: {Session.UsuarioActual}", fCuerpo));
                    doc.Add(new iTextParagraph("----------------------------------------------------------------------------------------------------------------------------------"));
                    doc.Add(new iTextParagraph(" "));

                    PdfPTable tablaResumenEstructura = new PdfPTable(2);
                    tablaResumenEstructura.WidthPercentage = 100;
                    tablaResumenEstructura.SetWidths(new float[] { 50f, 50f });

                    PdfPCell cellTot = new PdfPCell(new Phrase($"TOTAL ACUMULADO EN PERIODO:\n{totalGeneral:C}", fBold)) { BackgroundColor = new BaseColor(253, 237, 236), Padding = 8, HorizontalAlignment = Element.ALIGN_CENTER };
                    PdfPCell cellEf = new PdfPCell(new Phrase($"TOTAL RETIRADO EN EFECTIVO:\n{totalEfectivoPuro:C}", fBold)) { BackgroundColor = new BaseColor(234, 242, 248), Padding = 8, HorizontalAlignment = Element.ALIGN_CENTER };

                    tablaResumenEstructura.AddCell(cellTot);
                    tablaResumenEstructura.AddCell(cellEf);
                    doc.Add(tablaResumenEstructura);
                    doc.Add(new iTextParagraph(" "));

                    doc.Add(new iTextParagraph($"DETALLE CRONOLÓGICO DEL PERIODO ({periodoSeleccionado.ToUpper()})", fSub));
                    doc.Add(new iTextParagraph(" "));

                    PdfPTable tablaHistorial = new PdfPTable(5);
                    tablaHistorial.WidthPercentage = 100;
                    tablaHistorial.SetWidths(new float[] { 18f, 35f, 20f, 15f, 12f });

                    string[] headers = { "Fecha/Hora", "Descripción / Concepto", "Categoría", "Método Pago", "Monto" };
                    foreach (string h in headers)
                    {
                        tablaHistorial.AddCell(new PdfPCell(new Phrase(h, fTablaHead)) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = azulMarino, Padding = 5 });
                    }

                    foreach (dynamic item in dgGastos.ItemsSource)
                    {
                        tablaHistorial.AddCell(new PdfPCell(new Phrase(item.Fecha.ToString("dd/MM/yyyy HH:mm"), fCuerpo)) { Padding = 4 });
                        tablaHistorial.AddCell(new PdfPCell(new Phrase(item.Descripcion, fCuerpo)) { Padding = 4 });
                        tablaHistorial.AddCell(new PdfPCell(new Phrase(item.Categoria, fCuerpo)) { Padding = 4 });
                        tablaHistorial.AddCell(new PdfPCell(new Phrase(item.MetodoPago, fCuerpo)) { Padding = 4, HorizontalAlignment = Element.ALIGN_CENTER });
                        tablaHistorial.AddCell(new PdfPCell(new Phrase(item.Monto.ToString("C"), fCuerpo)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4 });
                    }

                    doc.Add(tablaHistorial);
                    doc.Add(new iTextParagraph(" "));
                    doc.Add(new iTextParagraph($"Firma de Supervisor / Administrador: ___________________________", fCuerpo));

                    doc.Close();
                    MessageBox.Show("Reporte de gastos exportado a PDF con éxito.", "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al generar el documento PDF del reporte: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}