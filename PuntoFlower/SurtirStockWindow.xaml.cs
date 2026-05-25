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
            // 1. Validación de formato de entrada numérico seguro
            if (!int.TryParse(txtCantidad.Text, out int cantidadATraspasar) || cantidadATraspasar <= 0)
            {
                MessageBox.Show("Por favor, ingresa una cantidad de piezas entera y mayor a cero.", "Cantidad Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConexionDB db = new ConexionDB();
            using (SqlConnection con = db.OpenConnection())
            {
                // 2. Comprobación previa: Verificamos si hay stock suficiente en Bodega antes de operar
                int stockDisponibleBodega = 0;
                string sqlVerificar = "SELECT ISNULL(StockActual, 0) FROM Productos WHERE Nombre = @n AND Categoria = 'Bodega'";

                using (SqlCommand cmdCheck = new SqlCommand(sqlVerificar, con))
                {
                    cmdCheck.Parameters.AddWithValue("@n", _nombreProd);
                    object resultado = cmdCheck.ExecuteScalar();
                    if (resultado != null)
                    {
                        stockDisponibleBodega = Convert.ToInt32(resultado);
                    }
                }

                // Si el almacén de reserva no tiene lo solicitado, detenemos la operación de inmediato
                if (stockDisponibleBodega < cantidadATraspasar)
                {
                    MessageBox.Show($"Traspaso denegado. No hay suficientes existencias en Bodega.\n\n" +
                                    $"Cantidad en Bodega: {stockDisponibleBodega} pz.\n" +
                                    $"Cantidad solicitada: {cantidadATraspasar} pz.",
                                    "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 3. Inicio del proceso transaccional transaccional coordinado
                SqlTransaction tra = con.BeginTransaction();
                try
                {
                    // PASO A: Restar las piezas del renglón de BODEGA
                    string sqlRestarBodega = "UPDATE Productos SET StockActual = StockActual - @c WHERE Nombre = @n AND Categoria = 'Bodega'";
                    using (SqlCommand cmdResta = new SqlCommand(sqlRestarBodega, con, tra))
                    {
                        cmdResta.Parameters.AddWithValue("@c", cantidadATraspasar);
                        cmdResta.Parameters.AddWithValue("@n", _nombreProd);
                        cmdResta.ExecuteNonQuery();
                    }

                    // PASO B: Sumar las piezas al renglón de VENTA (Mostrador)
                    // Usamos IF EXISTS por si la flor se dio de alta originalmente solo en Bodega, se autocree su contraparte de Venta
                    string sqlSumaVenta = @"
                        IF EXISTS (SELECT 1 FROM Productos WHERE Nombre = @n AND Categoria = 'Venta')
                            UPDATE Productos SET StockActual = StockActual + @c WHERE Nombre = @n AND Categoria = 'Venta';
                        ELSE
                            INSERT INTO Productos (Nombre, Categoria, TipoVenta, PrecioCompra, PrecioVenta, StockActual, StockMinimo, FechaIngreso, RutaImagen)
                            SELECT Nombre, 'Venta', TipoVenta, PrecioCompra, PrecioVenta, @c, StockMinimo, GETDATE(), RutaImagen 
                            FROM Productos WHERE Nombre = @n AND Categoria = 'Bodega';";

                    using (SqlCommand cmdSuma = new SqlCommand(sqlSumaVenta, con, tra))
                    {
                        cmdSuma.Parameters.AddWithValue("@c", cantidadATraspasar);
                        cmdSuma.Parameters.AddWithValue("@n", _nombreProd);
                        cmdSuma.ExecuteNonQuery();
                    }

                    // Si ambos pasos se completaron de forma correcta, confirmamos los cambios en la BD
                    tra.Commit();

                    MessageBox.Show($"El traspaso interno de '{_nombreProd}' se completó con éxito.\n\n" +
                                    $"Se retiraron {cantidadATraspasar} pz de la cámara fría y ya se encuentran disponibles en mostrador para la venta.",
                                    "Traspaso Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);

                    this.DialogResult = true;
                }
                catch (Exception ex)
                {
                    // Deshacer cualquier movimiento en caso de error imprevisto
                    tra.Rollback();
                    MessageBox.Show("Ocurrió un error interno al procesar el traspaso en la base de datos: " + ex.Message, "Error de Servidor", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}