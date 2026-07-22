using SQLite;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Appinventario.Models
{
     public class Transferencia
    {
        public int Numero{ get; set; }
        public DateTime Fecha { get; set; }
        public string CodigoEnvia { get; set; }
        public string CodigoRecibe { get; set; }
        public string DocumentoOrigen {  get; set; }
        public int TipoValor { get; set; }
        public decimal TotalValor { get; set; }
        public string Observacion { get; set; }
        public int Status { get; set; }
        public String Usuario {  get; set; }
        public Boolean InterContable { get; set; }
        public DateTime FechaEmision  { get; set; }
        public int IdLote { get; set; }
        public int IdDespacho { get; set; }
        public Boolean Correccion { get; set; }
        public List<MovTransferencias> Detalles { get; set; }

    }
    
}
