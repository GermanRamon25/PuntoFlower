using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class AdminExpensesView : UserControl
    {
        public AdminExpensesView()
        {
            InitializeComponent();
            cmbMetodoAdmin.SelectedIndex = 0;

            dpInicioAdmin.SelectedDate = DateTime.Now.AddDays(-7);
            dpFinAdmin.SelectedDate = DateTime.Now;

            CargarDatosModulo();
        }

        private void CargarDatosModulo()
        {
            if (dpInicioAdmin.SelectedDate == null || dpFinAdmin.SelectedDate == null) return;

            ConexionDB db = new ConexionDB();

            DateTime inicioBusqueda = dpInicioAdmin.SelectedDate.Value.Date;
            DateTime finBusqueda = dpFinAdmin.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);

            decimal totalIngresosCombinados = 0;
            decimal totalGastosEmpleado = 0;
            decimal totalGastosAdminEfectivo = 0;
            List<object> listaGastosAdmin = new List<object>();

            using (SqlConnection con = db.OpenConnection())
            {
                // BLINDAJE: Leer los ingresos solo de la sucursal activa
                string qVentas = "SELECT SUM(Total) FROM Ventas WHERE Fecha BETWEEN @i AND @f AND Sucursal = @s";
                using (SqlCommand cmd = new SqlCommand(qVentas, con))
                {
                    cmd.Parameters.AddWithValue("@i", inicioBusqueda);
                    cmd.Parameters.AddWithValue("@f", finBusqueda);
                    cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                    var res = cmd.ExecuteScalar();
                    if (res != DBNull.Value && res != null) totalIngresosCombinados = Convert.ToDecimal(res);
                }

                // BLINDAJE: Leer los gastos operativos del empleado solo de la sucursal activa
                string qGastosEmp = "SELECT SUM(Monto) FROM Gastos WHERE RegistradoPor = 'Empleado' AND Fecha BETWEEN @i AND @f AND Sucursal = @s";
                using (SqlCommand cmd = new SqlCommand(qGastosEmp, con))
                {
                    cmd.Parameters.AddWithValue("@i", inicioBusqueda);
                    cmd.Parameters.AddWithValue("@f", finBusqueda);
                    cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                    var res = cmd.ExecuteScalar();
                    if (res != DBNull.Value && res != null) totalGastosEmpleado = Convert.ToDecimal(res);
                }

                // BLINDAJE: Calcular los retiros en efectivo que hiciste solo en esta sucursal
                string qGastosAdminEf = "SELECT SUM(Monto) FROM Gastos WHERE RegistradoPor = 'Admin' AND (MetodoPago = 'Efectivo de Caja' OR MetodoPago = 'Efectivo') AND Fecha BETWEEN @i AND @f AND Sucursal = @s";
                using (SqlCommand cmd = new SqlCommand(qGastosAdminEf, con))
                {
                    cmd.Parameters.AddWithValue("@i", inicioBusqueda);
                    cmd.Parameters.AddWithValue("@f", finBusqueda);
                    cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                    var res = cmd.ExecuteScalar();
                    if (res != DBNull.Value && res != null) totalGastosAdminEfectivo = Convert.ToDecimal(res);
                }

                // BLINDAJE: Historial de egresos para la tabla (solo los de esta sucursal)
                string qHistorialAdmin = "SELECT Fecha, Descripcion, MetodoPago, Monto FROM Gastos WHERE RegistradoPor = 'Admin' AND Fecha BETWEEN @i AND @f AND Sucursal = @s ORDER BY Fecha DESC";
                using (SqlCommand cmd = new SqlCommand(qHistorialAdmin, con))
                {
                    cmd.Parameters.AddWithValue("@i", inicioBusqueda);
                    cmd.Parameters.AddWithValue("@f", finBusqueda);
                    cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            listaGastosAdmin.Add(new
                            {
                                Fecha = r["Fecha"],
                                Descripcion = r["Descripcion"].ToString(),
                                MetodoPago = r["MetodoPago"] != DBNull.Value ? r["MetodoPago"].ToString() : "Efectivo de Caja",
                                Monto = Convert.ToDecimal(r["Monto"])
                            });
                        }
                    }
                }
            }

            decimal dineroDisponibleReal = totalIngresosCombinados - totalGastosEmpleado - totalGastosAdminEfectivo;

            txtDineroDisponible.Text = dineroDisponibleReal.ToString("C");
            dgEgresosAdmin.ItemsSource = listaGastosAdmin;
        }

        private void btnConsultar_Click(object sender, RoutedEventArgs e)
        {
            CargarDatosModulo();
        }

        private void btnGuardarGastoAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtConceptoAdmin.Text) || !decimal.TryParse(txtMontoAdmin.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Por favor ingresa un concepto y un monto numérico válido mayor a cero.", "Campos obligatorios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // BLINDAJE COMPLETO: Registrar el gasto guardando la hora local de tu computadora y sellándolo a la sucursal conectada
                    string queryGasto = "INSERT INTO Gastos (Descripcion, Monto, Fecha, Categoria, MetodoPago, RegistradoPor, Sucursal) VALUES (@desc, @monto, @fechaLocal, @cat, @metodo, 'Admin', @s)";
                    using (SqlCommand cmd = new SqlCommand(queryGasto, con))
                    {
                        cmd.Parameters.AddWithValue("@desc", txtConceptoAdmin.Text.Trim());
                        cmd.Parameters.AddWithValue("@monto", monto);
                        cmd.Parameters.AddWithValue("@fechaLocal", DateTime.Now);
                        cmd.Parameters.AddWithValue("@cat", "Administración / Surtido");
                        cmd.Parameters.AddWithValue("@metodo", ((ComboBoxItem)cmbMetodoAdmin.SelectedItem).Content.ToString());
                        cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);

                        cmd.ExecuteNonQuery();
                    }
                }

                txtConceptoAdmin.Clear();
                txtMontoAdmin.Clear();
                CargarDatosModulo();
                MessageBox.Show($"Gasto de administración registrado con éxito y amarrado correctamente a {ConexionGlobal.SucursalActual}.", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar el egreso administrativo: " + ex.Message, "Error");
            }
        }
    }
}