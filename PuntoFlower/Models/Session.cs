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
        public static string RolActual { get; set; } // Esta es la clave para los permisos

        public static void CerrarSesion()
        {
            UsuarioActual = null;
            RolActual = null;
        }
    }
}