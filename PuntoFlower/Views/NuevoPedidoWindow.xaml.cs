using PuntoFlower.Data;
using System;
using System.Data.SqlClient;
using System.Windows;

namespace PuntoFlower.Views
{
    public partial class NuevoPedidoWindow : Window
    {
        public NuevoPedidoWindow()
        {
            InitializeComponent();
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validación básica
            if (string.IsNullOrEmpty(txtCliente.Text) || dpFecha.SelectedDate == null)
            {
                MessageBox.Show("Por favor, ingresa al menos el nombre del cliente y la fecha de entrega.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // Se agregó 'Descripcion' a la consulta SQL
                    string query = "INSERT INTO Pedidos (ClienteNombre, Telefono, FechaEntrega, Direccion, NotaTarjeta, Estado, Descripcion) " +
                                   "VALUES (@nom, @tel, @fec, @dir, @not, 'Pendiente', @des)";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@nom", txtCliente.Text);
                    cmd.Parameters.AddWithValue("@tel", txtTelefono.Text);
                    cmd.Parameters.AddWithValue("@fec", dpFecha.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@dir", txtDireccion.Text);
                    cmd.Parameters.AddWithValue("@not", txtNota.Text);
                    cmd.Parameters.AddWithValue("@des", txtDescripcion.Text); // Nuevo parámetro

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Pedido guardado con éxito en la agenda.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar en la base de datos: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}