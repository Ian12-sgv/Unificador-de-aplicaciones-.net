using System;

namespace WebServicesApiNetCor.Models
{
    public class TransferenciaHistorial : Transferencia
    {
        public string SucursalNombre { get; set; }
        public int Referencias { get; set; }
        public int Unidades { get; set; }
    }
}
