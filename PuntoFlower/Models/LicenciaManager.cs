using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Security.Cryptography;

namespace PuntoFlower.Models
{
    public static class LicenciaManager
    {
        // Clave secreta para cifrar y descifrar la licencia (¡No la compartas!)
        private static readonly string ClaveSecreta = "Punt0Fl0w3r_Secret_Key_2026!";

        /// <summary>
        /// Obtiene un identificador único basado en la Tarjeta Madre del equipo.
        /// </summary>
        public static string ObtenerHWID()
        {
            string hwid = "";
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (ManagementObject mo in searcher.Get())
                {
                    hwid = mo["SerialNumber"]?.ToString().Trim();
                    break;
                }

                if (string.IsNullOrEmpty(hwid) || hwid.ToLower() == "none")
                {
                    // Respaldo por si la tarjeta madre no reporta número de serie (procesador)
                    searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        hwid = mo["ProcessorId"]?.ToString().Trim();
                        break;
                    }
                }
            }
            catch
            {
                hwid = "HWID-GENERIC-PUNTOFLOWER-ERROR";
            }

            // Sanitizar y acortar usando MD5 para que sea una cadena limpia y manejable
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(hwid);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString(); // Retorna un ID de 32 caracteres alfanuméricos
            }
        }

        /// <summary>
        /// Algoritmo para generar o validar la licencia combinando el HWID y la Clave Secreta.
        /// </summary>
        public static string GenerarLicencia(string hwid)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string mezcla = hwid + ClaveSecreta;
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(mezcla));
                return Convert.ToBase64String(hashBytes).Replace("=", "").Replace("/", "").Replace("+", "").Substring(0, 24);
            }
        }
    }
}
