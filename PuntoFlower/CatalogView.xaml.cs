using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PuntoFlower.Data; // Asegúrate de que tu clase de conexión esté aquí
using PuntoFlower.Models; // Asegúrate de que tu clase Producto esté aquí

namespace PuntoFlower.Views
{
    public partial class CatalogView : UserControl
    {
        public CatalogView()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void CargarDatos()
        {
            List<Producto> lista = new List<Producto>();
            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    string query = "SELECT Id, Nombre, PrecioVenta, RutaImagen FROM Productos";
                    SqlCommand cmd = new SqlCommand(query, con);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            lista.Add(new Producto
                            {
                                Id = (int)r["Id"],
                                Nombre = r["Nombre"].ToString(),
                                PrecioVenta = (decimal)r["PrecioVenta"],
                                RutaImagen = r.IsDBNull(3) ? "" : r["RutaImagen"].ToString()
                            });
                        }
                    }
                }
                icProductos.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar catálogo: " + ex.Message);
            }
        }

        // ESTE ES EL MÉTODO QUE TE DABA ERROR
        private void btnSubirFoto_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            var producto = boton.Tag as Producto;

            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Imágenes (*.jpg; *.png)|*.jpg;*.png";

            if (open.ShowDialog() == true)
            {
                try
                {
                    string carpetaFotos = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FotosCatalogo");
                    if (!Directory.Exists(carpetaFotos)) Directory.CreateDirectory(carpetaFotos);

                    string nombreImagen = $"prod_{producto.Id}_{DateTime.Now.Ticks}{Path.GetExtension(open.FileName)}";
                    string destino = Path.Combine(carpetaFotos, nombreImagen);

                    File.Copy(open.FileName, destino, true);

                    // Actualizar base de datos
                    ConexionDB db = new ConexionDB();
                    using (SqlConnection con = db.OpenConnection())
                    {
                        string query = "UPDATE Productos SET RutaImagen = @img WHERE Id = @id";
                        SqlCommand cmd = new SqlCommand(query, con);
                        cmd.Parameters.AddWithValue("@img", nombreImagen);
                        cmd.Parameters.AddWithValue("@id", producto.Id);
                        cmd.ExecuteNonQuery();
                    }

                    CargarDatos(); // Refrescar la galería
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al guardar imagen: " + ex.Message);
                }
            }
        }
    }
}