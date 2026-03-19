using System;
using System.Data.SqlClient;
using System.Windows;
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
                ConexionDB db = new ConexionDB();
                using (SqlConnection conexion = db.OpenConnection())
                {
                    string query = "INSERT INTO Productos (Nombre, Categoria, TipoVenta, PrecioCompra, PrecioVenta, StockActual, StockMinimo, FechaIngreso) " +
                                   "VALUES (@nom, @cat, @tipo, @pc, @pv, @sa, @sm, @fecha)";

                    SqlCommand cmd = new SqlCommand(query, conexion);
                    cmd.Parameters.AddWithValue("@nom", txtNombre.Text);
                    cmd.Parameters.AddWithValue("@cat", cbCategoria.Text);
                    cmd.Parameters.AddWithValue("@tipo", cbTipo.Text);
                    cmd.Parameters.AddWithValue("@pc", decimal.Parse(txtPrecioCompra.Text));
                    cmd.Parameters.AddWithValue("@pv", decimal.Parse(txtPrecioVenta.Text));
                    cmd.Parameters.AddWithValue("@sa", int.Parse(txtStockActual.Text));
                    cmd.Parameters.AddWithValue("@sm", int.Parse(txtStockMinimo.Text));
                    cmd.Parameters.AddWithValue("@fecha", DateTime.Now);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Producto registrado correctamente.");
                    this.DialogResult = true; // Cierra la ventana y avisa que hubo cambios
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }
    }
}