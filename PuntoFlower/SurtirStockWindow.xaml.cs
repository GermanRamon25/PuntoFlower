using System;
using System.Data.SqlClient;
using System.Windows;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class SurtirStockWindow : Window
    {
        private string _nombreProd;

        public SurtirStockWindow(string nombreProducto)
        {
            InitializeComponent();
            _nombreProd = nombreProducto;
            lblProducto.Text = _nombreProd;
        }

        private void btnSurtir_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtCantidad.Text, out int cantidadATraspasar) || cantidadATraspasar <= 0)
            {
                MessageBox.Show("Por favor, ingresa una cantidad de piezas entera y mayor a cero.", "Cantidad Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConexionDB db = new ConexionDB();
            using (SqlConnection con = db.OpenConnection())
            {
                int stockDisponibleBodega = 0;
                // BLINDAJE: Verificar el stock de la sucursal actual
                string sqlVerificar = "SELECT ISNULL(StockActual, 0) FROM Productos WHERE Nombre = @n AND Categoria = 'Bodega' AND Sucursal = @s";

                using (SqlCommand cmdCheck = new SqlCommand(sqlVerificar, con))
                {
                    cmdCheck.Parameters.AddWithValue("@n", _nombreProd);
                    cmdCheck.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                    object resultado = cmdCheck.ExecuteScalar();
                    if (resultado != null)
                    {
                        stockDisponibleBodega = Convert.ToInt32(resultado);
                    }
                }

                if (stockDisponibleBodega < cantidadATraspasar)
                {
                    MessageBox.Show($"Traspaso denegado. No hay suficientes existencias en la Bodega de {ConexionGlobal.SucursalActual}.\n\n" +
                                    $"Cantidad en Bodega local: {stockDisponibleBodega} pz.\n" +
                                    $"Cantidad solicitada: {cantidadATraspasar} pz.",
                                    "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SqlTransaction tra = con.BeginTransaction();
                try
                {
                    // BLINDAJE
                    string sqlRestarBodega = "UPDATE Productos SET StockActual = StockActual - @c WHERE Nombre = @n AND Categoria = 'Bodega' AND Sucursal = @s";
                    using (SqlCommand cmdResta = new SqlCommand(sqlRestarBodega, con, tra))
                    {
                        cmdResta.Parameters.AddWithValue("@c", cantidadATraspasar);
                        cmdResta.Parameters.AddWithValue("@n", _nombreProd);
                        cmdResta.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                        cmdResta.ExecuteNonQuery();
                    }

                    // BLINDAJE: Traspaso a Venta solo para la sucursal correspondiente
                    string sqlSumaVenta = @"
                        IF EXISTS (SELECT 1 FROM Productos WHERE Nombre = @n AND Categoria = 'Venta' AND Sucursal = @s)
                            UPDATE Productos SET StockActual = StockActual + @c WHERE Nombre = @n AND Categoria = 'Venta' AND Sucursal = @s;
                        ELSE
                            INSERT INTO Productos (Nombre, Categoria, TipoVenta, PrecioCompra, PrecioVenta, StockActual, StockMinimo, FechaIngreso, RutaImagen, Sucursal)
                            SELECT Nombre, 'Venta', TipoVenta, PrecioCompra, PrecioVenta, @c, StockMinimo, GETDATE(), RutaImagen, @s 
                            FROM Productos WHERE Nombre = @n AND Categoria = 'Bodega' AND Sucursal = @s;";

                    using (SqlCommand cmdSuma = new SqlCommand(sqlSumaVenta, con, tra))
                    {
                        cmdSuma.Parameters.AddWithValue("@c", cantidadATraspasar);
                        cmdSuma.Parameters.AddWithValue("@n", _nombreProd);
                        cmdSuma.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                        cmdSuma.ExecuteNonQuery();
                    }

                    tra.Commit();

                    MessageBox.Show($"El traspaso interno de '{_nombreProd}' se completó con éxito en {ConexionGlobal.SucursalActual}.\n\n" +
                                    $"Se retiraron {cantidadATraspasar} pz de la cámara fría y ya se encuentran disponibles en mostrador para la venta.",
                                    "Traspaso Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);

                    this.DialogResult = true;
                }
                catch (Exception ex)
                {
                    tra.Rollback();
                    MessageBox.Show("Ocurrió un error interno al procesar el traspaso en la base de datos: " + ex.Message, "Error de Servidor", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}