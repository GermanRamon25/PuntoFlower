using PuntoFlower.Models;
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
    public partial class ExpensesView : UserControl
    {
        public ExpensesView()
        {
            InitializeComponent();
            CargarGastosPrueba();
        }

        private void CargarGastosPrueba()
        {
            List<Gasto> listaGastos = new List<Gasto>
            {
                new Gasto { Fecha = DateTime.Now.AddDays(-2), Descripcion = "Pago a bodega floral", Categoria = "Bodega Floral", Monto = 2500.00m },
                new Gasto { Fecha = DateTime.Now.AddDays(-1), Descripcion = "Sueldo semanal Juanita", Categoria = "Pago Empleada", Monto = 1200.00m },
                new Gasto { Fecha = DateTime.Now, Descripcion = "Cloro y jabón piso", Categoria = "Material Limpieza", Monto = 150.00m }
            };

            dgGastos.ItemsSource = listaGastos;
        }
    }
}
