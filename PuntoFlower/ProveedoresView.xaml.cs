using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class ProveedoresView : UserControl
    {
        public ProveedoresView()
        {
            InitializeComponent();
            CargarProveedores();
        }

        private void CargarProveedores()
        {
            List<object> lista = new List<object>();
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "SELECT * FROM Proveedores";
                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            lista.Add(new
                            {
                                Id = r["Id"],
                                Nombre = r["Nombre"].ToString(),
                                Telefono = r["Telefono"].ToString(),
                                Categoria = r["Categoria"].ToString(),
                                Direccion = r["Direccion"].ToString()
                            });
                        }
                    }
                }
                dgProveedores.ItemsSource = null;
                dgProveedores.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar: " + ex.Message);
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtNombre.Text))
            {
                MessageBox.Show("El nombre del proveedor es obligatorio.");
                return;
            }

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "INSERT INTO Proveedores (Nombre, Telefono, Direccion, Categoria) VALUES (@nom, @tel, @dir, @cat)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@nom", txtNombre.Text);
                    cmd.Parameters.AddWithValue("@tel", txtTelefono.Text);
                    cmd.Parameters.AddWithValue("@dir", txtDireccion.Text);
                    cmd.Parameters.AddWithValue("@cat", (cbCategoria.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Varios");

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Proveedor guardado con éxito.");

                    // Limpiar campos
                    txtNombre.Clear(); txtTelefono.Clear(); txtDireccion.Clear();
                    CargarProveedores();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        // CAMBIO APLICADO AQUÍ: Conexión con la ventana de registro de compras
        private void btnNuevaCompra_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = dgProveedores.SelectedItem;

            if (seleccionado == null)
            {
                MessageBox.Show("Por favor, selecciona un proveedor de la lista para registrar su surtido.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Usamos 'dynamic' para acceder a las propiedades del objeto anónimo de la lista
                dynamic prov = seleccionado;
                int id = prov.Id;
                string nombre = prov.Nombre;

                // Abrimos la ventana de registro de compras pasándole el ID y Nombre
                RegistrarCompraWindow ventana = new RegistrarCompraWindow(id, nombre);
                ventana.Owner = Window.GetWindow(this); // Se centra sobre el programa

                if (ventana.ShowDialog() == true)
                {
                    // Si el surtido fue exitoso, aquí podrías refrescar alguna lista si fuera necesario
                    // Por ahora, solo confirmamos que se cerró tras guardar.
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al abrir la ventana de compras: " + ex.Message);
            }
        }
    }
}