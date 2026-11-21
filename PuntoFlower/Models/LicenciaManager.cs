using System;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace PuntoFlower.Models
{
    public static class LicenciaManager
    {
        // Clave secreta para cifrar y descifrar la licencia (¡No la compartas!)
        private static readonly string ClaveSecreta = "Punt0Fl0w3r_Secret_Key_2026!";

        // Caché en memoria durante la sesión y archivo fijo en ProgramData (sobrevive a actualizaciones ClickOnce)
        private static string _hwidCacheEnMemoria = null;
        private static readonly string CarpetaLicenciaLocal = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "PuntoFlower");
        private static readonly string RutaArchivoLicenciaLocal = Path.Combine(CarpetaLicenciaLocal, "pf_act.lic");

        /// <summary>
        /// Obtiene un identificador único basado en la Tarjeta Madre del equipo con reintentos ante demoras de WMI.
        /// </summary>
        public static string ObtenerHWID()
        {
            if (!string.IsNullOrEmpty(_hwidCacheEnMemoria))
                return _hwidCacheEnMemoria;

            string hwidCrudo = "";

            // Reintentar hasta 3 veces si WMI tarda en responder al arrancar Windows
            for (int intento = 1; intento <= 3; intento++)
            {
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
                    {
                        foreach (ManagementObject mo in searcher.Get())
                        {
                            hwidCrudo = mo["SerialNumber"]?.ToString().Trim();
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(hwidCrudo) ||
                        hwidCrudo.Equals("none", StringComparison.OrdinalIgnoreCase) ||
                        hwidCrudo.Equals("to be filled by o.e.m.", StringComparison.OrdinalIgnoreCase) ||
                        hwidCrudo.Equals("default string", StringComparison.OrdinalIgnoreCase))
                    {
                        using (ManagementObjectSearcher searcherCpu = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                        {
                            foreach (ManagementObject mo in searcherCpu.Get())
                            {
                                hwidCrudo = mo["ProcessorId"]?.ToString().Trim();
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(hwidCrudo))
                        break;
                }
                catch
                {
                    if (intento < 3)
                        Thread.Sleep(350);
                }
            }

            if (string.IsNullOrEmpty(hwidCrudo))
                hwidCrudo = "HWID-GENERIC-PUNTOFLOWER-ERROR";

            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(hwidCrudo);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }

                string resultado = sb.ToString();

                // Solo cacheamos en memoria si se obtuvo un HWID real (no el de error)
                if (hwidCrudo != "HWID-GENERIC-PUNTOFLOWER-ERROR")
                    _hwidCacheEnMemoria = resultado;

                return resultado;
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

                return Convert.ToBase64String(hashBytes)
                              .Replace("=", "")
                              .Replace("/", "")
                              .Replace("+", "")
                              .Substring(0, 24)
                              .ToUpper();
            }
        }

        /// <summary>
        /// Guarda un respaldo local cifrado en C:\ProgramData\PuntoFlower para resistir micro-cortes de Azure SQL y actualizaciones ClickOnce.
        /// </summary>
        public static void GuardarRespaldoLocal(string claveLicencia)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(claveLicencia)) return;
                string hwidActual = ObtenerHWID();
                string licenciaEsperada = GenerarLicencia(hwidActual);

                if (!claveLicencia.Trim().Equals(licenciaEsperada, StringComparison.OrdinalIgnoreCase))
                    return;

                if (!Directory.Exists(CarpetaLicenciaLocal))
                    Directory.CreateDirectory(CarpetaLicenciaLocal);

                File.WriteAllText(RutaArchivoLicenciaLocal, licenciaEsperada, Encoding.UTF8);
            }
            catch { }
        }

        /// <summary>
        /// Verifica si el equipo ya tiene una licencia válida guardada localmente en ProgramData.
        /// </summary>
        public static bool ValidarRespaldoLocal()
        {
            try
            {
                if (!File.Exists(RutaArchivoLicenciaLocal))
                    return false;

                string claveGuardada = File.ReadAllText(RutaArchivoLicenciaLocal, Encoding.UTF8).Trim();
                string hwidActual = ObtenerHWID();
                string licenciaEsperada = GenerarLicencia(hwidActual);

                return claveGuardada.Equals(licenciaEsperada, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}