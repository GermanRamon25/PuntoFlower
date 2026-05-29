using System;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using PuntoFlower.Data;

namespace PuntoFlower.Views
{
    public partial class CambiarPasswordWindow : Window
    {
        public CambiarPasswordWindow()
        {
            InitializeComponent();

            // Al abrir la ventana, el campo "Usuario Actual" se llena automáticamente
            txtUsuarioActual.Text = Session.UsuarioActual;
            // Dejamos el campo del "Nuevo Usuario" listo para que escribas el cambio
            txtNuevoUsuario.Text = Session.UsuarioActual;
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Captura de datos de la interfaz
            string usuarioActualInput = txtUsuarioActual.Text.Trim().ToLower();
            string nuevoUsuario = txtNuevoUsuario.Text.Trim().ToLower();
            string passActual = txtPasswordActual.Password.Trim();
            string passNueva = txtPasswordNueva.Password.Trim();
            string passConfirmar = txtPasswordConfirmar.Password.Trim();

            // 1. Validar que ningún campo quede vacío
            if (string.IsNullOrEmpty(usuarioActualInput) || string.IsNullOrEmpty(nuevoUsuario) ||
                string.IsNullOrEmpty(passActual) || string.IsNullOrEmpty(passNueva) || string.IsNullOrEmpty(passConfirmar))
            {
                MessageBox.Show("Por favor, rellena todos los campos de seguridad.", "Campos Vacíos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Validar que las nuevas contraseñas coincidan
            if (passNueva != passConfirmar)
            {
                MessageBox.Show("La nueva contraseña y su confirmación no coinciden. Verifica los datos.", "Error de Coincidencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConexionDB db = new ConexionDB();
            try
            {
                using (SqlConnection con = db.OpenConnection())
                {
                    // 3. AUTENTICACIÓN: Verificar que el Usuario Actual y la Contraseña Actual sean correctos
                    string queryCheck = "SELECT COUNT(1) FROM Usuarios WHERE Username = @userActual AND PasswordHash = @passActual";
                    using (SqlCommand cmdCheck = new SqlCommand(queryCheck, con))
                    {
                        cmdCheck.Parameters.AddWithValue("@userActual", Session.UsuarioActual.ToLower());
                        cmdCheck.Parameters.AddWithValue("@passActual", passActual);

                        int credencialesCorrectas = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        if (credencialesCorrectas == 0)
                        {
                            MessageBox.Show("El nombre de usuario actual o la contraseña actual son incorrectos.", "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    // 4. DISPONIBILIDAD: Si decidió cambiar el nombre de usuario, validamos que el nuevo no esté repetido en el sistema
                    if (nuevoUsuario != Session.UsuarioActual.ToLower())
                    {
                        string queryDispo = "SELECT COUNT(1) FROM Usuarios WHERE Username = @nuevoUser";
                        using (SqlCommand cmdDispo = new SqlCommand(queryDispo, con))
                        {
                            cmdDispo.Parameters.AddWithValue("@nuevoUser", nuevoUsuario);
                            int yaExiste = Convert.ToInt32(cmdDispo.ExecuteScalar());

                            if (yaExiste > 0)
                            {
                                MessageBox.Show("El nuevo nombre de usuario ya está ocupado por otra cuenta. Elige uno diferente.", "Usuario No Disponible", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    // 5. ACTUALIZACIÓN ACTUAL: Modificamos tanto el Nombre de Usuario como la Contraseña de una sola vez
                    string queryUpdate = "UPDATE Usuarios SET Username = @nuevoUser, PasswordHash = @nuevaPass WHERE Username = @userActual";
                    using (SqlCommand cmdUp = new SqlCommand(queryUpdate, con))
                    {
                        cmdUp.Parameters.AddWithValue("@nuevoUser", nuevoUsuario);
                        cmdUp.Parameters.AddWithValue("@nuevaPass", passNueva);
                        cmdUp.Parameters.AddWithValue("@userActual", Session.UsuarioActual.ToLower()); // Filtramos usando el usuario con el que se abrió la sesión
                        cmdUp.ExecuteNonQuery();
                    }
                }

                // 6. Sincronizamos la sesión global de PuntoFlower para que los cambios se apliquen de inmediato en el sistema
                Session.UsuarioActual = nuevoUsuario;

                MessageBox.Show("Tus credenciales administrativas se han actualizado con éxito.\n\nA partir de tu siguiente inicio de sesión, usa tus nuevos datos.", "Seguridad Actualizada", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error crítico al actualizar los datos en el servidor local: " + ex.Message, "Error de Servidor", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}