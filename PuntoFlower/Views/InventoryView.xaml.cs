using PuntoFlower.Data;
using PuntoFlower.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PuntoFlower.Views
{
    public partial class InventoryView : UserControl
    {
        public InventoryView()
        {
            InitializeComponent();
            CargarDesdeSQL();

            // Refrescar automáticamente al entrar a la vista (Útil tras vender)
            this.IsVisibleChanged += (s, e) => {
                if ((bool)e.NewValue) CargarDesdeSQL();
            };
        }

        private void CargarDesdeSQL(string filtro = "")
        {
            List<Producto> listaProductos = new List<Producto>();
            ConexionDB db = new ConexionDB();

            try
            {
                using (SqlConnection conexion = db.OpenConnection())
                {
                    string query = "SELECT * FROM Productos";

                    if (!string.IsNullOrEmpty(filtro))
                    {
                        query += " WHERE Nombre LIKE @buscar OR Categoria LIKE @buscar";
                    }

                    SqlCommand comando = new SqlCommand(query, conexion);

                    if (!string.IsNullOrEmpty(filtro))
                    {
                        comando.Parameters.AddWithValue("@buscar", "%" + filtro + "%");
                    }

                    using (SqlDataReader reader = comando.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listaProductos.Add(new Producto
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nombre = reader["Nombre"].ToString(),
                                Categoria = reader["Categoria"].ToString(),
                                TipoVenta = reader["TipoVenta"].ToString(),
                                StockActual = Convert.ToInt32(reader["StockActual"]),
                                StockMinimo = Convert.ToInt32(reader["StockMinimo"]),
                                PrecioCompra = Convert.ToDecimal(reader["PrecioCompra"]),
                                PrecioVenta = Convert.ToDecimal(reader["PrecioVenta"])
                            });
                        }
                    }
                }
                // Limpiamos y asignamos para asegurar el refresco visual en el DataGrid
                dgInventario.ItemsSource = null;
                dgInventario.ItemsSource = listaProductos;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar inventario: " + ex.Message);
            }
        }

        // Lógica para eliminar el producto seleccionado (Botón reubicado arriba)
        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = dgInventario.SelectedItem as Producto;

            if (seleccionado == null)
            {
                MessageBox.Show("Por favor, selecciona un producto de la tabla para eliminarlo.", "Atención", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"¿Deseas eliminar '{seleccionado.Nombre}' permanentemente?",
                                         "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                ConexionDB db = new ConexionDB();
                try
                {
                    using (SqlConnection conexion = db.OpenConnection())
                    {
                        string query = "DELETE FROM Productos WHERE Id = @id";
                        SqlCommand cmd = new SqlCommand(query, conexion);
                        cmd.Parameters.AddWithValue("@id", seleccionado.Id);
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Producto eliminado correctamente.");
                    CargarDesdeSQL();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al eliminar: " + ex.Message);
                }
            }
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            CargarDesdeSQL(txtSearch.Text);
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CargarDesdeSQL(txtSearch.Text);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtSearch.Text))
            {
                CargarDesdeSQL();
            }
        }

        private void btnNuevaFlor_Click(object sender, RoutedEventArgs e)
        {
            NuevoProductoWindow ventana = new NuevoProductoWindow();
            ventana.Owner = Window.GetWindow(this);

            if (ventana.ShowDialog() == true)
            {
                CargarDesdeSQL();
            }
        }
    }
}