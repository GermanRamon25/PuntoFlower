using System;
using System.Windows;
using PuntoFlower.Views;
using System.Globalization;
using PuntoFlower.Data;
using PuntoFlower.Models;
using System.Configuration; // Agregado para leer el App.config de forma segura

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

                // Ocultamos el selector de sucursales para que no cambien de BD
                if (cmbSucursales != null) cmbSucursales.Visibility = Visibility.Collapsed;

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

                // Habilitamos el selector de sucursales en la nube
                if (cmbSucursales != null) cmbSucursales.Visibility = Visibility.Visible;

                if (btnGastos != null)
                {
                    btnGastos.Visibility = Visibility.Visible;
                    txtTextoGastosButton.Text = "Gastos y Surtido"; // Restaura la etiqueta administrativa para gerencia
                }
                if (btnConfiguracion != null) btnConfiguracion.Visibility = Visibility.Visible;
            }
        }

        // --- Evento del Selector de Sucursales en la Nube ---
        private void CmbSucursales_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Validamos que el componente ya esté cargado
            if (cmbSucursales != null && cmbSucursales.SelectedItem is System.Windows.Controls.ComboBoxItem seleccion)
            {
                string sucursalSeleccionada = seleccion.Content.ToString();

                // 1. Actualizamos la variable global para saber dónde estamos parados
                ConexionGlobal.SucursalActual = sucursalSeleccionada;

                // 2. Cambiamos la conexión dinámicamente según la selección
                if (sucursalSeleccionada == "Matriz (Pruebas)")
                {
                    // Solución segura: Leemos la conexión original de tu App.config, sin exponer contraseñas aquí
                    ConexionGlobal.CadenaConexionActiva = ConfigurationManager.ConnectionStrings["PuntoFlowerDB"].ConnectionString;
                }
                else if (sucursalSeleccionada == "Sucursal Los Mochis")
                {
                    // Espacio preparado para cuando crees la BD de Los Mochis en tu servidor de Azure
                    // ConexionGlobal.CadenaConexionActiva = "Server=tcp:servidor-puntoflower-sin.database.windows.net,1433;Initial Catalog=PuntoFlower_Mochis;Persist Security Info=False;User ID=puntofloweradmin;Password=TU_CONTRASEÑA;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
                }
                else if (sucursalSeleccionada == "Sucursal Guasave")
                {
                    // Espacio preparado para Guasave
                }
                else if (sucursalSeleccionada == "Sucursal Culiacán")
                {
                    // Espacio preparado para Culiacán
                }

                // 3. Actualizamos el título de la ventana
                this.Title = $"PuntoFlower - {sucursalSeleccionada}";

                // 4. Si el administrador está viendo el Dashboard, lo recargamos para que muestre los números de la nueva sucursal
                if (MainContent != null && MainContent.Content is DashboardView)
                {
                    MainContent.Content = new DashboardView();
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