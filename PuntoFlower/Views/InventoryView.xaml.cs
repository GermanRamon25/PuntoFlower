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
            CargarDatosReales();
        }

        private void CargarDatosReales()
        {
            // Esta lista ahora refleja la realidad de la florería
            List<Producto> listaProductos = new List<Producto>
            {
                // Basado en la lista de precios de Ramos (Imagen 2)
                new Producto { Nombre="Ramo Básico", TipoVenta="Ramos", CantidadFlores=6, StockActual=15, StockMinimo=5, PrecioVenta=300m, Categoria="Venta" },
                new Producto { Nombre="Ramo Estándar", TipoVenta="Ramos", CantidadFlores=12, StockActual=20, StockMinimo=10, PrecioVenta=450m, Categoria="Venta" },
                new Producto { Nombre="Ramo Especial", TipoVenta="Ramos", CantidadFlores=24, StockActual=8, StockMinimo=5, PrecioVenta=850m, Categoria="Venta" },
                new Producto { Nombre="Ramo Medio Ciento", TipoVenta="Ramos", CantidadFlores=50, StockActual=4, StockMinimo=2, PrecioVenta=1650m, Categoria="Venta" },
                new Producto { Nombre="Ramo de Ciento", TipoVenta="Ramos", CantidadFlores=100, StockActual=2, StockMinimo=1, PrecioVenta=3450m, Categoria="Venta" },
                
                // Basado en lo que más se vende (Imagen 1)
                new Producto { Nombre="Corona Fúnebre", TipoVenta="Coronas", StockActual=3, StockMinimo=2, PrecioVenta=2800m, Categoria="Venta" },
                new Producto { Nombre="Medallón Floral", TipoVenta="Medallones", StockActual=5, StockMinimo=3, PrecioVenta=1500m, Categoria="Venta" },
                
                // Ejemplo de Insumo (Lo que se compra a bodega)
                new Producto { Nombre="Paquete Gipsofila", TipoVenta="Insumo", StockActual=2, StockMinimo=5, PrecioCompra=80m, Categoria="Bodega" }
            };

            dgInventario.ItemsSource = listaProductos;
        }
    }
}