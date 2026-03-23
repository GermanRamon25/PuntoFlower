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

namespace PuntoFlower.Views
{
    public partial class ConfigurationView : UserControl
    {
        public ConfigurationView()
        {
            InitializeComponent();
        }

        // Este método se puede dejar vacío o eliminar si el botón 
        // solo está dentro de esta misma vista y no pretendes navegar a otra sub-vista.
        private void btnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            // Nota: En MainWindow.xaml el contenedor se llama "MainContent".
            // Para cambiar la vista desde aquí (un UserControl), usaríamos:
            var principal = Window.GetWindow(this) as MainWindow;
            if (principal != null)
            {
                principal.MainContent.Content = new ConfigurationView();
            }
        }
    }
}