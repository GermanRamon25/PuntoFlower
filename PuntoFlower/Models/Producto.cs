using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuntoFlower.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; } // Ejemplo: Rosa, Tulipán, Follaje, Base

        // Lo que nos cuesta a nosotros (Gasto)
        public decimal PrecioCompra { get; set; }

        // En cuánto lo damos al público (Ingreso)
        public decimal PrecioVenta { get; set; }

        // Gestión de Stock
        public int StockActual { get; set; }
        public int StockMinimo { get; set; } // Si el stock baja de aquí, el sistema avisará

        public DateTime FechaIngreso { get; set; }

        // Propiedad calculada para saber si necesitamos surtir
        public bool NecesitaSurtir => StockActual <= StockMinimo;
    }
}