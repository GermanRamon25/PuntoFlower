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
                // 1. Validaciones iniciales de presencia de datos
                if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtPrecioVenta.Text))
                {
                    MessageBox.Show("Por favor, asigne un nombre y un precio de venta inicial.", "Datos Faltantes", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. Blindaje numérico: Validamos que los formatos de entrada sean correctos
                if (!decimal.TryParse(txtPrecioVenta.Text, out decimal precioVenta))
                {
                    MessageBox.Show("El precio de venta sugerido debe ser un número válido.", "Error de Formato", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtStockActual.Text, out int stockActual) || !int.TryParse(txtStockMinimo.Text, out int stockMinimo))
                {
                    MessageBox.Show("Las cantidades de stock deben ser números enteros válidos.", "Error de Formato", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (stockActual < 0 || stockMinimo < 0)
                {
                    MessageBox.Show("Las existencias de stock no pueden ser cantidades negativas.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 3. Conexión e inserción segura en la base de datos local
                ConexionDB db = new ConexionDB();
                using (SqlConnection conexion = db.OpenConnection())
                {
                    string query = "INSERT INTO Productos (Nombre, Categoria, TipoVenta, PrecioCompra, PrecioVenta, StockActual, StockMinimo, FechaIngreso, RutaImagen) " +
                                   "VALUES (@nom, @cat, @tipo, @pc, @pv, @sa, @sm, @fecha, @ruta)";

                    using (SqlCommand cmd = new SqlCommand(query, conexion))
                    {
                        cmd.Parameters.AddWithValue("@nom", txtNombre.Text.Trim());
                        cmd.Parameters.AddWithValue("@cat", ((ComboBoxItem)cbCategoria.SelectedItem).Content.ToString());
                        cmd.Parameters.AddWithValue("@tipo", ((ComboBoxItem)cbTipo.SelectedItem).Content.ToString());
                        cmd.Parameters.AddWithValue("@pc", 0.00m); // Costo inicial base en cero, se alimenta al surtir o comprar
                        cmd.Parameters.AddWithValue("@pv", precioVenta);
                        cmd.Parameters.AddWithValue("@sa", stockActual);
                        cmd.Parameters.AddWithValue("@sm", stockMinimo);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ruta", ""); // Inicialización de fotografía vacía

                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show($"'{txtNombre.Text.Trim()}' registrado correctamente en el catálogo.\n\nPuedes agregar su fotografía desde el 'Catálogo de Fotos'.", "Alta Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar en catálogo: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}