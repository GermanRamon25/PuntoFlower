using PuntoFlower.Models;
using PuntoFlower.Data; 
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.SqlClient; 

namespace PuntoFlower.Views
{
    public partial class SalesView : UserControl
    {
        public ObservableCollection<Venta> ProductosEnTicket { get; set; }
        private decimal totalVenta = 0;

        public SalesView()
        {
            InitializeComponent();
            ProductosEnTicket = new ObservableCollection<Venta>();
            lstVenta.ItemsSource = ProductosEnTicket;
        }

        private void btnAgregarRamo_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            string nombreProducto = "";
            decimal precio = 0;

            if (boton.Content is StackPanel panel)
            {
                var textBlocks = panel.Children.OfType<TextBlock>().ToList();
                nombreProducto = textBlocks[0].Text;

                if (nombreProducto.Contains("6")) precio = 300;
                else if (nombreProducto.Contains("12")) precio = 450;
                else if (nombreProducto.Contains("24")) precio = 850;
                else if (nombreProducto.Contains("50")) precio = 1650;
                else if (nombreProducto.Contains("100")) precio = 3450;
            }
            else
            {
                nombreProducto = boton.Content.ToString();
                if (nombreProducto.Contains("Corona")) precio = 2500;
                else if (nombreProducto.Contains("Medallón")) precio = 1200;
                else if (nombreProducto.Contains("Docena")) precio = 450;
            }

            ProductosEnTicket.Add(new Venta
            {
                ProductoNombre = nombreProducto,
                Total = precio,
                Fecha = DateTime.Now
            });

            ActualizarTotal();
        }

        // LÓGICA PARA ELIMINAR UN PRODUCTO EN ESPECIFICO 
        private void btnEliminarItem_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            var itemAEliminar = boton.DataContext as Venta;

            if (itemAEliminar != null)
            {
                ProductosEnTicket.Remove(itemAEliminar);
                ActualizarTotal();
            }
        }

        // LÓGICA PARA LIMPIAR TODO EL TICKET
        private void btnLimpiarTicket_Click(object sender, RoutedEventArgs e)
        {
            if (ProductosEnTicket.Count > 0)
            {
                var result = MessageBox.Show("¿Deseas vaciar el ticket actual?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    ProductosEnTicket.Clear();
                    ActualizarTotal();
                }
            }
        }

        // ---LOGICA PARA PROCESAR LA VENTA EN SQL ---
        private void btnConfirmarVenta_Click(object sender, RoutedEventArgs e)
        {
            if (ProductosEnTicket.Count == 0)
            {
                MessageBox.Show("El ticket está vacío.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("¿Desea finalizar la venta?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ConexionDB db = new ConexionDB();
                using (SqlConnection conexion = db.OpenConnection())
                {
                    // Usamos una transacción para que si falla un paso, no se guarde nada
                    SqlTransaction transaccion = conexion.BeginTransaction();

                    try
                    {
                        foreach (var item in ProductosEnTicket)
                        {
                            // 1. Insertar en la tabla Ventas
                            string queryVenta = "INSERT INTO Ventas (Fecha, ProductoNombre, Total, Cantidad, MetodoPago) " +
                                                "VALUES (@fecha, @nombre, @total, @cant, @metodo)";

                            SqlCommand cmdVenta = new SqlCommand(queryVenta, conexion, transaccion);
                            cmdVenta.Parameters.AddWithValue("@fecha", DateTime.Now);
                            cmdVenta.Parameters.AddWithValue("@nombre", item.ProductoNombre);
                            cmdVenta.Parameters.AddWithValue("@total", item.Total);
                            cmdVenta.Parameters.AddWithValue("@cant", 1);
                            cmdVenta.Parameters.AddWithValue("@metodo", "Efectivo");
                            cmdVenta.ExecuteNonQuery();

                            // 2. Descontar del Inventario
                            string queryStock = "UPDATE Productos SET StockActual = StockActual - 1 WHERE Nombre LIKE @nombreProd";

                            SqlCommand cmdStock = new SqlCommand(queryStock, conexion, transaccion);
                            // Al usar '%' + nombre + '%', SQL buscará cualquier producto que contenga ese texto
                            cmdStock.Parameters.AddWithValue("@nombreProd", "%" + item.ProductoNombre + "%");
                            cmdStock.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                        MessageBox.Show("Venta procesada con éxito y stock actualizado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                        ProductosEnTicket.Clear();
                        ActualizarTotal();
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        MessageBox.Show("Error al procesar la venta: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ActualizarTotal()
        {
            totalVenta = ProductosEnTicket.Sum(x => x.Total);
            txtTotal.Text = string.Format("{0:C}", totalVenta);
        }
    }
}