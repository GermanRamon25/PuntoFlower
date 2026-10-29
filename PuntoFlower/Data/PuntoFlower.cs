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
        public SqlConnection OpenConnection()
        {
            try
            {
                // Lee directamente la cadena global hacia Azure
                SqlConnection conexion = new SqlConnection(ConexionGlobal.CadenaConexionActiva);
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
                    // FILTRO MULTI-SUCURSAL APLICADO
                    string query = "SELECT Valor FROM ConfiguracionSistema WHERE Clave = 'NombreSucursal' AND Sucursal = @sucursal";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@sucursal", ConexionGlobal.SucursalActual);
                        object resultado = cmd.ExecuteScalar();
                        if (resultado != null)
                        {
                            nombre = resultado.ToString();
                        }
                    }
                }
            }
            catch { }
            return nombre;
        }

        public void GuardarNombreSucursal(string nuevoNombre)
        {
            using (SqlConnection con = OpenConnection())
            {
                string query = @"
                    IF EXISTS (SELECT 1 FROM ConfiguracionSistema WHERE Clave = 'NombreSucursal' AND Sucursal = @s)
                        UPDATE ConfiguracionSistema SET Valor = @v WHERE Clave = 'NombreSucursal' AND Sucursal = @s;
                    ELSE
                        INSERT INTO ConfiguracionSistema (Clave, Valor, Sucursal) VALUES ('NombreSucursal', @v, @s);";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@v", nuevoNombre);
                    cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public string ObtenerEncargadoCuenta1()
        {
            string nombre = "Encargado 1";
            try
            {
                using (SqlConnection con = OpenConnection())
                {
                    string query = "SELECT Valor FROM ConfiguracionSistema WHERE Clave = 'EncargadoCuenta1' AND Sucursal = @sucursal";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@sucursal", ConexionGlobal.SucursalActual);
                        object resultado = cmd.ExecuteScalar();
                        if (resultado != null)
                        {
                            nombre = resultado.ToString();
                        }
                    }
                }
            }
            catch { }
            return nombre;
        }

        public string ObtenerEncargadoCuenta2()
        {
            string nombre = "Encargado 2";
            try
            {
                using (SqlConnection con = OpenConnection())
                {
                    string query = "SELECT Valor FROM ConfiguracionSistema WHERE Clave = 'EncargadoCuenta2' AND Sucursal = @sucursal";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@sucursal", ConexionGlobal.SucursalActual);
                        object resultado = cmd.ExecuteScalar();
                        if (resultado != null)
                        {
                            nombre = resultado.ToString();
                        }
                    }
                }
            }
            catch { }
            return nombre;
        }

        public void GuardarDatosSucursal(string nombreMostrar, string encargado1, string encargado2)
        {
            using (SqlConnection con = OpenConnection())
            {
                string query = @"
                    -- Nombre de la Sucursal
                    IF EXISTS (SELECT 1 FROM ConfiguracionSistema WHERE Clave = 'NombreSucursal' AND Sucursal = @s)
                        UPDATE ConfiguracionSistema SET Valor = @sucursal WHERE Clave = 'NombreSucursal' AND Sucursal = @s;
                    ELSE
                        INSERT INTO ConfiguracionSistema (Clave, Valor, Sucursal) VALUES ('NombreSucursal', @sucursal, @s);

                    -- Encargado de Cuenta 1
                    IF EXISTS (SELECT 1 FROM ConfiguracionSistema WHERE Clave = 'EncargadoCuenta1' AND Sucursal = @s)
                        UPDATE ConfiguracionSistema SET Valor = @e1 WHERE Clave = 'EncargadoCuenta1' AND Sucursal = @s;
                    ELSE
                        INSERT INTO ConfiguracionSistema (Clave, Valor, Sucursal) VALUES ('EncargadoCuenta1', @e1, @s);

                    -- Encargado de Cuenta 2
                    IF EXISTS (SELECT 1 FROM ConfiguracionSistema WHERE Clave = 'EncargadoCuenta2' AND Sucursal = @s)
                        UPDATE ConfiguracionSistema SET Valor = @e2 WHERE Clave = 'EncargadoCuenta2' AND Sucursal = @s;
                    ELSE
                        INSERT INTO ConfiguracionSistema (Clave, Valor, Sucursal) VALUES ('EncargadoCuenta2', @e2, @s);";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@sucursal", nombreMostrar.Trim());
                    cmd.Parameters.AddWithValue("@e1", encargado1.Trim());
                    cmd.Parameters.AddWithValue("@e2", encargado2.Trim());
                    cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual); // Llave de aislamiento
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public decimal ObtenerFondoCaja()
        {
            decimal fondo = 0;
            try
            {
                using (SqlConnection con = OpenConnection())
                {
                    string query = "SELECT Valor FROM ConfiguracionSistema WHERE Clave = 'FondoCajaUltimo' AND Sucursal = @sucursal";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@sucursal", ConexionGlobal.SucursalActual);
                        object res = cmd.ExecuteScalar();
                        if (res != null)
                        {
                            decimal.TryParse(res.ToString(), out fondo);
                        }
                    }
                }
            }
            catch { }
            return fondo;
        }

        public void GuardarFondoCaja(decimal monto)
        {
            try
            {
                using (SqlConnection con = OpenConnection())
                {
                    string query = @"
                        IF EXISTS (SELECT 1 FROM ConfiguracionSistema WHERE Clave = 'FondoCajaUltimo' AND Sucursal = @s)
                            UPDATE ConfiguracionSistema SET Valor = @v WHERE Clave = 'FondoCajaUltimo' AND Sucursal = @s;
                        ELSE
                            INSERT INTO ConfiguracionSistema (Clave, Valor, Sucursal) VALUES ('FondoCajaUltimo', @v, @s);";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@v", monto.ToString("F2"));
                        cmd.Parameters.AddWithValue("@s", ConexionGlobal.SucursalActual);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }
    }
}