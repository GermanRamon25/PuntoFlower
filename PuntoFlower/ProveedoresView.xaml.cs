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
        // Variable para controlar si estamos creando o editando
        private int _proveedorIdEnEdicion = 0;

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
                    // BLINDAJE: Cargar proveedores solo de la sucursal actual
                    string query = "SELECT * FROM Proveedores WHERE Sucursal = @s";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            lista.Add(new
                            {
                                Id = Convert.ToInt32(r["Id"]),
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
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre del proveedor es obligatorio.", "Datos Faltantes", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;

                    if (_proveedorIdEnEdicion == 0) // MODO INSERCIÓN
                    {
                        // BLINDAJE: Insertar proveedor en la sucursal actual
                        cmd.CommandText = "INSERT INTO Proveedores (Nombre, Telefono, Direccion, Categoria, Sucursal) VALUES (@nom, @tel, @dir, @cat, @s)";
                    }
                    else // MODO EDICIÓN
                    {
                        // BLINDAJE: Actualizar proveedor asegurando que pertenece a la sucursal
                        cmd.CommandText = "UPDATE Proveedores SET Nombre = @nom, Telefono = @tel, Direccion = @dir, Categoria = @cat WHERE Id = @id AND Sucursal = @s";
                        cmd.Parameters.AddWithValue("@id", _proveedorIdEnEdicion);
                    }

                    cmd.Parameters.AddWithValue("@nom", txtNombre.Text.Trim());
                    cmd.Parameters.AddWithValue("@tel", txtTelefono.Text.Trim());
                    cmd.Parameters.AddWithValue("@dir", txtDireccion.Text.Trim());
                    cmd.Parameters.AddWithValue("@cat", (cbCategoria.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Flores Frescas");
                    cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);

                    cmd.ExecuteNonQuery();

                    if (_proveedorIdEnEdicion == 0)
                        MessageBox.Show("Proveedor guardado con éxito.", "Guardado Correcto", MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        MessageBox.Show("Proveedor actualizado con éxito.", "Actualización Correcta", MessageBoxButton.OK, MessageBoxImage.Information);

                    RestablecerFormulario();
                    CargarProveedores();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // LÓGICA BOTÓN EDITAR
        private void btnEditarProveedor_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            if (boton == null) return;
            dynamic prov = boton.DataContext;

            if (prov != null)
            {
                _proveedorIdEnEdicion = prov.Id;
                txtNombre.Text = prov.Nombre;
                txtTelefono.Text = prov.Telefono;
                txtDireccion.Text = prov.Direccion;

                // Buscar y seleccionar la categoría en el ComboBox
                foreach (ComboBoxItem item in cbCategoria.Items)
                {
                    if (item.Content.ToString() == prov.Categoria)
                    {
                        cbCategoria.SelectedItem = item;
                        break;
                    }
                }

                // Cambiar el aspecto del botón Guardar para indicar que es Edición
                btnGuardar.Content = "ACTUALIZAR PROVEEDOR";
                btnGuardar.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#2980B9"); // Azul
                btnCancelarEdicion.Visibility = Visibility.Visible;
            }
        }

        // LÓGICA BOTÓN ELIMINAR
        private void btnEliminarProveedor_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            if (boton == null) return;
            dynamic prov = boton.DataContext;

            if (prov != null)
            {
                var confirmacion = MessageBox.Show($"¿Deseas eliminar permanentemente al proveedor '{prov.Nombre}' de {ConexionGlobal.SucursalActual}?\n\nNota: Esto no borrará su historial de compras pasadas.", "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirmacion == MessageBoxResult.Yes)
                {
                    ConexionDB db = new ConexionDB();
                    try
                    {
                        using (SqlConnection con = db.OpenConnection())
                        {
                            // BLINDAJE: Borrar el proveedor solo si pertenece a la sucursal actual
                            string query = "DELETE FROM Proveedores WHERE Id = @id AND Sucursal = @s";
                            SqlCommand cmd = new SqlCommand(query, con);
                            cmd.Parameters.AddWithValue("@id", prov.Id);
                            cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);

                            int filasAfectadas = cmd.ExecuteNonQuery();

                            if (filasAfectadas > 0)
                            {
                                MessageBox.Show("Proveedor eliminado con éxito.", "Eliminación Correcta", MessageBoxButton.OK, MessageBoxImage.Information);
                                if (_proveedorIdEnEdicion == prov.Id) RestablecerFormulario(); // Si estaba editando el que borró, limpia el form
                                CargarProveedores();
                            }
                            else
                            {
                                MessageBox.Show("No se pudo eliminar el proveedor. Verifica si existe en esta sucursal.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            }
                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        // Control de error por si la base de datos restringe el borrado por llaves foráneas (FK)
                        if (sqlEx.Number == 547)
                        {
                            MessageBox.Show("No se puede eliminar este proveedor porque está vinculado a registros de compras existentes.", "Restricción de Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show("Error de base de datos al eliminar: " + sqlEx.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al eliminar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void btnCancelarEdicion_Click(object sender, RoutedEventArgs e)
        {
            RestablecerFormulario();
        }

        private void RestablecerFormulario()
        {
            _proveedorIdEnEdicion = 0;
            txtNombre.Clear();
            txtTelefono.Clear();
            txtDireccion.Clear();
            cbCategoria.SelectedIndex = 0;

            btnGuardar.Content = "GUARDAR PROVEEDOR";
            btnGuardar.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#27AE60"); // Verde
            btnCancelarEdicion.Visibility = Visibility.Collapsed;
        }

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
                dynamic prov = seleccionado;
                int id = prov.Id;
                string nombre = prov.Nombre;

                RegistrarCompraWindow ventana = new RegistrarCompraWindow(id, nombre);
                ventana.Owner = Window.GetWindow(this);

                if (ventana.ShowDialog() == true)
                {
                    // Lógica tras cerrar ventana
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al abrir la ventana de compras: " + ex.Message, "Error");
            }
        }
    }
}