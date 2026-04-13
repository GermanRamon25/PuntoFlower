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
                SqlCommand cmd = new SqlCommand("SELECT Nombre FROM Productos ORDER BY Nombre", con);
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
            if (lblTotal == null || lblDetalleCalculo == null) return;

            if (decimal.TryParse(txtCosto.Text, out decimal costoTotal) && int.TryParse(txtCantidad.Text, out int cantidad))
            {
                lblTotal.Text = $"Total de la Compra: {costoTotal:C2}";

                int piezasTotales = (cbFormatoSurtido.SelectedIndex == 0) ? (cantidad * 12) : cantidad;
                decimal unitario = piezasTotales > 0 ? (costoTotal / piezasTotales) : 0;

                lblDetalleCalculo.Text = piezasTotales > 0
                    ? $"Cálculo: {piezasTotales} piezas totales ({unitario:C2} c/u)"
                    : "Costo calculado: $0.00 por pieza";
            }
        }

        private void btnGuardarSurtido_Click(object sender, RoutedEventArgs e)
        {
            var prod = cbProductos.SelectedItem as Producto;

            if (prod == null || string.IsNullOrEmpty(txtCantidad.Text) || string.IsNullOrEmpty(txtCosto.Text))
            {
                MessageBox.Show("Por favor, complete todos los campos.");
                return;
            }

            try
            {
                int cantidadIngresada = int.Parse(txtCantidad.Text);
                decimal costoTotal = decimal.Parse(txtCosto.Text);

                // CONVERSIÓN: Si es docena multiplicamos cantidad, si no se queda igual
                int piezasTotalesEntran = (cbFormatoSurtido.SelectedIndex == 0) ? (cantidadIngresada * 12) : cantidadIngresada;

                // CÁLCULO: Precio Unitario para la tabla Productos
                decimal costoUnitarioFinal = costoTotal / piezasTotalesEntran;

                ConexionDB db = new ConexionDB();
                using (SqlConnection con = db.OpenConnection())
                {
                    SqlTransaction tra = con.BeginTransaction();
                    try
                    {
                        // 1. ACTUALIZACIÓN AUTOMÁTICA EN INVENTARIO
                        // Aumentamos stock y actualizamos el costo unitario oficial de la flor
                        string queryStock = "UPDATE Productos SET StockActual = StockActual + @cant, PrecioCompra = @costoUnit WHERE Nombre = @nom";
                        SqlCommand cmdStock = new SqlCommand(queryStock, con, tra);
                        cmdStock.Parameters.AddWithValue("@cant", piezasTotalesEntran);
                        cmdStock.Parameters.AddWithValue("@costoUnit", costoUnitarioFinal);
                        cmdStock.Parameters.AddWithValue("@nom", prod.Nombre);
                        cmdStock.ExecuteNonQuery();

                        // 2. REGISTRO EN HISTORIAL
                        string queryHist = "INSERT INTO DetalleCompras (ProveedorId, ProductoNombre, Cantidad, PrecioCosto) VALUES (@pId, @nom, @cant, @costoUnit)";
                        SqlCommand cmdHist = new SqlCommand(queryHist, con, tra);
                        cmdHist.Parameters.AddWithValue("@pId", _provId);
                        cmdHist.Parameters.AddWithValue("@nom", prod.Nombre);
                        cmdHist.Parameters.AddWithValue("@cant", piezasTotalesEntran);
                        cmdHist.Parameters.AddWithValue("@costoUnit", costoUnitarioFinal);
                        cmdHist.ExecuteNonQuery();

                        tra.Commit();
                        MessageBox.Show($"¡Surtido exitoso!\nEntraron: {piezasTotalesEntran} unidades.\nNuevo costo unitario: {costoUnitarioFinal:C2}");
                        this.DialogResult = true;
                    }
                    catch (Exception ex)
                    {
                        tra.Rollback();
                        MessageBox.Show("Error en la base de datos: " + ex.Message);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error al procesar: " + ex.Message); }
        }
    }
}