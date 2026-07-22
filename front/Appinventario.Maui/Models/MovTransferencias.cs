using System;
using System.Collections.Generic;
using System.Text;

namespace Appinventario.Models
{
    public class MovTransferencias
    {
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public string CodigoBarra { get; set; }
        public int Cantidad { get; set; } 
        public decimal Valor { get; set; }
        public int NumeroCaja { get; set; }
        public int Item { get; set; }
        public decimal UltimoCosto { get; set; }
        public decimal CostoInicial { get; set; }
        public decimal CostoDolar { get; set; }

    }
}

