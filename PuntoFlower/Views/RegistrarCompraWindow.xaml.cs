using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;
using PuntoFlower.Models;

namespace PuntoFlower.Views
{
    public partial class RegistrarCompraWindow : Window
    {
        private int _provId;

        public RegistrarCompraWindow(int proveedorId, string nombreProveedor)
        {
            InitializeComponent();
            _provId = proveedorId;
            lblProveedor.Text = "Proveedor: " + nombreProveedor;
            CargarProductos();
        }

        private void CargarProductos()
        {
            List<Producto> lista = new List<Producto>();
            ConexionDB db = new ConexionDB();
            using (SqlConnection con = db.OpenConnection())
            {
                SqlCommand cmd = new SqlCommand("SELECT DISTINCT Nombre FROM Productos ORDER BY Nombre", con);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) lista.Add(new Producto { Nombre = r["Nombre"].ToString() });
                }
            }
            cbProductos.ItemsSource = lista;
        }

        private void ActualizarResumenTotal(object sender, TextChangedEventArgs e) { CalcularVistaPrevia(); }
        private void ActualizarResumenTotal(object sender, SelectionChangedEventArgs e) { CalcularVistaPrevia(); }

        private void CalcularVistaPrevia()
        {
            if (lblTotal == null || lblDetalleCalculo == null || cbFormatoSurtido == null) return;

            if (decimal.TryParse(txtCosto.Text, out decimal costoTotal) && int.TryParse(txtCantidad.Text, out int cantidad))
            {
                lblTotal.Text = $"Total de la Compra: {costoTotal:C2}";

                // EVALUACIÓN MATEMÁTICA EN VISTA PREVIA
                int piezasTotales = 0;
                switch (cbFormatoSurtido.SelectedIndex)
                {
                    case 0: // Por Docenas
                        piezasTotales = cantidad * 12;
                        break;
                    case 1: // Por Piezas
                        piezasTotales = cantidad;
                        break;
                    case 2: // Por Paquetes (24 pz)
                        piezasTotales = cantidad * 24;
                        break;
                    default:
                        piezasTotales = cantidad;
                        break;
                }

                decimal unitario = piezasTotales > 0 ? (costoTotal / piezasTotales) : 0;

                lblDetalleCalculo.Text = piezasTotales > 0
                    ? $"Cálculo: {piezasTotales} piezas totales ({unitario:C2} c/u)"
                    : "Costo calculado: $0.00 por pieza";
            }
        }

        private void btnGuardarSurtido_Click(object sender, RoutedEventArgs e)
        {
            var prod = cbProductos.SelectedItem as Producto;

            if (prod == null || string.IsNullOrWhiteSpace(txtCantidad.Text) || string.IsNullOrWhiteSpace(txtCosto.Text))
            {
                MessageBox.Show("Por favor, complete todos los campos.", "Campos Vacíos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtCantidad.Text, out int cantidadIngresada) || cantidadIngresada <= 0)
            {
                MessageBox.Show("Por favor, ingresa una cantidad de entrada numérica válida.", "Formato Incorrecto", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtCosto.Text, out decimal costoTotal) || costoTotal < 0)
            {
                MessageBox.Show("Por favor, ingresa un monto de costo total válido.", "Formato Incorrecto", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // EVALUACIÓN MATEMÁTICA EN INSERCIÓN BD
            int piezasTotalesEntran = 0;
            switch (cbFormatoSurtido.SelectedIndex)
            {
                case 0: // Por Docenas
                    piezasTotalesEntran = cantidadIngresada * 12;
                    break;
                case 1: // Por Piezas
                    piezasTotalesEntran = cantidadIngresada;
                    break;
                case 2: // Por Paquetes (24 pz)
                    piezasTotalesEntran = cantidadIngresada * 24;
                    break;
                default:
                    piezasTotalesEntran = cantidadIngresada;
                    break;
            }

            decimal costoUnitarioFinal = piezasTotalesEntran > 0 ? (costoTotal / piezasTotalesEntran) : 0;

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    SqlTransaction tra = con.BeginTransaction();
                    try
                    {
                        // 1. ACTUALIZACIÓN AUTOMÁTICA EN INVENTARIO (EN BODEGA ÚNICAMENTE)
                        string queryStock = @"
                            IF EXISTS (SELECT 1 FROM Productos WHERE Nombre = @nom AND Categoria = 'Bodega')
                            BEGIN
                                UPDATE Productos 
                                SET StockActual = StockActual + @cant, 
                                    PrecioCompra = @costoUnit 
                                WHERE Nombre = @nom AND Categoria = 'Bodega';
                            END
                            ELSE
                            BEGIN
                                INSERT INTO Productos (Nombre, Categoria, TipoVenta, PrecioCompra, PrecioVenta, StockActual, StockMinimo, FechaIngreso, RutaImagen)
                                VALUES (@nom, 'Bodega', 'Insumo', @costoUnit, 0.00, @cant, 10, GETDATE(), '');
                            END";

                        using (SqlCommand cmdStock = new SqlCommand(queryStock, con, tra))
                        {
                            cmdStock.Parameters.AddWithValue("@cant", piezasTotalesEntran);
                            cmdStock.Parameters.AddWithValue("@costoUnit", costoUnitarioFinal);
                            cmdStock.Parameters.AddWithValue("@nom", prod.Nombre);
                            cmdStock.ExecuteNonQuery();
                        }

                        // 2. REGISTRO EN HISTORIAL DE DETALLE COMPRAS
                        string queryHist = "INSERT INTO DetalleCompras (ProveedorId, ProductoNombre, Cantidad, PrecioCosto) VALUES (@pId, @nom, @cant, @costoUnit)";
                        using (SqlCommand cmdHist = new SqlCommand(queryHist, con, tra))
                        {
                            cmdHist.Parameters.AddWithValue("@pId", _provId);
                            cmdHist.Parameters.AddWithValue("@nom", prod.Nombre);
                            cmdHist.Parameters.AddWithValue("@cant", piezasTotalesEntran);
                            cmdHist.Parameters.AddWithValue("@costoUnit", costoUnitarioFinal);
                            cmdHist.ExecuteNonQuery();
                        }

                        tra.Commit();
                        MessageBox.Show($"¡Surtido exitoso!\n\nLas {piezasTotalesEntran} unidades entraron directamente a la BODEGA.\nNuevo costo unitario: {costoUnitarioFinal:C2}", "Ingreso de Mercancía", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.DialogResult = true;
                    }
                    catch (Exception ex)
                    {
                        tra.Rollback();
                        MessageBox.Show("Error en la base de datos al guardar la transacción: " + ex.Message, "Error Interno", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar con el servidor: " + ex.Message, "Fallo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}