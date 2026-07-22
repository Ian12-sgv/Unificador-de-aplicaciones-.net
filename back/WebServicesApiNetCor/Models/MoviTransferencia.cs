using System;

namespace WebServicesApiNetCor.Models
{
    public class MoviTransferencia
    {
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public string CodigoBarra { get; set; }
        public int Cantidad { get; set; }
        public decimal Valor { get; set; }
        public int NumeroCaja { get; set; }
        public int Item { get; set; }
    }
}
