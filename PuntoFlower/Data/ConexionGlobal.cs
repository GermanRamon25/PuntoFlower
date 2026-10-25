using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace PuntoFlower.Data
{
    public static class ConexionGlobal
    {
        // Al arrancar, lee lo que está en el App.config de la computadora física
        public static string CadenaConexionActiva = ConfigurationManager.ConnectionStrings["PuntoFlowerDB"].ConnectionString;

        public static string SucursalActual = "Matriz (Pruebas)";
    }
}
