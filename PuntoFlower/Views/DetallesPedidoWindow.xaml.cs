using System;
using System.Data.SqlClient;
using System.Windows;
using PuntoFlower.Data;
using PuntoFlower.Models;

namespace PuntoFlower.Views
{
    public partial class DetallesPedidoWindow : Window
    {
        private int _pedidoId;
        private decimal _saldoPendienteNum;

        public DetallesPedidoWindow(dynamic pedido)
        {
            InitializeComponent();
            _pedidoId = pedido.Id;
            _saldoPendienteNum = pedido.SaldoPendiente;

            lblCliente.Text = pedido.ClienteNombre;
            lblFecha.Text = "Entrega programada: " + pedido.FechaEntrega.ToString("dd/MM/yyyy");
            lblDescripcion.Text = pedido.Descripcion;
            lblDireccion.Text = pedido.Direccion;
            txtNota.Text = pedido.NotaTarjeta;

            lblTotal.Text = string.Format("{0:C}", pedido.PrecioTotal);
            lblAnticipo.Text = string.Format("{0:C}", pedido.Anticipo);
            lblSaldo.Text = string.Format("{0:C}", _saldoPendienteNum);

            decimal envioMonto = 0;
            try { envioMonto = pedido.CostoEnvio; }
            catch { envioMonto = ConsultarCostoEnvioDeSeguridad(_pedidoId); }

            lblCostoEnvio.Text = string.Format("{0:C}", envioMonto);

            // Ocultar paneles de pago si ya está liquidado
            if (_saldoPendienteNum <= 0)
            {
                if (panelMetodoLiquidacion != null) panelMetodoLiquidacion.Visibility = Visibility.Collapsed;
                if (btnAbonar != null) btnAbonar.Visibility = Visibility.Collapsed;
                lblSaldo.Foreground = System.Windows.Media.Brushes.SeaGreen;
            }
        }

        private decimal ConsultarCostoEnvioDeSeguridad(int idPedido)
        {
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "SELECT ISNULL(CostoEnvio, 0) FROM Pedidos WHERE Id = @id";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", idPedido);
                        return Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                }
            }
            catch { return 0; }
        }

        // MEJORA #6: Registrar abonos parciales directos al corte de caja
        private void btnAbonar_Click(object sender, RoutedEventArgs e)
        {
            if (_saldoPendienteNum <= 0) return;

            string montoStr = Microsoft.VisualBasic.Interaction.InputBox(
                $"El saldo pendiente es de {_saldoPendienteNum:C}.\n¿Qué cantidad está abonando el cliente en este momento?",
                "Registrar Abono", _saldoPendienteNum.ToString("F2"));

            if (string.IsNullOrEmpty(montoStr) || !decimal.TryParse(montoStr, out decimal abono) || abono <= 0) return;

            if (abono > _saldoPendienteNum)
            {
                MessageBox.Show("El abono no puede ser mayor al saldo que debe el cliente.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string metodo = Microsoft.VisualBasic.Interaction.InputBox("Método de Pago (Efectivo, Tarjeta, Transferencia):", "Método de Pago", "Efectivo");
            if (string.IsNullOrEmpty(metodo)) return;

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                using (SqlTransaction tra = con.BeginTransaction())
                {
                    try
                    {
                        // 1. Descontar saldo y sumar al anticipo
                        string qPed = "UPDATE Pedidos SET Anticipo = Anticipo + @m, SaldoPendiente = SaldoPendiente - @m WHERE Id = @id";
                        using (SqlCommand cP = new SqlCommand(qPed, con, tra))
                        {
                            cP.Parameters.AddWithValue("@m", abono);
                            cP.Parameters.AddWithValue("@id", _pedidoId);
                            cP.ExecuteNonQuery();
                        }

                        // 2. Insertar Venta para el Corte de Caja de HOY
                        string qVen = @"INSERT INTO Ventas (Fecha, ProductoNombre, Cantidad, Total, MetodoPago, MontoRecibido, MontoCambio, DescuentoAplicado)
                                        VALUES (GETDATE(), @con, 1, @tot, @met, @tot, 0, 0)";
                        using (SqlCommand cV = new SqlCommand(qVen, con, tra))
                        {
                            cV.Parameters.AddWithValue("@con", $"Abono Pedido #{_pedidoId}: {lblCliente.Text}");
                            cV.Parameters.AddWithValue("@tot", abono);
                            cV.Parameters.AddWithValue("@met", metodo.Trim());
                            cV.ExecuteNonQuery();
                        }

                        // 3. Historial de Abonos
                        string qAb = @"INSERT INTO HistorialAbonosPedidos (PedidoId, FechaAbono, Monto, MetodoPago, UsuarioRegistro)
                                       VALUES (@id, GETDATE(), @m, @met, @usr)";
                        using (SqlCommand cA = new SqlCommand(qAb, con, tra))
                        {
                            cA.Parameters.AddWithValue("@id", _pedidoId);
                            cA.Parameters.AddWithValue("@m", abono);
                            cA.Parameters.AddWithValue("@met", metodo.Trim());
                            cA.Parameters.AddWithValue("@usr", Session.UsuarioActual ?? "Empleado");
                            cA.ExecuteNonQuery();
                        }

                        tra.Commit();

                        // Actualizar UI
                        _saldoPendienteNum -= abono;
                        lblSaldo.Text = _saldoPendienteNum.ToString("C");

                        string txtAnt = lblAnticipo.Text.Replace("$", "").Replace(",", "");
                        if (decimal.TryParse(txtAnt, out decimal antActual))
                        {
                            lblAnticipo.Text = (antActual + abono).ToString("C");
                        }

                        if (_saldoPendienteNum <= 0)
                        {
                            btnAbonar.Visibility = Visibility.Collapsed;
                            panelMetodoLiquidacion.Visibility = Visibility.Collapsed;
                            lblSaldo.Foreground = System.Windows.Media.Brushes.SeaGreen;
                        }

                        MessageBox.Show("Abono registrado con éxito. El dinero ya está reflejado en el corte de caja.", "Abono Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch
                    {
                        tra.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar el abono: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPreparar_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("En Preparación");
            MessageBox.Show("El pedido ahora aparece 'En Preparación'.", "Estatus Actualizado", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
        }

        private void btnListo_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Listo para Entregar");
            MessageBox.Show("El pedido ahora aparece como 'Listo para Entregar'.", "Estatus Actualizado", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
        }

        private void ActualizarEstado(string nuevoEstado)
        {
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "UPDATE Pedidos SET Estado = @estado WHERE Id = @id";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@estado", nuevoEstado);
                    cmd.Parameters.AddWithValue("@id", _pedidoId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar el estado: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ORQUESTADOR MAESTRO DE ENTREGA: Aquí se rematan las mejoras #1, #2, #4 y #5
        private void btnEntregar_Click(object sender, RoutedEventArgs e)
        {
            var conf = MessageBox.Show("¿Confirmas que el pedido ya se entregó o está saliendo a ruta en este momento?", "Confirmar Entrega", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (conf != MessageBoxResult.Yes) return;

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                using (SqlTransaction tra = con.BeginTransaction())
                {
                    try
                    {
                        // 1. Obtenemos la radiografía actual del pedido en la base de datos
                        decimal costoEnvio = 0;
                        int? prodId = null;
                        int cantFlores = 0;
                        bool invDesc = false;
                        bool fleteCobrado = false;

                        string qInfo = "SELECT CostoEnvio, ProductoId, CantidadFlores, InventarioDescontado, FleteCobradoEnCaja FROM Pedidos WHERE Id = @id";
                        using (SqlCommand cmdInfo = new SqlCommand(qInfo, con, tra))
                        {
                            cmdInfo.Parameters.AddWithValue("@id", _pedidoId);
                            using (SqlDataReader r = cmdInfo.ExecuteReader())
                            {
                                if (r.Read())
                                {
                                    costoEnvio = Convert.ToDecimal(r["CostoEnvio"]);
                                    if (r["ProductoId"] != DBNull.Value) prodId = Convert.ToInt32(r["ProductoId"]);
                                    cantFlores = Convert.ToInt32(r["CantidadFlores"]);
                                    invDesc = Convert.ToBoolean(r["InventarioDescontado"]);
                                    fleteCobrado = Convert.ToBoolean(r["FleteCobradoEnCaja"]);
                                }
                            }
                        }

                        // 2. LIQUIDACIÓN DE SALDO: Si debe dinero al momento de entregar, lo liquidamos con el método elegido
                        string metLiqFinal = cbMetodoLiquidacion.Text;

                        if (_saldoPendienteNum > 0)
                        {
                            string qVenta = @"INSERT INTO Ventas (Fecha, ProductoNombre, Cantidad, Total, MetodoPago, MontoRecibido, MontoCambio, DescuentoAplicado)
                                              VALUES (GETDATE(), @concepto, 1, @tot, @met, @tot, 0, 0)";
                            using (SqlCommand cmdV = new SqlCommand(qVenta, con, tra))
                            {
                                cmdV.Parameters.AddWithValue("@concepto", $"Liquidación contra Entrega Pedido #{_pedidoId}: {lblCliente.Text}");
                                cmdV.Parameters.AddWithValue("@tot", _saldoPendienteNum);
                                cmdV.Parameters.AddWithValue("@met", metLiqFinal);
                                cmdV.ExecuteNonQuery();
                            }

                            string qAbono = @"INSERT INTO HistorialAbonosPedidos (PedidoId, FechaAbono, Monto, MetodoPago, UsuarioRegistro)
                                              VALUES (@p, GETDATE(), @m, @met, @usr)";
                            using (SqlCommand cmdA = new SqlCommand(qAbono, con, tra))
                            {
                                cmdA.Parameters.AddWithValue("@p", _pedidoId);
                                cmdA.Parameters.AddWithValue("@m", _saldoPendienteNum);
                                cmdA.Parameters.AddWithValue("@met", metLiqFinal);
                                cmdA.Parameters.AddWithValue("@usr", Session.UsuarioActual ?? "Empleado");
                                cmdA.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            metLiqFinal = "Liquidado Previamente";
                        }

                        // 3. MEJORA #5 (INVENTARIO DIFERIDO): Descontamos la flor base que se usó (solo si no se había descontado ya)
                        if (!invDesc && prodId.HasValue && cantFlores > 0)
                        {
                            string qInv = "UPDATE Productos SET StockActual = StockActual - @c WHERE Id = @pid";
                            using (SqlCommand cmdI = new SqlCommand(qInv, con, tra))
                            {
                                cmdI.Parameters.AddWithValue("@c", cantFlores);
                                cmdI.Parameters.AddWithValue("@pid", prodId.Value);
                                cmdI.ExecuteNonQuery();
                            }
                        }

                        // 4. MEJORA #1 y #2 (FLETE AUTOMÁTICO): Si cobramos envío, generamos el egreso automático para pagar al repartidor hoy
                        if (!fleteCobrado && costoEnvio > 0)
                        {
                            string qGasto = @"INSERT INTO Gastos (Descripcion, Monto, Fecha, Categoria, MetodoPago, RegistradoPor) 
                                              VALUES (@desc, @monto, GETDATE(), 'Servicios', 'Efectivo', @user)";
                            using (SqlCommand cmdG = new SqlCommand(qGasto, con, tra))
                            {
                                cmdG.Parameters.AddWithValue("@desc", $"Flete / Servicio a Domicilio: Pedido #{_pedidoId} - {lblCliente.Text}");
                                cmdG.Parameters.AddWithValue("@monto", costoEnvio);
                                cmdG.Parameters.AddWithValue("@user", Session.UsuarioActual ?? "Empleado");
                                cmdG.ExecuteNonQuery();
                            }
                        }

                        // 5. MEJORA #4 (HORA EXACTA DE ENTREGA): Finalizamos el pedido en la base de datos
                        string qUpdate = @"UPDATE Pedidos 
                                           SET Estado = 'Entregado', 
                                               SaldoPendiente = 0, 
                                               MetodoPagoLiquidacion = @metLiq,
                                               FechaEntregaReal = GETDATE(),
                                               InventarioDescontado = 1,
                                               FleteCobradoEnCaja = 1
                                           WHERE Id = @id";
                        using (SqlCommand cmdU = new SqlCommand(qUpdate, con, tra))
                        {
                            cmdU.Parameters.AddWithValue("@metLiq", metLiqFinal);
                            cmdU.Parameters.AddWithValue("@id", _pedidoId);
                            cmdU.ExecuteNonQuery();
                        }

                        tra.Commit();
                        MessageBox.Show("¡Pedido entregado con éxito!\n\n• Se registró la fecha y hora exacta.\n• Se descontó el inventario.\n• El flete se registró como salida de caja automáticamente.", "Operación Completada", MessageBoxButton.OK, MessageBoxImage.Information);

                        this.DialogResult = true;
                    }
                    catch (Exception ex)
                    {
                        tra.Rollback();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar la entrega: " + ex.Message, "Error de Red", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}