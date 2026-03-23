using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            dpInicio.SelectedDate = DateTime.Now.AddDays(-7); // Por defecto última semana
            dpFin.SelectedDate = DateTime.Now;
        }

        private void btnGenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            if (dpInicio.SelectedDate == null || dpFin.SelectedDate == null) return;

            DateTime inicio = dpInicio.SelectedDate.Value;
            DateTime fin = dpFin.SelectedDate.Value;

            decimal totalVentas = 0, totalGastos = 0;
            List<object> movimientos = new List<object>();
            ConexionDB db = new ConexionDB();

            using (SqlConnection con = db.OpenConnection())
            {
                // 1. Obtener Ventas
                string qVentas = "SELECT Fecha, ProductoNombre, Total FROM Ventas WHERE Fecha BETWEEN @i AND @f";
                SqlCommand cmdV = new SqlCommand(qVentas, con);
                cmdV.Parameters.AddWithValue("@i", inicio);
                cmdV.Parameters.AddWithValue("@f", fin.AddDays(1)); // Para incluir el día completo
                using (SqlDataReader r = cmdV.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal m = (decimal)r["Total"];
                        totalVentas += m;
                        movimientos.Add(new { Fecha = r["Fecha"], Concepto = "Venta: " + r["ProductoNombre"], Tipo = "Ingreso", Monto = m });
                    }
                }

                // 2. Obtener Gastos de Surtido (Proveedores)
                string qSurtido = "SELECT Fecha, ProductoNombre, (Cantidad * PrecioCosto) as Total FROM DetalleCompras WHERE Fecha BETWEEN @i AND @f";
                SqlCommand cmdS = new SqlCommand(qSurtido, con);
                cmdS.Parameters.AddWithValue("@i", inicio);
                cmdS.Parameters.AddWithValue("@f", fin.AddDays(1));
                using (SqlDataReader r = cmdS.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal m = (decimal)r["Total"];
                        totalGastos += m;
                        movimientos.Add(new { Fecha = r["Fecha"], Concepto = "Surtido: " + r["ProductoNombre"], Tipo = "Egreso", Monto = m });
                    }
                }

                // 3. Obtener Gastos Operativos (Luz, Renta, etc.)
                string qGastos = "SELECT Fecha, Descripcion, Monto FROM Gastos WHERE Fecha BETWEEN @i AND @f";
                SqlCommand cmdG = new SqlCommand(qGastos, con);
                cmdG.Parameters.AddWithValue("@i", inicio);
                cmdG.Parameters.AddWithValue("@f", fin.AddDays(1));
                using (SqlDataReader r = cmdG.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal m = (decimal)r["Monto"];
                        totalGastos += m;
                        movimientos.Add(new { Fecha = r["Fecha"], Concepto = r["Descripcion"], Tipo = "Egreso", Monto = m });
                    }
                }
            }

            // Mostrar resultados
            txtRepVentas.Text = totalVentas.ToString("C");
            txtRepGastos.Text = totalGastos.ToString("C");
            txtRepUtilidad.Text = (totalVentas - totalGastos).ToString("C");
            dgReporte.ItemsSource = movimientos;
        }
    }
}