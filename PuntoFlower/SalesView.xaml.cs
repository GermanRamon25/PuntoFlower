using PuntoFlower.Models;
using PuntoFlower.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.SqlClient;

namespace PuntoFlower.Views
{
    public partial class SalesView : UserControl
    {
        public ObservableCollection<ItemTicket> ProductosEnTicket { get; set; }
        private List<DetalleInsumo> composicionRamoActual = new List<DetalleInsumo>();
        private int capacidadRamo = 0;
        private decimal precioRamo = 0;
        private int floresAgregadas = 0;

        // Diccionario para almacenar los precios configurados en la DB
        private Dictionary<int, decimal> preciosDinamicos = new Dictionary<int, decimal>();

        public SalesView()
        {
            InitializeComponent();
            ProductosEnTicket = new ObservableCollection<ItemTicket>();
            lstVenta.ItemsSource = ProductosEnTicket;
            CargarPreciosDesdeDB();
        }

        private void CargarPreciosDesdeDB()
        {
            preciosDinamicos.Clear();
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "SELECT Capacidad, Precio FROM PreciosRamos";
                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int cap = (int)r["Capacidad"];
                            decimal precio = Convert.ToDecimal(r["Precio"]);
                            preciosDinamicos.Add(cap, precio);

                            // ACTUALIZACIÓN VISUAL DE LOS BOTONES
                            ActualizarTextoBoton(cap, precio);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Si falla, se usarán los valores por defecto en Ramo_Checked
            }
        }

        // Método auxiliar para escribir el precio sobre el botón en la interfaz
        private void ActualizarTextoBoton(int capacidad, decimal precio)
        {
            string texto = $"{capacidad} pz ({precio:C0})";
            switch (capacidad)
            {
                case 6: rbRamo6.Content = texto; break;
                case 12: rbRamo12.Content = texto; break;
                case 24: rbRamo24.Content = texto; break;
                case 50: rbRamo50.Content = texto; break;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarInsumos();
            CargarPreciosDesdeDB(); // Refrescar precios cada vez que se entra a la pestaña
        }

        private void CargarInsumos()
        {
            List<Producto> lista = new List<Producto>();
            ConexionDB db = new ConexionDB();
            using (SqlConnection con = db.OpenConnection())
            {
                string query = "SELECT Nombre, PrecioVenta FROM Productos";
                SqlCommand cmd = new SqlCommand(query, con);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) lista.Add(new Producto
                    {
                        Nombre = r["Nombre"].ToString(),
                        PrecioVenta = Convert.ToDecimal(r["PrecioVenta"])
                    });
                }
            }
            cbInsumosRamos.ItemsSource = lista;
            cbInsumosLibre.ItemsSource = lista;
        }

        private void txtPagoCon_TextChanged(object sender, TextChangedEventArgs e)
        {
            decimal total = ProductosEnTicket.Sum(x => x.Total);
            if (decimal.TryParse(txtPagoCon.Text, out decimal pago))
            {
                decimal cambio = pago - total;
                txtCambio.Text = (cambio >= 0) ? cambio.ToString("C") : "$0.00";
            }
            else
            {
                txtCambio.Text = "$0.00";
            }
        }

        private void Ramo_Checked(object sender, RoutedEventArgs e)
        {
            var rb = sender as RadioButton;
            if (rb == null || rb.Tag == null) return;

            capacidadRamo = int.Parse(rb.Tag.ToString());

            if (preciosDinamicos.ContainsKey(capacidadRamo))
            {
                precioRamo = preciosDinamicos[capacidadRamo];
            }
            else
            {
                // Backup por si la DB está vacía
                switch (capacidadRamo)
                {
                    case 6: precioRamo = 300; break;
                    case 12: precioRamo = 450; break;
                    case 18: precioRamo = 650; break;
                    case 24: precioRamo = 850; break;
                    case 50: precioRamo = 1650; break;
                    default: precioRamo = 0; break;
                }
            }
            ActualizarProgreso();
        }

        private void btnAgregarAlRamo_Click(object sender, RoutedEventArgs e)
        {
            var flor = cbInsumosRamos.SelectedItem as Producto;
            if (flor == null || capacidadRamo == 0) return;
            if (!int.TryParse(txtCantFlorRamo.Text, out int cant) || cant <= 0) return;
            if (floresAgregadas + cant > capacidadRamo) { MessageBox.Show("Capacidad excedida."); return; }
            composicionRamoActual.Add(new DetalleInsumo { Nombre = flor.Nombre, Cantidad = cant });
            floresAgregadas += cant;
            ActualizarProgreso();
            txtCantFlorRamo.Text = "0";
        }

        private void btnFinalizarRamo_Click(object sender, RoutedEventArgs e)
        {
            if (floresAgregadas != capacidadRamo) { MessageBox.Show("Completa las flores del ramo."); return; }
            ProductosEnTicket.Add(new ItemTicket
            {
                ProductoNombre = $"Ramo {capacidadRamo} pz",
                Total = precioRamo,
                InsumosADescontar = new List<DetalleInsumo>(composicionRamoActual),
                DetalleVisual = string.Join(", ", composicionRamoActual.Select(x => $"{x.Cantidad} {x.Nombre}"))
            });
            LimpiarConfiguradorRamo();
        }

        private void btnAgregarVentaLibre_Click(object sender, RoutedEventArgs e)
        {
            var prod = cbInsumosLibre.SelectedItem as Producto;
            if (prod == null || !int.TryParse(txtCantLibre.Text, out int cant)) return;
            ProductosEnTicket.Add(new ItemTicket
            {
                ProductoNombre = prod.Nombre,
                Total = prod.PrecioVenta * cant,
                InsumosADescontar = new List<DetalleInsumo> { new DetalleInsumo { Nombre = prod.Nombre, Cantidad = cant } },
                DetalleVisual = $"{cant} unidad(es) x {prod.PrecioVenta:C}"
            });
            ActualizarTotal();
        }

        private void btnAgregarEspecial_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtNombreEspecial.Text) || !decimal.TryParse(txtPrecioEspecial.Text, out decimal precio)) return;
            ProductosEnTicket.Add(new ItemTicket
            {
                ProductoNombre = txtNombreEspecial.Text,
                Total = precio,
                InsumosADescontar = new List<DetalleInsumo>(),
                DetalleVisual = "Arreglo / Servicio Especial"
            });
            txtNombreEspecial.Text = ""; txtPrecioEspecial.Text = "";
            ActualizarTotal();
        }

        private void btnConfirmarVenta_Click(object sender, RoutedEventArgs e)
        {
            decimal total = ProductosEnTicket.Sum(x => x.Total);
            if (total <= 0) return;
            if (!decimal.TryParse(txtPagoCon.Text, out decimal pagoRecibido) || pagoRecibido < total)
            {
                MessageBox.Show("Monto recibido insuficiente.", "Cobro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            decimal cambioFinal = pagoRecibido - total;
            ConexionDB db = new ConexionDB();
            using (SqlConnection con = db.OpenConnection())
            {
                SqlTransaction tra = con.BeginTransaction();
                try
                {
                    foreach (var item in ProductosEnTicket)
                    {
                        string q = "INSERT INTO Ventas (Fecha, ProductoNombre, Total, Cantidad, MetodoPago, MontoRecibido, MontoCambio) " +
                                   "VALUES (GETDATE(), @n, @t, 1, 'Efectivo', @rec, @cam)";
                        SqlCommand cmdV = new SqlCommand(q, con, tra);
                        cmdV.Parameters.AddWithValue("@n", item.ProductoNombre);
                        cmdV.Parameters.AddWithValue("@t", item.Total);
                        cmdV.Parameters.AddWithValue("@rec", pagoRecibido);
                        cmdV.Parameters.AddWithValue("@cam", cambioFinal);
                        cmdV.ExecuteNonQuery();

                        foreach (var insumo in item.InsumosADescontar)
                        {
                            SqlCommand cmdS = new SqlCommand("UPDATE Productos SET StockActual = StockActual - @c WHERE Nombre = @nom", con, tra);
                            cmdS.Parameters.AddWithValue("@c", insumo.Cantidad);
                            cmdS.Parameters.AddWithValue("@nom", insumo.Nombre);
                            cmdS.ExecuteNonQuery();
                        }
                    }
                    tra.Commit();
                    MessageBox.Show($"¡Venta completada!\nCambio: {cambioFinal:C}");
                    ProductosEnTicket.Clear();
                    txtPagoCon.Clear();
                    txtCambio.Text = "$0.00";
                    ActualizarTotal();
                }
                catch (Exception ex) { tra.Rollback(); MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void LimpiarConfiguradorRamo()
        {
            composicionRamoActual.Clear(); floresAgregadas = 0; capacidadRamo = 0;
            ActualizarTotal(); ActualizarProgreso();
        }

        private void ActualizarProgreso() => lblProgresoRamo.Text = $"Seleccionadas: {floresAgregadas} / {capacidadRamo}";
        private void ActualizarTotal() => txtTotal.Text = $"Total: {ProductosEnTicket.Sum(x => x.Total):C}";
        private void btnLimpiarTicket_Click(object sender, RoutedEventArgs e) { ProductosEnTicket.Clear(); ActualizarTotal(); }
        private void btnEliminarItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).DataContext as ItemTicket;
            if (item != null) { ProductosEnTicket.Remove(item); ActualizarTotal(); }
        }

        public class ItemTicket
        {
            public string ProductoNombre { get; set; }
            public decimal Total { get; set; }
            public string DetalleVisual { get; set; }
            public List<DetalleInsumo> InsumosADescontar { get; set; }
        }
        public class DetalleInsumo
        {
            public string Nombre { get; set; }
            public int Cantidad { get; set; }
        }
    }
}