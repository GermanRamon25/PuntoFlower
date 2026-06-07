using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class ConfigurationView : UserControl
    {
        private ObservableCollection<FlorPrecio> listaFlores = new ObservableCollection<FlorPrecio>();
        private ObservableCollection<string> coleccionEncargados = new ObservableCollection<string>();
        private FlorPrecio florSeleccionada;

        public ConfigurationView()
        {
            InitializeComponent();
            CargarPreciosActuales();
            CargarTablaFlores();
            CargarUsuariosPendientes();

            // Asignamos el origen de datos al ListBox dinámico
            lbEncargadosCuentas.ItemsSource = coleccionEncargados;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ConexionDB db = new ConexionDB();
                txtNombreSucursalInput.Text = db.ObtenerNombreSucursal();

                // Leemos los encargados guardados en la BD
                string encargadosRaw = db.ObtenerEncargadoCuenta1();
                coleccionEncargados.Clear();

                if (!string.IsNullOrWhiteSpace(encargadosRaw))
                {
                    string[] lista = encargadosRaw.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var nombre in lista)
                    {
                        coleccionEncargados.Add(nombre.Trim());
                    }
                }

                ActualizarContadorVisual();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la configuración de la sucursal: " + ex.Message, "Fallo de Enlace");
            }
        }

        private void ActualizarContadorVisual()
        {
            if (txtContadorEncargados != null)
            {
                txtContadorEncargados.Text = $"({coleccionEncargados.Count} personas)";
            }
        }

        private void btnAñadirEncargado_Click(object sender, RoutedEventArgs e)
        {
            string nuevoNombre = txtNuevoEncargadoInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(nuevoNombre)) return;

            if (coleccionEncargados.Contains(nuevoNombre))
            {
                MessageBox.Show("Esta persona ya se encuentra registrada en la lista.", "Atención");
                return;
            }

            coleccionEncargados.Add(nuevoNombre);
            txtNuevoEncargadoInput.Clear();
            ActualizarContadorVisual();
        }

        private void btnQuitarEncargado_Click(object sender, RoutedEventArgs e)
        {
            string seleccionado = lbEncargadosCuentas.SelectedItem as string;
            if (seleccionado == null)
            {
                MessageBox.Show("Selecciona una persona de la lista para removerla.", "Atención");
                return;
            }

            coleccionEncargados.Remove(seleccionado);
            ActualizarContadorVisual();
        }

        private void btnGuardarSucursal_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombreSucursalInput.Text))
            {
                MessageBox.Show("El nombre de la sucursal no puede quedar vacío.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!coleccionEncargados.Any())
            {
                MessageBox.Show("Debes agregar al menos a un dueño o encargado de cuenta en el recuadro.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Unificamos los encargados de la colección dinámica por comas para guardarlos
                string encargadosUnificados = string.Join(", ", coleccionEncargados);

                ConexionDB db = new ConexionDB();
                db.GuardarDatosSucursal(
                    txtNombreSucursalInput.Text.Trim(),
                    encargadosUnificados,
                    encargadosUnificados
                );

                MessageBox.Show("Identidad de la sucursal y lista de encargados guardados con éxito.", "Configuración Guardada", MessageBoxButton.OK, MessageBoxImage.Information);

                Window parentWindow = Window.GetWindow(this);
                if (parentWindow is MainWindow main)
                {
                    main.Title = $"PuntoFlower - {txtNombreSucursalInput.Text.Trim().ToUpper()}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar la configuración: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAbrirPanelSeguridad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CambiarPasswordWindow ventanaSeguridad = new CambiarPasswordWindow();
                ventanaSeguridad.Owner = Window.GetWindow(this);
                ventanaSeguridad.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo desplegar el panel de seguridad local: " + ex.Message, "Error de Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarUsuariosPendientes()
        {
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string sql = "SELECT Username FROM Usuarios WHERE Estado = 'Pendiente'";
                    SqlCommand cmd = new SqlCommand(sql, con);
                    DataTable dt = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt);
                    dgUsuariosPendientes.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show("Error cargando usuarios: " + ex.Message); }
        }

        private void btnActivarUsuario_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var row = btn.DataContext as DataRowView;
            if (row == null) return;

            string usuario = row["Username"].ToString();
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string sql = "UPDATE Usuarios SET Estado = 'Activo' WHERE Username = @u";
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@u", usuario);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Usuario " + usuario + " activado correctamente.");
                CargarUsuariosPendientes();
            }
            catch (Exception ex) { MessageBox.Show("Error al activar: " + ex.Message); }
        }

        private void CargarPreciosActuales()
        {
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    SqlCommand cmd = new SqlCommand("SELECT Capacidad, Precio FROM PreciosRamos", con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int cap = (int)r["Capacidad"];
                            string p = Convert.ToDecimal(r["Precio"]).ToString("N2");
                            if (cap == 6) txtPrecio6.Text = p;
                            else if (cap == 12) txtPrecio12.Text = p;
                            else if (cap == 24) txtPrecio24.Text = p;
                            else if (cap == 36) txtPrecio36.Text = p;
                            else if (cap == 50) txtPrecio50.Text = p;
                            else if (cap == 72) txtPrecio72.Text = p;
                            else if (cap == 100) txtPrecio100.Text = p;
                            else if (cap == 150) txtPrecio150.Text = p;
                            else if (cap == 200) txtPrecio200.Text = p;
                            else if (cap == 250) txtPrecio250.Text = p;
                        }
                    }
                }
            }
            catch { }
        }

        private void btnAplicarAjustesRamos_Click(object sender, RoutedEventArgs e)
        {
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    GuardarPrecioRamo(con, 6, txtPrecio6.Text);
                    GuardarPrecioRamo(con, 12, txtPrecio12.Text);
                    GuardarPrecioRamo(con, 24, txtPrecio24.Text);
                    GuardarPrecioRamo(con, 36, txtPrecio36.Text);
                    GuardarPrecioRamo(con, 50, txtPrecio50.Text);
                    GuardarPrecioRamo(con, 72, txtPrecio72.Text);
                    GuardarPrecioRamo(con, 100, txtPrecio100.Text);
                    GuardarPrecioRamo(con, 150, txtPrecio150.Text);
                    GuardarPrecioRamo(con, 200, txtPrecio200.Text);
                    GuardarPrecioRamo(con, 250, txtPrecio250.Text);
                }
                MessageBox.Show("Todos los precios de ramos han sido actualizados.");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void GuardarPrecioRamo(SqlConnection con, int cap, string precioTxt)
        {
            if (string.IsNullOrEmpty(precioTxt)) return;
            if (decimal.TryParse(precioTxt, out decimal p))
            {
                string sql = "IF EXISTS(SELECT 1 FROM PreciosRamos WHERE Capacidad=@c) UPDATE PreciosRamos SET Precio=@p WHERE Capacidad=@c ELSE INSERT INTO PreciosRamos (Capacidad, Precio) VALUES(@c, @p)";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@c", cap);
                cmd.Parameters.AddWithValue("@p", p);
                cmd.ExecuteNonQuery();
            }
        }

        private void CargarTablaFlores()
        {
            ConexionDB db = new ConexionDB();
            listaFlores.Clear();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "SELECT Id, Nombre, PrecioCompra, PrecioVenta FROM Productos WHERE Categoria = 'Venta' ORDER BY Nombre ASC";
                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            listaFlores.Add(new FlorPrecio
                            {
                                Id = (int)r["Id"],
                                Nombre = r["Nombre"].ToString(),
                                PrecioCompra = Convert.ToDecimal(r["PrecioCompra"]),
                                PrecioVenta = Convert.ToDecimal(r["PrecioVenta"])
                            });
                        }
                    }
                }
                dgPreciosFlores.ItemsSource = listaFlores;
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void dgPreciosFlores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            florSeleccionada = dgPreciosFlores.SelectedItem as FlorPrecio;
            if (florSeleccionada != null)
            {
                txtEditNombre.Text = florSeleccionada.Nombre;
                txtEditCosto.Text = florSeleccionada.PrecioCompra.ToString("N2");
                txtEditVenta.Text = florSeleccionada.PrecioVenta.ToString("N2");
                btnGuardarFlor.IsEnabled = true;
            }
        }

        private void txtEditCosto_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtEditCosto == null || txtMargen == null || txtEditVenta == null) return;
            if (decimal.TryParse(txtEditCosto.Text, out decimal costo) && decimal.TryParse(txtMargen.Text, out decimal margen))
            {
                txtEditVenta.Text = (costo * (1 + (margen / 100))).ToString("N2");
            }
        }

        private void btnAplicarAjustesFlores_Click(object sender, RoutedEventArgs e)
        {
            if (florSeleccionada == null) return;
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    SqlCommand cmd = new SqlCommand("UPDATE Productos SET PrecioCompra=@pc, PrecioVenta=@pv WHERE Id=@id", con);
                    cmd.Parameters.AddWithValue("@pc", txtEditCosto.Text);
                    cmd.Parameters.AddWithValue("@pv", txtEditVenta.Text);
                    cmd.Parameters.AddWithValue("@id", florSeleccionada.Id);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Precio actualizado.");
                CargarTablaFlores();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void txtBuscarFlor_TextChanged(object sender, TextChangedEventArgs e)
        {
            string f = txtBuscarFlor.Text.ToLower();
            dgPreciosFlores.ItemsSource = string.IsNullOrEmpty(f) ? listaFlores : new ObservableCollection<FlorPrecio>(listaFlores.Where(x => x.Nombre.ToLower().Contains(f)));
        }
    }

    public class FlorPrecio
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
    }
}