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

        // =========================================================================
        // NUEVOS MÉTODOS COMPLEMENTARIOS PARA LA GESTIÓN DINÁMICA DE ENCARGADOS
        // =========================================================================

        // NUEVO: Recupera el nombre guardado para el Encargado de la Cuenta 1
        public string ObtenerEncargadoCuenta1()
        {
            string nombre = "Encargado 1";
            try
            {
                using (SqlConnection con = OpenConnection())
                {
                    string query = "SELECT Valor FROM ConfiguracionSistema WHERE Clave = 'EncargadoCuenta1'";
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
                // Respaldo por si ocurre un fallo en caliente o apagón
            }
            return nombre;
        }

        // NUEVO: Recupera el nombre guardado para el Encargado de la Cuenta 2
        public string ObtenerEncargadoCuenta2()
        {
            string nombre = "Encargado 2";
            try
            {
                using (SqlConnection con = OpenConnection())
                {
                    string query = "SELECT Valor FROM ConfiguracionSistema WHERE Clave = 'EncargadoCuenta2'";
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
                // Respaldo por si ocurre un fallo en caliente o apagón
            }
            return nombre;
        }

        // NUEVO: Guarda o actualiza de manera transaccional los tres parámetros comerciales de la sucursal
        public void GuardarDatosSucursal(string sucursal, string encargado1, string encargado2)
        {
            using (SqlConnection con = OpenConnection())
            {
                string query = @"
                    -- Nombre de la Sucursal
                    IF EXISTS (SELECT 1 FROM ConfiguracionSistema WHERE Clave = 'NombreSucursal')
                        UPDATE ConfiguracionSistema SET Valor = @sucursal WHERE Clave = 'NombreSucursal';
                    ELSE
                        INSERT INTO ConfiguracionSistema (Clave, Valor) VALUES ('NombreSucursal', @sucursal);

                    -- Encargado de Cuenta 1
                    IF EXISTS (SELECT 1 FROM ConfiguracionSistema WHERE Clave = 'EncargadoCuenta1')
                        UPDATE ConfiguracionSistema SET Valor = @e1 WHERE Clave = 'EncargadoCuenta1';
                    ELSE
                        INSERT INTO ConfiguracionSistema (Clave, Valor) VALUES ('EncargadoCuenta1', @e1);

                    -- Encargado de Cuenta 2
                    IF EXISTS (SELECT 1 FROM ConfiguracionSistema WHERE Clave = 'EncargadoCuenta2')
                        UPDATE ConfiguracionSistema SET Valor = @e2 WHERE Clave = 'EncargadoCuenta2';
                    ELSE
                        INSERT INTO ConfiguracionSistema (Clave, Valor) VALUES ('EncargadoCuenta2', @e2);";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@sucursal", sucursal.Trim());
                    cmd.Parameters.AddWithValue("@e1", encargado1.Trim());
                    cmd.Parameters.AddWithValue("@e2", encargado2.Trim());
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}