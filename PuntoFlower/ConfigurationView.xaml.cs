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

namespace PuntoFlower.Views
{
    public partial class ConfigurationView : UserControl
    {
        public ConfigurationView()
        {
            InitializeComponent();
        }

        private void btnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            // Cambiamos el contenido del ContentControl de la ventana principal
            ContentPrincipal.Content = new ConfigurationView();
        }
    }
}