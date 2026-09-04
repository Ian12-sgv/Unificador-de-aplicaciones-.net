using Dapper;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using WebServicesApiNetCor.Models;
using WebServicesApiNetCor.Util;

namespace WebServicesApiNetCor.Data
{
    public class LoginRepository
    {
        private readonly string connectionString = Modulo.connectionString;

        public async Task<string> Validar(LoginUsuario login)
        {
            const string sql = @"
SELECT TOP (1) U.CodUsuario
FROM dbo.USUARIOS AS U
INNER JOIN dbo.USUARIOGRUPO AS UG
    ON UG.CodUsuario = U.CodUsuario
WHERE (U.CodUsuario = @Usuario OR U.NombreUsuario = @Usuario)
  AND U.Pasword = @Password
  AND U.Status = 1
  AND UG.CodGrupo = 'INVE';";

            using (IDbConnection connection = new SqlConnection(connectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<string>(
                    sql,
                    new { Usuario = login.Usuario, Password = login.Password });
            }
        }
    }
}
