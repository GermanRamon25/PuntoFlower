using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PuntoFlower.Data;
using PuntoFlower.Models;

namespace PuntoFlower.Views
{
    public partial class CatalogView : UserControl
    {
        private readonly int[] capacidadesRamos = { 6, 12, 24, 36, 50, 72, 100, 150, 200, 250 };

        public CatalogView()
        {
            InitializeComponent();
            CargarDatos();
            GenerarTarjetasRamosMayoreo();
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
            catch (Exception ex) { MessageBox.Show("Error al cargar flores: " + ex.Message); }
        }

        private void GenerarTarjetasRamosMayoreo()
        {
            wpRamosMayoreo.Children.Clear();
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FotosCatalogo");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            foreach (int cap in capacidadesRamos)
            {
                // 1. Contenedor principal de la tarjeta
                Border card = new Border
                {
                    Width = 180,
                    Height = 270,
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(10),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(235, 237, 239)),
                    BorderThickness = new Thickness(1), // Corregido
                    Margin = new Thickness(10) // Corregido
                };

                StackPanel stack = new StackPanel();

                // 2. Contenedor de la Imagen
                Border imgBorder = new Border
                {
                    Height = 140,
                    Background = new SolidColorBrush(Color.FromRgb(242, 243, 244)),
                    CornerRadius = new CornerRadius(10, 10, 0, 0),
                    ClipToBounds = true
                };

                Image img = new Image
                {
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5) // Corregido
                };

                string fotoPath = Path.Combine(folderPath, $"ramo{cap}.jpeg");
                if (File.Exists(fotoPath))
                {
                    try
                    {
                        BitmapImage bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(fotoPath);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        img.Source = bmp;
                    }
                    catch { }
                }

                imgBorder.Child = img;

                // 3. Título del Ramo
                TextBlock txt = new TextBlock
                {
                    Text = $"Ramo {cap} pz",
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 5),
                    Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
                };

                // 4. Botones CRUD
                DockPanel dp = new DockPanel { Margin = new Thickness(10, 5, 10, 10) };

                Button btnDel = new Button
                {
                    Content = "🗑",
                    Width = 35,
                    Height = 30,
                    Background = Brushes.Firebrick,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    Tag = cap,
                    Margin = new Thickness(0, 0, 5, 0),
                    BorderThickness = new Thickness(0), // Corregido
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                btnDel.Click += (s, e) => EliminarFotoRamo((int)((Button)s).Tag);

                Button btnUp = new Button
                {
                    Content = "Subir Foto",
                    Height = 30,
                    Background = new SolidColorBrush(Color.FromRgb(169, 50, 38)),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    Tag = cap,
                    BorderThickness = new Thickness(0), // Corregido
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                btnUp.Click += BtnSubirFotoRamo_Click;

                dp.Children.Add(btnDel);
                dp.Children.Add(btnUp);

                stack.Children.Add(imgBorder);
                stack.Children.Add(txt);
                stack.Children.Add(dp);
                card.Child = stack;

                wpRamosMayoreo.Children.Add(card);
            }
        }

        private void EliminarFotoRamo(int cap)
        {
            if (MessageBox.Show($"¿Deseas eliminar la foto de referencia para el ramo de {cap} piezas?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FotosCatalogo", $"ramo{cap}.jpeg");
                    if (File.Exists(path)) File.Delete(path);
                    GenerarTarjetasRamosMayoreo();
                }
                catch (Exception ex) { MessageBox.Show("Error al eliminar archivo: " + ex.Message); }
            }
        }

        private void btnEliminarFoto_Click(object sender, RoutedEventArgs e)
        {
            var prod = (sender as Button).Tag as Producto;
            if (prod == null || string.IsNullOrEmpty(prod.RutaImagen)) return;

            if (MessageBox.Show($"¿Eliminar la foto de {prod.Nombre}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FotosCatalogo", prod.RutaImagen);
                    if (File.Exists(fullPath)) File.Delete(fullPath);

                    ConexionDB db = new ConexionDB();
                    using (SqlConnection con = db.OpenConnection())
                    {
                        SqlCommand cmd = new SqlCommand("UPDATE Productos SET RutaImagen = '' WHERE Id = @id", con);
                        cmd.Parameters.AddWithValue("@id", prod.Id);
                        cmd.ExecuteNonQuery();
                    }
                    CargarDatos();
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void BtnSubirFotoRamo_Click(object sender, RoutedEventArgs e)
        {
            int piezas = (int)((Button)sender).Tag;
            OpenFileDialog open = new OpenFileDialog { Filter = "Imágenes|*.jpg;*.jpeg;*.png" };
            if (open.ShowDialog() == true)
            {
                try
                {
                    string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FotosCatalogo");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    File.Copy(open.FileName, Path.Combine(folder, $"ramo{piezas}.jpeg"), true);
                    GenerarTarjetasRamosMayoreo();
                }
                catch (Exception ex) { MessageBox.Show("Error al subir imagen: " + ex.Message); }
            }
        }

        private void btnSubirFoto_Click(object sender, RoutedEventArgs e)
        {
            var prod = (sender as Button).Tag as Producto;
            if (prod == null) return;

            OpenFileDialog open = new OpenFileDialog { Filter = "Imágenes|*.jpg;*.jpeg;*.png" };
            if (open.ShowDialog() == true)
            {
                try
                {
                    string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FotosCatalogo");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    string extension = Path.GetExtension(open.FileName);
                    string name = $"prod_{prod.Id}_{DateTime.Now.Ticks}{extension}";
                    File.Copy(open.FileName, Path.Combine(folder, name), true);

                    ConexionDB db = new ConexionDB();
                    using (SqlConnection con = db.OpenConnection())
                    {
                        SqlCommand cmd = new SqlCommand("UPDATE Productos SET RutaImagen = @img WHERE Id = @id", con);
                        cmd.Parameters.AddWithValue("@img", name);
                        cmd.Parameters.AddWithValue("@id", prod.Id);
                        cmd.ExecuteNonQuery();
                    }
                    CargarDatos();
                }
                catch (Exception ex) { MessageBox.Show("Error al actualizar foto: " + ex.Message); }
            }
        }
    }
}