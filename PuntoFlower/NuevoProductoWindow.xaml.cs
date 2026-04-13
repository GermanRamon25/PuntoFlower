using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class NuevoProductoWindow : Window
    {
        public NuevoProductoWindow()
        {
            InitializeComponent();
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validación: Solo pedimos nombre y precio de venta
                if (string.IsNullOrEmpty(txtNombre.Text) || string.IsNullOrEmpty(txtPrecioVenta.Text))
                {
                    MessageBox.Show("Por favor, asigne un nombre y un precio de venta inicial.");
                    return;
                }

                ConexionDB db = new ConexionDB();
                using (SqlConnection conexion = db.OpenConnection())
                {
                    // Mantenemos la estructura de la tabla pero enviamos @pc como 0
                    string query = "INSERT INTO Productos (Nombre, Categoria, TipoVenta, PrecioCompra, PrecioVenta, StockActual, StockMinimo, FechaIngreso) " +
                                   "VALUES (@nom, @cat, @tipo, @pc, @pv, @sa, @sm, @fecha)";

                    SqlCommand cmd = new SqlCommand(query, conexion);
                    cmd.Parameters.AddWithValue("@nom", txtNombre.Text);
                    cmd.Parameters.AddWithValue("@cat", ((ComboBoxItem)cbCategoria.SelectedItem).Content.ToString());
                    cmd.Parameters.AddWithValue("@tipo", ((ComboBoxItem)cbTipo.SelectedItem).Content.ToString());

                    // LOGICA NUEVA: El catálogo nace con costo 0. 
                    // El costo real se asignará al comprar con el proveedor.
                    cmd.Parameters.AddWithValue("@pc", 0.00m);

                    cmd.Parameters.AddWithValue("@pv", decimal.Parse(txtPrecioVenta.Text));
                    cmd.Parameters.AddWithValue("@sa", int.Parse(txtStockActual.Text));
                    cmd.Parameters.AddWithValue("@sm", int.Parse(txtStockMinimo.Text));
                    cmd.Parameters.AddWithValue("@fecha", DateTime.Now);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show($"'{txtNombre.Text}' registrado exitosamente. Recuerde registrar su primera compra en el módulo de Proveedores para actualizar el costo.");
                    this.DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar en catálogo: " + ex.Message);
            }
        }
    }
}