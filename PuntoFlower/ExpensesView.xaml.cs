using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PuntoFlower.Data;

// Alias para evitar conflictos con WPF
using iTextFont = iTextSharp.text.Font;
using iTextParagraph = iTextSharp.text.Paragraph;
using iTextDocument = iTextSharp.text.Document;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace PuntoFlower.Views
{
    public partial class ExpensesView : UserControl
    {
        public ExpensesView()
        {
            InitializeComponent();
            // La carga inicial se ejecuta automáticamente mediante el SelectionChanged del ComboBox
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

        // Evento que escucha el selector de periodos de tiempo
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
            string seleccion = (cmbPeriodoGastos.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Este Mes";

            // Estructura de filtrado inteligente en SQL Server
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
                    condicionFecha = ""; // Sin restricciones, jala todo el historial
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
                MessageBox.Show("Error al cargar el historial de egresos: " + ex.Message, "Fallo Operativo");
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
                    string query = @"INSERT INTO Gastos (Descripcion, Monto, Fecha, Categoria, MetodoPago) 
                                     VALUES (@desc, @monto, GETDATE(), @cat, @metodo)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@desc", descripcionFinal);
                        cmd.Parameters.AddWithValue("@monto", montoValidado);
                        cmd.Parameters.AddWithValue("@cat", SampleCategory);
                        cmd.Parameters.AddWithValue("@metodo", metodoPago);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"¡Gasto por '{descripcionFinal}' guardado exitosamente!", "Egreso Confirmado", MessageBoxButton.OK, MessageBoxImage.Information);

                txtMonto.Clear();
                txtDesc.Clear();
                cbConceptoServicio.SelectedIndex = 0;

                CargarGastosDeLaBase();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar el gasto en la base de datos: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        if (item.MetodoPago == "Efectivo")
                        {
                            totalEfectivoPuro += item.Monto;
                        }
                    }

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
                    doc.Add(new iTextParagraph(" "));

                    doc.Add(new iTextParagraph($"DETALLE CRONOLÓGICO DEL PERIODO ({periodoSeleccionado.ToUpper()})", fSub));
                    doc.Add(new iTextParagraph(" "));

                    PdfPTable tablaHistorial = new PdfPTable(5);
                    tablaHistorial.WidthPercentage = 100;
                    tablaHistorial.SetWidths(new float[] { 18f, 35f, 20f, 15f, 12f });

                    string[] headers = { "Fecha/Hora", "Descripción / Concepto", "Categoría", "Método Pago", "Monto" };
                    foreach (string h in headers)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(h, fTablaHead)) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = azulMarino, Padding = 5 };
                        tablaHistorial.AddCell(cell);
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