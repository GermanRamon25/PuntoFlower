using System;
using System.Collections.ObjectModel;
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
        private FlorPrecio florSeleccionada;

        public ConfigurationView()
        {
            InitializeComponent();
            CargarPreciosActuales();
            CargarTablaFlores();
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
                            else if (cap == 50) txtPrecio50.Text = p;
                        }
                    }
                }
            }
            catch { }
        }

        private void CargarTablaFlores()
        {
            ConexionDB db = new ConexionDB();
            listaFlores.Clear();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    SqlCommand cmd = new SqlCommand("SELECT Id, Nombre, PrecioCompra, PrecioVenta FROM Productos ORDER BY Nombre ASC", con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            listaFlores.Add(new FlorPrecio
                            {
                                Id = (int)r["Id"],
                                Nombre = r["Nombre"].ToString(),
                                PrecioCompra = r["PrecioCompra"] != DBNull.Value ? Convert.ToDecimal(r["PrecioCompra"]) : 0,
                                PrecioVenta = r["PrecioVenta"] != DBNull.Value ? Convert.ToDecimal(r["PrecioVenta"]) : 0
                            });
                        }
                    }
                }
                dgPreciosFlores.ItemsSource = listaFlores;
            }
            catch (Exception ex) { MessageBox.Show("Error al cargar tabla: " + ex.Message); }
        }

        // --- LÓGICA DE SELECCIÓN ---
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
            // Esta línea es CRITICA: evita el error al cargar la pantalla
            if (txtEditCosto == null || txtMargen == null || txtEditVenta == null) return;

            if (decimal.TryParse(txtEditCosto.Text, out decimal costo) &&
                decimal.TryParse(txtMargen.Text, out decimal margen))
            {
                decimal ventaSugerida = costo * (1 + (margen / 100));
                txtEditVenta.Text = ventaSugerida.ToString("N2");
            }
        }

        // --- GUARDADO DE FLORES INDIVIDUALES ---
        private void btnAplicarAjustesFlores_Click(object sender, RoutedEventArgs e)
        {
            if (florSeleccionada == null) return;

            if (decimal.TryParse(txtEditCosto.Text, out decimal nCosto) && decimal.TryParse(txtEditVenta.Text, out decimal nVenta))
            {
                ConexionDB db = new ConexionDB();
                try
                {
                    using (SqlConnection con = db.OpenConnection())
                    {
                        string sql = "UPDATE Productos SET PrecioCompra=@pc, PrecioVenta=@pv WHERE Id=@id";
                        SqlCommand cmd = new SqlCommand(sql, con);
                        cmd.Parameters.AddWithValue("@pc", nCosto);
                        cmd.Parameters.AddWithValue("@pv", nVenta);
                        cmd.Parameters.AddWithValue("@id", florSeleccionada.Id);
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("¡Precio de " + florSeleccionada.Nombre + " actualizado!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    CargarTablaFlores();
                }
                catch (Exception ex) { MessageBox.Show("Error al actualizar: " + ex.Message); }
            }
        }

        // --- GUARDADO DE RAMOS (LISTA) ---
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
                    GuardarPrecioRamo(con, 50, txtPrecio50.Text);
                }
                MessageBox.Show("Precios de ramos actualizados correctamente.", "PuntoFlower");
            }
            catch (Exception ex) { MessageBox.Show("Error al guardar ramos: " + ex.Message); }
        }

        private void GuardarPrecioRamo(SqlConnection con, int cap, string precioTxt)
        {
            if (decimal.TryParse(precioTxt, out decimal p))
            {
                string sql = "IF EXISTS(SELECT 1 FROM PreciosRamos WHERE Capacidad=@c) " +
                             "UPDATE PreciosRamos SET Precio=@p WHERE Capacidad=@c " +
                             "ELSE INSERT INTO PreciosRamos (Capacidad, Precio) VALUES(@c, @p)";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@c", cap);
                cmd.Parameters.AddWithValue("@p", p);
                cmd.ExecuteNonQuery();
            }
        }

        // --- BÚSQUEDA ---
        private void txtBuscarFlor_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = txtBuscarFlor.Text.ToLower();
            if (string.IsNullOrEmpty(filtro))
                dgPreciosFlores.ItemsSource = listaFlores;
            else
                dgPreciosFlores.ItemsSource = listaFlores.Where(f => f.Nombre.ToLower().Contains(filtro)).ToList();
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