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
            lblFecha.Text = "Entrega programada: " + pedido.FechaEntrega.ToString("dd/MM/yyyy hh:mm tt");
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
                        string qPed = "UPDATE Pedidos SET Anticipo = Anticipo + @m, SaldoPendiente = SaldoPendiente - @m WHERE Id = @id";
                        using (SqlCommand cP = new SqlCommand(qPed, con, tra))
                        {
                            cP.Parameters.AddWithValue("@m", abono);
                            cP.Parameters.AddWithValue("@id", _pedidoId);
                            cP.ExecuteNonQuery();
                        }

                        string qVen = @"INSERT INTO Ventas (Fecha, ProductoNombre, Cantidad, Total, MetodoPago, MontoRecibido, MontoCambio, DescuentoAplicado)
                                        VALUES (GETDATE(), @con, 1, @tot, @met, @tot, 0, 0)";
                        using (SqlCommand cV = new SqlCommand(qVen, con, tra))
                        {
                            cV.Parameters.AddWithValue("@con", $"Abono Pedido #{_pedidoId}: {lblCliente.Text}");
                            cV.Parameters.AddWithValue("@tot", abono);
                            cV.Parameters.AddWithValue("@met", metodo.Trim());
                            cV.ExecuteNonQuery();
                        }

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

                        // CORRECCIÓN 2: El Flete ahora entra como un valor negativo en Ventas para aparecer en el Corte de Caja.
                        if (!fleteCobrado && costoEnvio > 0)
                        {
                            // A) Se guarda en Gastos para el historial general
                            string qGasto = @"INSERT INTO Gastos (Descripcion, Monto, Fecha, Categoria, MetodoPago, RegistradoPor) 
                                              VALUES (@desc, @monto, GETDATE(), 'Servicios', 'Efectivo', @user)";
                            using (SqlCommand cmdG = new SqlCommand(qGasto, con, tra))
                            {
                                cmdG.Parameters.AddWithValue("@desc", $"Flete a Domicilio: Pedido #{_pedidoId}");
                                cmdG.Parameters.AddWithValue("@monto", costoEnvio);
                                cmdG.Parameters.AddWithValue("@user", Session.UsuarioActual ?? "Empleado");
                                cmdG.ExecuteNonQuery();
                            }

                            // B) Se guarda en Ventas como número negativo para el Corte de Caja
                            string qVentaFlete = @"INSERT INTO Ventas (Fecha, ProductoNombre, Cantidad, Total, MetodoPago, MontoRecibido, MontoCambio, DescuentoAplicado)
                                                   VALUES (GETDATE(), @descV, 1, @montoNeg, 'Efectivo', @montoNeg, 0, 0)";
                            using (SqlCommand cmdVF = new SqlCommand(qVentaFlete, con, tra))
                            {
                                cmdVF.Parameters.AddWithValue("@descV", $"Salida de Caja (Gasto): Flete Pedido #{_pedidoId} - {lblCliente.Text}");
                                cmdVF.Parameters.AddWithValue("@montoNeg", -costoEnvio);
                                cmdVF.ExecuteNonQuery();
                            }
                        }

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