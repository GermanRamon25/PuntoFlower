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
        // Usamos el nombre exacto que pusiste en tu App.config
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
                // Si hay un error de conexión, nos avisará aquí
                throw new Exception("Error al conectar a SQL Server: " + ex.Message);
            }
        }
    }
}