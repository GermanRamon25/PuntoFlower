using System;
using System.Windows;
using PuntoFlower.Views;
using System.Globalization;
using PuntoFlower.Data;
using PuntoFlower.Models;

namespace PuntoFlower
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Configurar fecha actual con cultura de México
            txtFechaActual.Text = DateTime.Now.ToString("dddd dd 'de' MMMM, yyyy", new CultureInfo("es-MX"));

            // Mostrar nombre del usuario logueado y su rol
            txtUsuarioLogueado.Text = $"{Session.RolActual}: {Session.UsuarioActual}";

            // ASIGNACIÓN DINÁMICA DE LA SUCURSAL LOCAL
            try
            {
                ConexionDB db = new ConexionDB();
                string sucursalActual = db.ObtenerNombreSucursal();
                this.Title = $"PuntoFlower - {sucursalActual}";
            }
            catch
            {
                this.Title = "PuntoFlower - Gestión de Florería";
            }

            // Aplicar restricciones de seguridad según el Rol
            ConfigurarAccesoSegunRol();

            // Vista inicial
            MainContent.Content = new DashboardView();
        }

        private void ConfigurarAccesoSegunRol()
        {
            // Caso 1: Si el usuario es un Empleado operativo en mostrador
            if (Session.RolActual == "Empleado")
            {
                // Ocultamos los módulos gerenciales clásicos administrativos
                if (btnDashboard != null) btnDashboard.Visibility = Visibility.Collapsed;
                if (btnInventario != null) btnInventario.Visibility = Visibility.Collapsed;
                if (btnReportes != null) btnReportes.Visibility = Visibility.Collapsed;
                if (btnConfiguracion != null) btnConfiguracion.Visibility = Visibility.Collapsed;

                // CORRECCIÓN: El módulo de gastos se mantiene visible pero adaptado a servicios operativos
                if (btnGastos != null)
                {
                    btnGastos.Visibility = Visibility.Visible;
                    txtTextoGastosButton.Text = "Gastos de Servicios"; // Contextualiza la etiqueta para el empleado
                }
            }
            // Caso 2: Si el usuario es Administrador o Dueño
            else
            {
                // El sistema habilita todas las funciones de auditoría y configuración al 100%
                if (btnDashboard != null) btnDashboard.Visibility = Visibility.Visible;
                if (btnInventario != null) btnInventario.Visibility = Visibility.Visible;
                if (btnReportes != null) btnReportes.Visibility = Visibility.Visible;
                if (btnGastos != null)
                {
                    btnGastos.Visibility = Visibility.Visible;
                    txtTextoGastosButton.Text = "Gastos y Surtido"; // Restaura la etiqueta administrativa para gerencia
                }
                if (btnConfiguracion != null) btnConfiguracion.Visibility = Visibility.Visible;
            }
        }

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

        // --- Métodos de Navegación del Panel Izquierdo ---

        private void btnDashboard_Click(object sender, RoutedEventArgs e) => MainContent.Content = new DashboardView();

        private void btnInventario_Click(object sender, RoutedEventArgs e) => MainContent.Content = new InventoryView();

        private void btnCatalogo_Click(object sender, RoutedEventArgs e) => MainContent.Content = new CatalogView();

        private void btnVentas_Click(object sender, RoutedEventArgs e) => MainContent.Content = new SalesView();

        private void btnAgenda_Click(object sender, RoutedEventArgs e) => MainContent.Content = new AgendaView();

        private void btnGastos_Click(object sender, RoutedEventArgs e) => MainContent.Content = new ExpensesView();

        private void btnReportes_Click(object sender, RoutedEventArgs e) => MainContent.Content = new ReportsView();

        private void btnCorteCaja_Click(object sender, RoutedEventArgs e) => MainContent.Content = new CashCloseOutView();

        private void btnConfiguracion_Click(object sender, RoutedEventArgs e) => MainContent.Content = new ConfigurationView();
    }
}