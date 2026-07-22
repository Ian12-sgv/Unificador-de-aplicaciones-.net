using System;

namespace Appinventario.Models.BorradorDev
{
    public class BorradorDevListItem
    {
        public long Numero { get; set; }
        public DateTime? Fecha { get; set; }
        public string Observacion { get; set; }
        public string Usuario { get; set; }
        public int? Status { get; set; }

        public string NumeroDisplay
        {
            get { return "#" + Numero; }
        }

        public string ResumenDisplay
        {
            get
            {
                var fecha = Fecha.HasValue ? Fecha.Value.ToString("yyyy-MM-dd") : "Sin fecha";
                var usuario = string.IsNullOrWhiteSpace(Usuario) ? "Sin usuario" : Usuario;
                var status = Status.HasValue ? Status.Value.ToString() : "-";
                var observacion = string.IsNullOrWhiteSpace(Observacion) ? "" : " | " + Observacion;

                return fecha + " | " + usuario + " | Status: " + status + observacion;
            }
        }
    }
}
