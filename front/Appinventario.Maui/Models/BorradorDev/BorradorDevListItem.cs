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

        public string FechaDisplay
        {
            get { return Fecha.HasValue ? Fecha.Value.ToString("yyyy-MM-dd") : "Sin fecha"; }
        }

        public string UsuarioDisplay
        {
            get { return string.IsNullOrWhiteSpace(Usuario) ? "Sin usuario" : Usuario; }
        }

        public string StatusDisplay
        {
            get { return "Status: " + (Status.HasValue ? Status.Value.ToString() : "-"); }
        }

        public string ObservacionDisplay
        {
            get { return string.IsNullOrWhiteSpace(Observacion) ? "Sin observacion" : Observacion; }
        }

        public string ResumenDisplay
        {
            get
            {
                var observacion = string.IsNullOrWhiteSpace(Observacion) ? "" : " | " + Observacion;

                return FechaDisplay + " | " + UsuarioDisplay + " | " + StatusDisplay + observacion;
            }
        }
    }
}

