using System.Collections.Generic;

namespace WebServicesApiNetCor.Models
{
    public class TransferenciaRegistro : Transferencia
    {
        public IEnumerable<MoviTransferencia> Detalles { get; set; }
    }
}
