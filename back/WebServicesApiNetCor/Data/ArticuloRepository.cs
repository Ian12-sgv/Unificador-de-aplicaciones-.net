using Dapper;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using WebServicesApiNetCor.Models;
using WebServicesApiNetCor.Util;

namespace WebServicesApiNetCor.Data
{
    public class ArticuloRepository
    {
        private string connectionString;

        public ArticuloRepository()
        {
            connectionString = Modulo.connectionString;
        }

        public IDbConnection Connection
        {
            get
            {
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    ConnectTimeout = 8
                };

                return new SqlConnection(builder.ConnectionString);
            }
        }

        public async Task<IEnumerable<Articulo>> Get(string CodigoBarra)
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));

                try
                {
                    dbConnection.Open();
                    string sql = @"
SELECT
    I.Nombre,
    I.Existencia AS Existencia,
    I.CodigoBarra,
    M.Codigo AS CodigoMarca,
    M.Nombre AS NombreMarca,

    ROUND(I.PrecioDetal + (I.PrecioDetal * IMP.PorcentajeImpuesto / 100), 2)
        / (SELECT TOP 1 Valor FROM TASA_CAMBIO ORDER BY Fecha DESC) AS PrecioDolaresDetal,

    ROUND(I.PrecioMayor + (I.PrecioMayor * IMP.PorcentajeImpuesto / 100), 2)
        / (SELECT TOP 1 Valor FROM TASA_CAMBIO ORDER BY Fecha DESC) AS PrecioDolaresMayor,

    ROUND(I.PrecioDetal + (I.PrecioDetal * IMP.PorcentajeImpuesto / 100), 2) AS PrecioDetal,
    ROUND(I.PrecioMayor + (I.PrecioMayor * IMP.PorcentajeImpuesto / 100), 2) AS PrecioMayor,
    ROUND(I.PrecioAfiliado + (I.PrecioAfiliado * IMP.PorcentajeImpuesto / 100), 2) AS PrecioAfiliado,
    ROUND(I.PrecioPromocion + (I.PrecioPromocion * IMP.PorcentajeImpuesto / 100), 2) AS PrecioPromocion,

    CASE
        WHEN (I.Promocion = 1)
         AND (CONVERT(VARCHAR(10), GETDATE(), 103) BETWEEN I.FechaInicial AND I.FechaFinal)
        THEN 1 ELSE 0
    END AS PromocionActiva,

    CASE
        WHEN (I.Promocion = 1)
         AND (CONVERT(VARCHAR(10), GETDATE(), 103) BETWEEN I.FechaInicial AND I.FechaFinal)
        THEN (1 - ROUND((I.PrecioPromocion / I.PrecioDetal), 2)) * 100
        ELSE 0
    END AS PorcentajeDescuento,

    IMP.PorcentajeImpuesto AS TasaImpuesto
FROM INVENTARIO I
INNER JOIN IMPUESTOS IMP ON IMP.Codigo = I.TipoImpuesto
INNER JOIN MARCAS M ON M.Codigo = I.CodigoMarca
WHERE I.CodigoBarra = @CodigoBarra;
";

                    return await dbConnection.QueryAsync<Articulo>(sql, new { CodigoBarra });
                }
                catch (Exception fail)
                {
                    log.Error("Error SQL en Articulo: " + fail.GetBaseException().Message);
                    throw new InvalidOperationException("No se pudo consultar SQL Server.", fail);
                }
            }
        }
    }
}
