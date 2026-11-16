using System;
using System.Windows;
using PuntoFlower.Views;
using System.Globalization;
using PuntoFlower.Data;
using System.Windows.Controls;
using System.Collections.Generic;

namespace PuntoFlower
{
    public partial class MainWindow : Window
    {
        // Bandera para evitar recargas múltiples al arrancar
        private bool isInicializando = true;

        public MainWindow()
        {
            InitializeComponent();

            // 1. Configurar fecha actual
            txtFechaActual.Text = DateTime.Now.ToString("dddd dd 'de' MMMM, yyyy", new CultureInfo("es-MX"));

            // ====================================================================
            // 2. ETIQUETAS VISUALES PERSONALIZADAS PARA ADMINISTRADORES
            // ====================================================================
            string usuarioFiltro = Session.UsuarioActual.ToLower();
            string etiquetaPersonalizada = $"{Session.RolActual}: {Session.UsuarioActual} ({Session.SucursalAsignada})"; // Etiqueta por defecto

            if (Session.RolActual == "Admin")
            {
                if (usuarioFiltro == "crisbel" || usuarioFiltro == "yeseli")
                {
                    etiquetaPersonalizada = $"Admin Universal: {Session.UsuarioActual} (Todas)";
                }
                else if (usuarioFiltro == "omar")
                {
                    etiquetaPersonalizada = $"Admin: {Session.UsuarioActual} (Mochis, Gve, Gml)";
                }
                else if (usuarioFiltro == "leticia")
                {
                    etiquetaPersonalizada = $"Admin: {Session.UsuarioActual} (Gve, Mochis)";
                }
                else
                {
                    etiquetaPersonalizada = $"Admin: {Session.UsuarioActual} ({Session.SucursalAsignada})";
                }
            }

            txtUsuarioLogueado.Text = etiquetaPersonalizada;

            // 3. Forzamos el seleccionador visual a la sucursal de este usuario
            AutoSeleccionarSucursalDeSesion();

            // 4. Aplicamos restricciones de seguridad
            ConfigurarAccesoSegunRol();

            isInicializando = false;

            // 5. Vista inicial
            MainContent.Content = new DashboardView();
        }

        private void AutoSeleccionarSucursalDeSesion()
        {
            if (string.IsNullOrEmpty(Session.SucursalAsignada)) return;

            // Garantizar que ConexionGlobal tenga la sucursal correcta
            ConexionGlobal.SucursalActual = Session.SucursalAsignada;

            this.Title = $"PuntoFlower - Viendo datos de: {ConexionGlobal.SucursalActual}";

            // Buscar en el ComboBox el Item que coincide con la sucursal asignada
            foreach (ComboBoxItem item in cmbSucursales.Items)
            {
                string nombreLimpioItem = item.Content.ToString().Replace("Sucursal ", "").Trim();

                if (nombreLimpioItem.Equals(Session.SucursalAsignada, StringComparison.OrdinalIgnoreCase))
                {
                    cmbSucursales.SelectedItem = item;
                    break;
                }
            }
        }

        private void ConfigurarAccesoSegunRol()
        {
            if (Session.RolActual == "Empleado")
            {
                // Un empleado NO ve estas pestañas
                if (btnDashboard != null) btnDashboard.Visibility = Visibility.Collapsed;
                if (btnInventario != null) btnInventario.Visibility = Visibility.Collapsed;
                if (btnReportes != null) btnReportes.Visibility = Visibility.Collapsed;
                if (btnConfiguracion != null) btnConfiguracion.Visibility = Visibility.Collapsed;

                // EL CANDADO PRINCIPAL: El empleado puede ver en qué sucursal está, pero NO puede cambiarla.
                if (cmbSucursales != null) cmbSucursales.IsEnabled = false;

                if (btnGastos != null)
                {
                    btnGastos.Visibility = Visibility.Visible;
                    txtTextoGastosButton.Text = "Gastos de Servicios";
                }
            }
            else
            {
                // Un Administrador ve todo y puede cambiar libremente entre todas las sucursales
                if (btnDashboard != null) btnDashboard.Visibility = Visibility.Visible;
                if (btnInventario != null) btnInventario.Visibility = Visibility.Visible;
                if (btnReportes != null) btnReportes.Visibility = Visibility.Visible;
                if (btnConfiguracion != null) btnConfiguracion.Visibility = Visibility.Visible;

                if (cmbSucursales != null) cmbSucursales.IsEnabled = true;

                if (btnGastos != null)
                {
                    btnGastos.Visibility = Visibility.Visible;
                    txtTextoGastosButton.Text = "Gastos y Surtido";
                }
            }
        }

        // --- Evento del Selector de Sucursales en la Nube ---
        private void CmbSucursales_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInicializando) return; // Evita que se recargue la vista antes de estar listos

            if (cmbSucursales != null && cmbSucursales.SelectedItem is ComboBoxItem seleccion)
            {
                string sucursalSeleccionada = seleccion.Content.ToString().Replace("Sucursal ", "").Trim();

                // 1. ACTUALIZAMOS LA VARIABLE GLOBAL
                ConexionGlobal.SucursalActual = sucursalSeleccionada;

                // 2. Actualizamos el título de la ventana
                this.Title = $"PuntoFlower - Viendo datos de: {ConexionGlobal.SucursalActual}";

                // 3. Recargamos la vista actual
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