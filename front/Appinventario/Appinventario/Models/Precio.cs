using System;
using System.Collections.Generic;
using System.Text;
//Creado por angel
namespace Appinventario.Models
{
    public class Precio
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public decimal Existencia { get; set; }
        public decimal PrecioDetal { get; set; }
        public decimal PrecioMayor { get; set; }
        public decimal PrecioAfiliado { get; set; }
        public decimal PrecioPromocion { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal TasaImpuesto { get; set; }
        public decimal ImpuestoMonto { get; set; }
        public string Url { get; set; }
        public string CodigoBarra { get; set; }
        public string nombre { get; set; }
        public decimal precio { get; set; }
        public decimal PrecioDolaresDetal { get; set; }
        public decimal PrecioDolaresMayor { get; set; }
        public decimal TasaDolares { get; set; }


    }
}
