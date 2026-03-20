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
using PuntoFlower.Views; // Referencia necesaria para ver las carpetas de vistas

namespace PuntoFlower
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Al arrancar, nos aseguramos de que el Dashboard sea lo primero en verse
            MainContent.Content = new DashboardView();
        }

        // LOGICA PARA EL BOTON DE RESUMEN
        private void btnDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new DashboardView();
        }

        // LOGICA PARA EL BOTON DE INVENTARIO 
        private void btnInventario_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new InventoryView();
        }

        // LOGICA PARA LA VENTA 
        private void btnVentas_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new SalesView();
        }

        // NUEVO: LOGICA PARA LA AGENDA DE PEDIDOS
        private void btnAgenda_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new AgendaView();
        }

        // METODO PARA NAVEGAR A LOS GASTOS 
        private void btnGastos_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ExpensesView();
        }
    }
}