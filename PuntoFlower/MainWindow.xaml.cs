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

        // Lógica para el botón de Resumen
        private void btnDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new DashboardView();
        }

        // Lógica para el botón de Inventario
        private void btnInventario_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new InventoryView();
        }
    }
}