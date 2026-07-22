using System;

namespace Appinventario.Models.BorradorDev
{
    public class BorradorDevHeader
    {
        public long Numero { get; set; }
        public DateTime? Fecha { get; set; }
        public string Observacion { get; set; }
        public string Usuario { get; set; }
        public int? Status { get; set; }
    }
}
