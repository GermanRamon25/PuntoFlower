using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuntoFlower.Models
{
    public class Gasto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } // Ejemplo: "Pago a Bodega", "Sueldo Empleada"
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string Categoria { get; set; } // Operativo, Insumos, Limpieza
    }
}
 
