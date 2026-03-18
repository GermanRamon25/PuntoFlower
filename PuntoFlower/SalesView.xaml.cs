using PuntoFlower.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

        // LÓGICA PARA ELIMINAR UN ITEM ESPECÍFICO
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

        private void ActualizarTotal()
        {
            totalVenta = ProductosEnTicket.Sum(x => x.Total);
            txtTotal.Text = string.Format("{0:C}", totalVenta);
        }
    }
}