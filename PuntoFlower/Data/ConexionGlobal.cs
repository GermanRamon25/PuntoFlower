using System;
using System.Configuration;

namespace PuntoFlower.Data
{
    public static class ConexionGlobal
    {
        // Única cadena de conexión para todo el sistema (apunta siempre a PuntoFlowerDB en Azure)
        public static string CadenaConexionActiva = ConfigurationManager.ConnectionStrings["PuntoFlowerDB"].ConnectionString;

        // EL CORAZÓN DEL SISTEMA MULTI-SUCURSAL:
        // Esta variable filtrará absolutamente todos los SELECT, INSERT, UPDATE y DELETE del sistema.
        // Se actualizará automáticamente en la pantalla de Login dependiendo de quién inicie sesión.
        public static string SucursalActual = "Guasave";
    }
}