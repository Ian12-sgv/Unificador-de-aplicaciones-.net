using System.Collections.Generic;

namespace Appinventario.Models.BorradorDev
{
    public class BorradorDevListResponse
    {
        public bool Ok { get; set; }
        public List<BorradorDevListItem> Items { get; set; }
    }

    public class BorradorDevCreateResponse
    {
        public bool Ok { get; set; }
        public long Numero { get; set; }
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Detalles { get; set; }
    }

    public class BorradorDevScanResponse
    {
        public bool Ok { get; set; }
        public long Numero { get; set; }
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Detalles { get; set; }
    }

    public class BorradorDevRunInfo
    {
        public int NumerosProcesados { get; set; }
        public int FilasProcesadas { get; set; }
    }

    public class BorradorDevRunResponse
    {
        public bool Ok { get; set; }
        public BorradorDevRunInfo Info { get; set; }
    }

    public class BorradorDevDetailResponse
    {
        public bool Ok { get; set; }
        public BorradorDevHeader Header { get; set; }
        public List<BorradorDevDetailItem> Detalles { get; set; }
    }

    public class BorradorDevUpdateRequestItem
    {
        public string CodigoBarra { get; set; }
        public decimal Cantidad { get; set; }
        public int NumeroCaja { get; set; }
        public int NumeroCajaOriginal { get; set; }
    }

    public class BorradorDevUpdateResponse
    {
        public bool Ok { get; set; }
        public long Numero { get; set; }
        public int Updated { get; set; }
        public int Inserted { get; set; }
        public int Deleted { get; set; }
    }
}
