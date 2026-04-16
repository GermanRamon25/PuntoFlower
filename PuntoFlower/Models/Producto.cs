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
        public string Categoria { get; set; } // Flores, Accesorios, etc.

        // Atributos específicos para florería
        public string TipoVenta { get; set; } // Ramos, Arreglos, Coronas, Docenas, Medallones
        public int CantidadFlores { get; set; } // 6, 12, 24, 50, 100, etc.

        public decimal PrecioCompra { get; set; } // Lo que pagas en bodega
        public decimal PrecioVenta { get; set; }  // Precio según la lista del dueño

        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public DateTime FechaIngreso { get; set; }

        // PROPIEDAD NUEVA: Almacena el nombre del archivo de imagen en la carpeta FotosCatalogo
        public string RutaImagen { get; set; }

        // Lógica para avisar si hay que surtir
        public bool NecesitaSurtir => StockActual <= StockMinimo;
    }
}