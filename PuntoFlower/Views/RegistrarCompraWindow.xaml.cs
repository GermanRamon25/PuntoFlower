using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
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
                SqlCommand cmd = new SqlCommand("SELECT Nombre FROM Productos", con);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) lista.Add(new Producto { Nombre = r["Nombre"].ToString() });
                }
            }
            cbProductos.ItemsSource = lista;
        }

        private void btnGuardarSurtido_Click(object sender, RoutedEventArgs e)
        {
            var prod = cbProductos.SelectedItem as Producto;
            if (prod == null || string.IsNullOrEmpty(txtCantidad.Text)) return;

            ConexionDB db = new ConexionDB();
            using (SqlConnection con = db.OpenConnection())
            {
                SqlTransaction tra = con.BeginTransaction();
                try
                {
                    // 1. Aumentar el Stock del producto y actualizar su precio de compra
                    string queryStock = "UPDATE Productos SET StockActual = StockActual + @cant, PrecioCompra = @costo WHERE Nombre = @nom";
                    SqlCommand cmdStock = new SqlCommand(queryStock, con, tra);
                    cmdStock.Parameters.AddWithValue("@cant", int.Parse(txtCantidad.Text));
                    cmdStock.Parameters.AddWithValue("@costo", decimal.Parse(txtCosto.Text));
                    cmdStock.Parameters.AddWithValue("@nom", prod.Nombre);
                    cmdStock.ExecuteNonQuery();

                    // 2. Guardar el detalle de la compra para historial
                    string queryHistorial = "INSERT INTO DetalleCompras (ProveedorId, ProductoNombre, Cantidad, PrecioCosto) VALUES (@pId, @nom, @cant, @costo)";
                    SqlCommand cmdHist = new SqlCommand(queryHistorial, con, tra);
                    cmdHist.Parameters.AddWithValue("@pId", _provId);
                    cmdHist.Parameters.AddWithValue("@nom", prod.Nombre);
                    cmdHist.Parameters.AddWithValue("@cant", int.Parse(txtCantidad.Text));
                    cmdHist.Parameters.AddWithValue("@costo", decimal.Parse(txtCosto.Text));
                    cmdHist.ExecuteNonQuery();

                    tra.Commit();
                    MessageBox.Show("¡Stock actualizado correctamente!");
                    this.DialogResult = true;
                }
                catch (Exception ex)
                {
                    tra.Rollback();
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }
    }
}