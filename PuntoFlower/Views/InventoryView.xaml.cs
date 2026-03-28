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
                        query += " WHERE Nombre LIKE @buscar OR Categoria LIKE @buscar";

                    SqlCommand comando = new SqlCommand(query, conexion);
                    if (!string.IsNullOrEmpty(filtro))
                        comando.Parameters.AddWithValue("@buscar", "%" + filtro + "%");

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
                dgInventario.ItemsSource = null;
                dgInventario.ItemsSource = listaProductos;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar inventario: " + ex.Message);
            }
        }

        // --- NUEVA LÓGICA: REGISTRAR MERMA ---
        private void btnMerma_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = dgInventario.SelectedItem as Producto;
            if (seleccionado == null)
            {
                MessageBox.Show("Selecciona un producto para registrar la pérdida.", "Atención");
                return;
            }

            // Pedimos la cantidad (Puedes crear una ventanita pequeña o usar este InputBox rápido)
            string cantidadStr = Microsoft.VisualBasic.Interaction.InputBox(
                $"¿Cuántas unidades de '{seleccionado.Nombre}' vas a retirar por merma?", "Registrar Pérdida", "1");

            if (string.IsNullOrEmpty(cantidadStr)) return;

            if (int.TryParse(cantidadStr, out int cantBaja) && cantBaja > 0)
            {
                if (cantBaja > seleccionado.StockActual)
                {
                    MessageBox.Show("No puedes retirar más de lo que hay en stock.", "Error");
                    return;
                }

                string motivo = Microsoft.VisualBasic.Interaction.InputBox(
                    "Motivo de la merma (Ej: Marchita, Daño, Caducidad):", "Motivo", "Marchita");

                ConexionDB db = new ConexionDB();
                try
                {
                    using (SqlConnection con = db.OpenConnection())
                    {
                        // 1. Descontar del inventario
                        string qUpdate = "UPDATE Productos SET StockActual = StockActual - @cant WHERE Id = @id";
                        SqlCommand cmdUp = new SqlCommand(qUpdate, con);
                        cmdUp.Parameters.AddWithValue("@cant", cantBaja);
                        cmdUp.Parameters.AddWithValue("@id", seleccionado.Id);
                        cmdUp.ExecuteNonQuery();

                        // 2. Registrar en la tabla de Mermas para reportes
                        string qInsert = "INSERT INTO Mermas (ProductoNombre, Cantidad, Motivo, Fecha) VALUES (@nom, @cant, @mot, GETDATE())";
                        SqlCommand cmdIn = new SqlCommand(qInsert, con);
                        cmdIn.Parameters.AddWithValue("@nom", seleccionado.Nombre);
                        cmdIn.Parameters.AddWithValue("@cant", cantBaja);
                        cmdIn.Parameters.AddWithValue("@mot", motivo);
                        cmdIn.ExecuteNonQuery();
                    }
                    MessageBox.Show("Merma registrada y stock actualizado.");
                    CargarDesdeSQL();
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void btnSurtirStock_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = dgInventario.SelectedItem as Producto;
            if (seleccionado != null)
            {
                PuntoFlower.Views.SurtirStockWindow ventanaSurtir = new PuntoFlower.Views.SurtirStockWindow(seleccionado.Nombre);
                ventanaSurtir.Owner = Window.GetWindow(this);
                if (ventanaSurtir.ShowDialog() == true) CargarDesdeSQL();
            }
            else
            {
                MessageBox.Show("Selecciona un producto para surtir stock.", "Atención");
            }
        }

        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = dgInventario.SelectedItem as Producto;
            if (seleccionado == null) return;

            var result = MessageBox.Show($"¿Deseas eliminar '{seleccionado.Nombre}' permanentemente?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
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
                    CargarDesdeSQL();
                }
                catch (Exception ex) { MessageBox.Show("Error al eliminar: " + ex.Message); }
            }
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e) => CargarDesdeSQL(txtSearch.Text);
        private void txtSearch_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) CargarDesdeSQL(txtSearch.Text); }
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) { if (string.IsNullOrEmpty(txtSearch.Text)) CargarDesdeSQL(); }
        private void btnNuevaFlor_Click(object sender, RoutedEventArgs e)
        {
            NuevoProductoWindow ventana = new NuevoProductoWindow();
            ventana.Owner = Window.GetWindow(this);
            if (ventana.ShowDialog() == true) CargarDesdeSQL();
        }
    }
}