using PuntoFlower.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PuntoFlower.Views
{
    public partial class InventoryView : UserControl
    {
        public InventoryView()
        {
            InitializeComponent();
            CargarDatosDePrueba();
        }

        private void CargarDatosDePrueba()
        {
            // Creamos una lista basada en tu nuevo Modelo
            List<Producto> listaFlores = new List<Producto>
            {
                new Producto {
                    Nombre="Rosa Roja",
                    Categoria="Flores",
                    StockActual=50,
                    StockMinimo=20,
                    PrecioCompra=12.50m,
                    PrecioVenta=25.00m
                },
                new Producto {
                    Nombre="Tulipán Amarillo",
                    Categoria="Flores",
                    StockActual=8,
                    StockMinimo=15,
                    PrecioCompra=18.00m,
                    PrecioVenta=40.00m
                },
                new Producto {
                    Nombre="Base de Vidrio Mediana",
                    Categoria="Accesorios",
                    StockActual=12,
                    StockMinimo=5,
                    PrecioCompra=45.00m,
                    PrecioVenta=90.00m
                }
            };

            // Asignamos la lista al DataGrid que definimos en el XAML
            dgInventario.ItemsSource = listaFlores;
        }
    }
}