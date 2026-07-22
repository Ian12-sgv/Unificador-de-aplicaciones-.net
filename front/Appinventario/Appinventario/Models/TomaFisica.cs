using System;
using System.Collections.Generic;
using System.Text;

namespace Appinventario.Models
{
    public class TomaFisica
    {
        public int? NumeroPagina { get; set; }
        public DateTime Fecha { get; set; }
        public string Observacion { get; set; }
        public string Referencia { get; set; }
        public int Cantidad { get; set; }
        public string Usuario { get; set; }
        public int Status { get; set; }
        public string CodigoBarra { get; set; }
        public string Prenda { get; set; }
        public string Id { get; set; }
        public string Text { get; set; }
        public string Nombre { get; set; }
        public bool Valor { get; set; }
        public string Imei { get; set; }
    }
}
