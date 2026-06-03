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
// Alias para evitar conflictos con Wpf
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

                            // 1. Revertir saldo de agenda
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

                            // 2. Restaurar Stock (Lógica Universal)
                            // Buscamos el nombre del producto en el concepto de venta
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

                            // 3. Eliminar venta
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

        private string ObtenerRangoFechasTexto(string seleccion) { /* Lógica de fechas igual */ return DateTime.Now.ToString("dd/MM/yyyy"); }
        private void btnFinalizarCorte_Click(object sender, RoutedEventArgs e) { /* Lógica de PDF igual */ }
        private void ImprimirTicketTermico() { /* Lógica de impresión igual */ }
        private void DrawTicketPage(object sender, PrintPageEventArgs e) { /* Lógica de impresión igual */ }
    }
}