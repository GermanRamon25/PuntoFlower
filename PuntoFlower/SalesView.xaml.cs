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
        public ObservableCollection<VentaProxy> ProductosEnTicket { get; set; }
        private List<Venta> composicionRamoActual = new List<Venta>();
        private int capacidadRamo = 0;
        private decimal precioRamo = 0;
        private int floresAgregadas = 0;

        public SalesView()
        {
            InitializeComponent();
            ProductosEnTicket = new ObservableCollection<VentaProxy>();
            lstVenta.ItemsSource = ProductosEnTicket;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarInsumos();
        }

        private void CargarInsumos()
        {
            List<Producto> lista = new List<Producto>();
            ConexionDB db = new ConexionDB();
            using (SqlConnection con = db.OpenConnection())
            {
                string query = "SELECT Nombre FROM Productos";
                SqlCommand cmd = new SqlCommand(query, con);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) lista.Add(new Producto { Nombre = r["Nombre"].ToString() });
                }
            }
            cbInsumos.ItemsSource = lista;
        }

        private void Ramo_Checked(object sender, RoutedEventArgs e)
        {
            var rb = sender as RadioButton;
            capacidadRamo = int.Parse(rb.Tag.ToString());

            switch (capacidadRamo)
            {
                case 6: precioRamo = 300; break;
                case 12: precioRamo = 450; break;
                case 18: precioRamo = 650; break;
                case 24: precioRamo = 850; break;
                case 36: precioRamo = 1200; break;
                case 50: precioRamo = 1650; break;
            }
            ActualizarProgreso();
        }

        private void btnAgregarAlRamo_Click(object sender, RoutedEventArgs e)
        {
            var flor = cbInsumos.SelectedItem as Producto;
            if (flor == null || capacidadRamo == 0) return;

            if (!int.TryParse(txtCantFlor.Text, out int cant) || cant <= 0) return;

            if (floresAgregadas + cant > capacidadRamo)
            {
                MessageBox.Show("Superas la capacidad del ramo seleccionado.");
                return;
            }

            composicionRamoActual.Add(new Venta { ProductoNombre = flor.Nombre, Cantidad = cant });
            floresAgregadas += cant;
            ActualizarProgreso();
            txtCantFlor.Text = "0";
        }

        private void btnFinalizarRamo_Click(object sender, RoutedEventArgs e)
        {
            if (floresAgregadas != capacidadRamo)
            {
                MessageBox.Show($"Debes completar las {capacidadRamo} flores. Llevas {floresAgregadas}.");
                return;
            }

            string detalle = string.Join(", ", composicionRamoActual.Select(x => $"{x.Cantidad} {x.ProductoNombre}"));

            ProductosEnTicket.Add(new VentaProxy
            {
                ProductoNombre = $"Ramo {capacidadRamo} pz ({detalle})",
                Total = precioRamo,
                FloresInternas = new List<Venta>(composicionRamoActual)
            });

            composicionRamoActual.Clear();
            floresAgregadas = 0;
            capacidadRamo = 0;
            ActualizarTotal();
            ActualizarProgreso();
        }

        private void btnConfirmarVenta_Click(object sender, RoutedEventArgs e)
        {
            if (ProductosEnTicket.Count == 0) return;

            ConexionDB db = new ConexionDB();
            using (SqlConnection con = db.OpenConnection())
            {
                SqlTransaction tra = con.BeginTransaction();
                try
                {
                    foreach (var item in ProductosEnTicket)
                    {
                        SqlCommand cmdV = new SqlCommand("INSERT INTO Ventas (Fecha, ProductoNombre, Total, Cantidad, MetodoPago) VALUES (GETDATE(), @n, @t, 1, 'Efectivo')", con, tra);
                        cmdV.Parameters.AddWithValue("@n", item.ProductoNombre);
                        cmdV.Parameters.AddWithValue("@t", item.Total);
                        cmdV.ExecuteNonQuery();

                        foreach (var f in item.FloresInternas)
                        {
                            SqlCommand cmdS = new SqlCommand("UPDATE Productos SET StockActual = StockActual - @cant WHERE Nombre = @nom", con, tra);
                            cmdS.Parameters.AddWithValue("@cant", f.Cantidad);
                            cmdS.Parameters.AddWithValue("@nom", f.ProductoNombre);
                            cmdS.ExecuteNonQuery();
                        }
                    }
                    tra.Commit();
                    MessageBox.Show("Venta Exitosa.");
                    ProductosEnTicket.Clear();
                    ActualizarTotal();
                }
                catch (Exception ex) { tra.Rollback(); MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void btnEliminarItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).DataContext as VentaProxy;
            if (item != null)
            {
                ProductosEnTicket.Remove(item);
                ActualizarTotal();
            }
        }

        private void btnLimpiarTicket_Click(object sender, RoutedEventArgs e)
        {
            if (ProductosEnTicket.Count > 0)
            {
                ProductosEnTicket.Clear();
                ActualizarTotal();
            }
        }

        private void ActualizarProgreso() => lblProgresoRamo.Text = $"Flores seleccionadas: {floresAgregadas} / {capacidadRamo}";
        private void ActualizarTotal() => txtTotal.Text = $"Total: {ProductosEnTicket.Sum(x => x.Total):C}";

        // Clase auxiliar para manejar la lista de flores dentro del ticket
        public class VentaProxy
        {
            public string ProductoNombre { get; set; }
            public decimal Total { get; set; }
            public List<Venta> FloresInternas { get; set; }
        }
    }
}