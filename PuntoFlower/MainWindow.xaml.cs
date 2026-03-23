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
            MainContent.Content = new DashboardView();
        }

        private void btnDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new DashboardView();
        }

        private void btnInventario_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new InventoryView();
        }

        private void btnVentas_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new SalesView();
        }

        private void btnAgenda_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new AgendaView();
        }

        private void btnGastos_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ProveedoresView();
        }

        // NUEVO MÉTODO PARA REPORTES
        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ReportsView();
        }
    }
}