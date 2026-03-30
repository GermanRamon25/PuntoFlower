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
using PuntoFlower.Views;
using System.Globalization; // Necesario para el formato de fecha

namespace PuntoFlower
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // ASIGNAR FECHA ACTUAL AL CARGAR EL SISTEMA
            // Formato: "dddd dd 'de' MMMM, yyyy" -> Domingo 29 de Marzo, 2026
            txtFechaActual.Text = DateTime.Now.ToString("dddd dd 'de' MMMM, yyyy", new CultureInfo("es-MX"));

            // Iniciar con el Dashboard
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

        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ReportsView();
        }

        private void btnCorteCaja_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new CashCloseOutView();
        }

        private void btnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ConfigurationView();
        }
    }
}