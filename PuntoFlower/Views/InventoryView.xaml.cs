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
                dgInventario.ItemsSource = listaProductos;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar inventario: " + ex.Message);
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

        // Refresco automático al borrar el texto del buscador
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