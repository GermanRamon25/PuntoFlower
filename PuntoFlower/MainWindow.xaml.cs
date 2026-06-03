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
                // Ocultamos los módulos gerenciales clásicos
                btnDashboard.Visibility = Visibility.Collapsed;
                btnInventario.Visibility = Visibility.Collapsed;
                btnReportes.Visibility = Visibility.Collapsed;
                btnGastos.Visibility = Visibility.Collapsed;
                btnConfiguracion.Visibility = Visibility.Collapsed;
            }
            // Caso 2: Si el usuario es Administrador o Dueño
            else if (Session.RolActual == "Admin")
            {
                // El sistema habilita todas las funciones de auditoría y configuración
                btnDashboard.Visibility = Visibility.Visible;
                btnInventario.Visibility = Visibility.Visible;
                btnReportes.Visibility = Visibility.Visible;
                btnGastos.Visibility = Visibility.Visible;
                btnConfiguracion.Visibility = Visibility.Visible;
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

        // MODIFICADO: Ahora carga correctamente la vista ExpensesView en lugar de ProveedoresView
        private void btnGastos_Click(object sender, RoutedEventArgs e) => MainContent.Content = new ExpensesView();

        private void btnReportes_Click(object sender, RoutedEventArgs e) => MainContent.Content = new ReportsView();

        private void btnCorteCaja_Click(object sender, RoutedEventArgs e) => MainContent.Content = new CashCloseOutView();

        private void btnConfiguracion_Click(object sender, RoutedEventArgs e) => MainContent.Content = new ConfigurationView();
    }
}