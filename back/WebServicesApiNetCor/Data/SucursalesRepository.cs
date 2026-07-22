using Dapper;
using log4net;
using Remotion.Linq.Parsing.ExpressionVisitors.MemberBindings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WebServicesApiNetCor.Models;
using WebServicesApiNetCor.Util;

namespace WebServicesApiNetCor.Data
{
    public class SucursalesRepository
    {
        private string connectionString;
        public SucursalesRepository()
        { 
            connectionString = Modulo.connectionString;
        }

        public IDbConnection Connection
        {
            get
            {
                return new SqlConnection(connectionString);
            }
        }

        public async Task<IEnumerable<Test>> Get()
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                try {
                    string sql = string.Format("EXECUTE  [dbo].ApiBuscarTest");
                    dbConnection.Open();
                    return dbConnection.Query<Test>(sql);
                }
                catch(Exception fail)
                {
                    log.Error(fail.Message);
                    return null;
                }
               

            }
        }

        public async Task<IEnumerable<Generico>> GetExistParameters()
        {
            using (IDbConnection dbConnection = Connection)
            {
                string sql = string.Format("SELECT ISNULL(COUNT(*),0) AS Existe FROM sys.objects WHERE type = 'P' AND name = 'ApiBuscarTest'");
                dbConnection.Open();
                return dbConnection.Query<Generico>(sql);

            }
        }

 
        //public async Task<IEnumerable<Articulo>> Get(string CodigoBarra)
        //{
        //    using (IDbConnection dbConnection = Connection)
        //    {
        //        string sql = string.Format("SELECT I.Nombre,I.Existencia, ROUND(I.PrecioDetal + (PrecioDetal * IMP.PorcentajeImpuesto / 100), 2) / (SELECT TOP 1 Valor FROM TASA_CAMBIO ORDER BY Fecha DESC) AS PrecioDetalEnDolares," +
        //        "ROUND(I.PrecioMayor + (PrecioMayor * IMP.PorcentajeImpuesto / 100), 2) / (SELECT TOP 1 Valor FROM TASA_CAMBIO ORDER BY Fecha DESC) AS PrecioMayorEnDolares," +
        //        "ROUND(I.PrecioDetal + (PrecioDetal * IMP.PorcentajeImpuesto / 100), 2) AS PrecioDetal, " +
        //        "ROUND(I.PrecioMayor + (PrecioMayor * IMP.PorcentajeImpuesto / 100), 2) AS PrecioMayor," +
        //        "ROUND(I.PrecioAfiliado + (PrecioAfiliado * IMP.PorcentajeImpuesto / 100), 2) AS PrecioAfiliado, " +
        //        "ROUND(I.PrecioPromocion + (PrecioPromocion * IMP.PorcentajeImpuesto / 100), 2) AS PrecioPromocion," +
        //        "CASE WHEN(Promocion = 1) AND(GETDATE() BETWEEN FechaInicial AND FechaFinal) THEN 1 ELSE 0 END AS PromocionActiva, " +
        //        "CASE WHEN(Promocion = 1) AND(GETDATE() BETWEEN FechaInicial AND FechaFinal) THEN(1 - ROUND((PrecioPromocion / PrecioDetal), 2)) * 100 ELSE 0 END AS PorcentajeDescuento , " +
        //        "IMP.PorcentajeImpuesto AS TasaImpuesto " +
        //        "FROM INVENTARIO I INNER JOIN IMPUESTOS IMP ON IMP.Codigo = I.TipoImpuesto  " +
        //        "WHERE CodigoBarra = '{0}'", CodigoBarra);
        //        dbConnection.Open();
        //        return dbConnection.Query<Articulo>(sql);
        //    }
        //}

    }
}
