using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace PuntoFlower.Data
{
    public class ConexionDB
    {
        private readonly string _cadenaSQL = ConfigurationManager.ConnectionStrings["PuntoFlowerDBConnection"].ConnectionString;

        public SqlConnection OpenConnection()
        {
            try
            {
                SqlConnection conexion = new SqlConnection(_cadenaSQL);
                conexion.Open();
                return conexion;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al conectar a SQL Server: " + ex.Message);
            }
        }

        public string ObtenerNombreSucursal()
        {
            string nombre = "Sucursal Local";
            try
            {
                using (SqlConnection con = OpenConnection())
                {
                    string query = "SELECT Valor FROM ConfiguracionSistema WHERE Clave = 'NombreSucursal'";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        object resultado = cmd.ExecuteScalar();
                        if (resultado != null)
                        {
                            nombre = resultado.ToString();
                        }
                    }
                }
            }
            catch
            {
                // Evita que el sistema falle si la tabla aún no tiene registros o no está creada
            }
            return nombre;
        }

        // NUEVO: Método para guardar o actualizar manualmente desde la pantalla de configuración
        public void GuardarNombreSucursal(string nuevoNombre)
        {
            using (SqlConnection con = OpenConnection())
            {
                string query = @"
                    IF EXISTS (SELECT 1 FROM ConfiguracionSistema WHERE Clave = 'NombreSucursal')
                        UPDATE ConfiguracionSistema SET Valor = @v WHERE Clave = 'NombreSucursal';
                    ELSE
                        INSERT INTO ConfiguracionSistema (Clave, Valor) VALUES ('NombreSucursal', @v);";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@v", nuevoNombre);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}