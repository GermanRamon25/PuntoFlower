using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuntoFlower.Data
{
    public static class Session
    {
        public static string UsuarioActual { get; set; }
        public static string RolActual { get; set; } // Clave para los permisos (Admin o Empleado)

        // NUEVO: La llave de seguridad que guarda a qué florería pertenece este usuario
        public static string SucursalAsignada { get; set; }

        public static void CerrarSesion()
        {
            UsuarioActual = null;
            RolActual = null;
            SucursalAsignada = null;
        }
    }
}