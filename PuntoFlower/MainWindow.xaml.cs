using System;
using System.Windows;
using PuntoFlower.Views;
using System.Globalization;
using PuntoFlower.Data;
using System.Configuration;

namespace PuntoFlower
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Configurar fecha actual con cultura de México
            txtFechaActual.Text = DateTime.Now.ToString("dddd dd 'de' MMMM, yyyy", new CultureInfo("es-MX"));

            // Mostrar nombre del usuario, rol y sucursal base
            txtUsuarioLogueado.Text = $"{Session.RolActual}: {Session.UsuarioActual} ({Session.SucursalAsignada})";

            // ASIGNACIÓN DINÁMICA DEL TÍTULO DE VENTANA
            try
            {
                ConexionDB db = new ConexionDB();
                string nombreFisico = db.ObtenerNombreSucursal();
                this.Title = $"PuntoFlower - {nombreFisico} | Viendo datos de: {ConexionGlobal.SucursalActual}";
            }
            catch
            {
                this.Title = $"PuntoFlower - Viendo datos de: {ConexionGlobal.SucursalActual}";
            }

            // Aplicar restricciones de seguridad según el Rol
            ConfigurarAccesoSegunRol();

            // Vista inicial
            MainContent.Content = new DashboardView();
        }

        private void ConfigurarAccesoSegunRol()
        {
            if (Session.RolActual == "Empleado")
            {
                if (btnDashboard != null) btnDashboard.Visibility = Visibility.Collapsed;
                if (btnInventario != null) btnInventario.Visibility = Visibility.Collapsed;
                if (btnReportes != null) btnReportes.Visibility = Visibility.Collapsed;
                if (btnConfiguracion != null) btnConfiguracion.Visibility = Visibility.Collapsed;

                // El empleado NO puede ver ni tocar el selector de sucursales
                if (cmbSucursales != null) cmbSucursales.Visibility = Visibility.Collapsed;

                if (btnGastos != null)
                {
                    btnGastos.Visibility = Visibility.Visible;
                    txtTextoGastosButton.Text = "Gastos de Servicios";
                }
            }
            else
            {
                if (btnDashboard != null) btnDashboard.Visibility = Visibility.Visible;
                if (btnInventario != null) btnInventario.Visibility = Visibility.Visible;
                if (btnReportes != null) btnReportes.Visibility = Visibility.Visible;

                // Habilitamos el selector de sucursales en la nube para el Administrador
                if (cmbSucursales != null) cmbSucursales.Visibility = Visibility.Visible;

                if (btnGastos != null)
                {
                    btnGastos.Visibility = Visibility.Visible;
                    txtTextoGastosButton.Text = "Gastos y Surtido";
                }
                if (btnConfiguracion != null) btnConfiguracion.Visibility = Visibility.Visible;
            }
        }

        // --- Evento del Selector de Sucursales en la Nube (MODIFICADO PARA MULTI-TENANT) ---
        private void CmbSucursales_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbSucursales != null && cmbSucursales.SelectedItem is System.Windows.Controls.ComboBoxItem seleccion)
            {
                // Limpiamos el texto por si tu XAML incluye la palabra "Sucursal " (ej: "Sucursal Los Mochis")
                // Esto garantiza que empate exactamente con el nombre de la BD ("Los Mochis")
                string sucursalSeleccionada = seleccion.Content.ToString().Replace("Sucursal ", "").Trim();

                // 1. ACTUALIZAMOS LA VARIABLE GLOBAL (El filtro mágico)
                ConexionGlobal.SucursalActual = sucursalSeleccionada;

                // 2. Actualizamos el título de la ventana para confirmar dónde estamos
                this.Title = $"PuntoFlower - Viendo datos de: {ConexionGlobal.SucursalActual}";

                // 3. Recargamos la vista actual para que renderice los datos de la nueva ciudad seleccionada
                if (MainContent != null)
                {
                    if (MainContent.Content is DashboardView) MainContent.Content = new DashboardView();
                    else if (MainContent.Content is InventoryView) MainContent.Content = new InventoryView();
                    else if (MainContent.Content is CatalogView) MainContent.Content = new CatalogView();
                    else if (MainContent.Content is SalesView) MainContent.Content = new SalesView();
                    else if (MainContent.Content is AgendaView) MainContent.Content = new AgendaView();
                    else if (MainContent.Content is ExpensesView) MainContent.Content = new ExpensesView();
                    else if (MainContent.Content is ReportsView) MainContent.Content = new ReportsView();
                    else if (MainContent.Content is CashCloseOutView) MainContent.Content = new CashCloseOutView();
                    else if (MainContent.Content is ConfigurationView) MainContent.Content = new ConfigurationView();
                }
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