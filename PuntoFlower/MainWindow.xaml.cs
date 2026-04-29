using System;
using System.Windows;
using PuntoFlower.Views;
using System.Globalization;
using PuntoFlower.Data; 

namespace PuntoFlower
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            txtFechaActual.Text = DateTime.Now.ToString("dddd dd 'de' MMMM, yyyy", new CultureInfo("es-MX"));

            // Mostrar nombre del usuario logueado
            txtUsuarioLogueado.Text = $"Empleado: {Session.UsuarioActual}";

            MainContent.Content = new DashboardView();
        }

        // --- NUEVA LÓGICA DE CIERRE DE SESIÓN ---
        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Estás seguro que deseas cerrar sesión?", "Cerrar Sesión", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Session.CerrarSesion();
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }

        // Métodos de navegación existentes...
        private void btnDashboard_Click(object sender, RoutedEventArgs e) => MainContent.Content = new DashboardView();
        private void btnInventario_Click(object sender, RoutedEventArgs e) => MainContent.Content = new InventoryView();
        private void btnCatalogo_Click(object sender, RoutedEventArgs e) => MainContent.Content = new CatalogView();
        private void btnVentas_Click(object sender, RoutedEventArgs e) => MainContent.Content = new SalesView();
        private void btnAgenda_Click(object sender, RoutedEventArgs e) => MainContent.Content = new AgendaView();
        private void btnGastos_Click(object sender, RoutedEventArgs e) => MainContent.Content = new ProveedoresView();
        private void btnReportes_Click(object sender, RoutedEventArgs e) => MainContent.Content = new ReportsView();
        private void btnCorteCaja_Click(object sender, RoutedEventArgs e) => MainContent.Content = new CashCloseOutView();
        private void btnConfiguracion_Click(object sender, RoutedEventArgs e) => MainContent.Content = new ConfigurationView();
    }
}