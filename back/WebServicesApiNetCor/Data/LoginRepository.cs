using Dapper;
using System;
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
SELECT TOP (1)
    U.CodUsuario,
    U.Pasword,
    CAST(U.Pasword AS VARBINARY(200)) AS PaswordBytes
FROM dbo.USUARIOS AS U
INNER JOIN dbo.USUARIOGRUPO AS UG
    ON UG.CodUsuario = U.CodUsuario
WHERE (U.CodUsuario = @Usuario OR U.NombreUsuario = @Usuario)
  AND U.Status = 1
  AND UG.CodGrupo = 'INVE';";

            using (IDbConnection connection = new SqlConnection(connectionString))
            {
                var usuario = await connection.QueryFirstOrDefaultAsync<UsuarioLogin>(
                    sql,
                    new { Usuario = login.Usuario });

                if (usuario == null)
                    return null;

                // Mantiene usuarios con contraseña en texto y admite el formato legacy.
                if (string.Equals(usuario.Pasword, login.Password, StringComparison.Ordinal) ||
                    PasswordLegacyCodec.Matches(login.Password, usuario.PaswordBytes))
                {
                    return usuario.CodUsuario;
                }

                return null;
            }
        }

        private sealed class UsuarioLogin
        {
            public string CodUsuario { get; set; }
            public string Pasword { get; set; }
            public byte[] PaswordBytes { get; set; }
        }
    }
}