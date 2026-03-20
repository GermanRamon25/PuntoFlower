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
            lblProducto.Text = "Surtir: " + _nombreProd;
        }

        private void btnSurtir_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtCantidad.Text, out int cant) || !decimal.TryParse(txtCosto.Text, out decimal costo))
            {
                MessageBox.Show("Por favor, ingresa valores válidos.");
                return;
            }

            ConexionDB db = new ConexionDB();
            using (SqlConnection con = db.OpenConnection())
            {
                SqlTransaction tra = con.BeginTransaction();
                try
                {
                    // 1. Aumentar StockActual en la tabla Productos
                    SqlCommand cmdStock = new SqlCommand("UPDATE Productos SET StockActual = StockActual + @c WHERE Nombre = @n", con, tra);
                    cmdStock.Parameters.AddWithValue("@c", cant);
                    cmdStock.Parameters.AddWithValue("@n", _nombreProd);
                    cmdStock.ExecuteNonQuery();

                    // 2. Registrar el Gasto en la tabla Compras
                    SqlCommand cmdGasto = new SqlCommand("INSERT INTO Compras (ProductoNombre, Cantidad, CostoUnitario) VALUES (@n, @c, @costo)", con, tra);
                    cmdGasto.Parameters.AddWithValue("@n", _nombreProd);
                    cmdGasto.Parameters.AddWithValue("@c", cant);
                    cmdGasto.Parameters.AddWithValue("@costo", costo);
                    cmdGasto.ExecuteNonQuery();

                    tra.Commit();
                    MessageBox.Show("Stock actualizado y gasto registrado.");
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