using System.Net.Sockets;
using Microsoft.Data.SqlClient;

public sealed record SqlConnectionDiagnostic(
    bool DatabaseOk,
    string ErrorType,
    string Message,
    string Server,
    string Database,
    string Authentication,
    int? SqlErrorNumber = null);

public static class SqlConnectionDiagnostics
{
    public static async Task<SqlConnectionDiagnostic> CheckAsync(
        string? connectionString,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new SqlConnectionDiagnostic(
                false,
                "CONFIGURACION",
                "No está configurada ConnectionStrings:SqlServer.",
                string.Empty,
                string.Empty,
                "NO_CONFIGURADA");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = Math.Clamp(timeoutSeconds, 1, 30)
            };
        }
        catch
        {
            return new SqlConnectionDiagnostic(
                false,
                "CONFIGURACION",
                "La cadena de conexión SQL no tiene un formato válido.",
                string.Empty,
                string.Empty,
                "DESCONOCIDA");
        }

        var server = builder.DataSource?.Trim() ?? string.Empty;
        var database = builder.InitialCatalog?.Trim() ?? string.Empty;
        var authentication = builder.IntegratedSecurity ? "WINDOWS" : "SQL";

        try
        {
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return new SqlConnectionDiagnostic(
                true,
                "NINGUNO",
                "Conexión SQL satisfactoria.",
                server,
                database,
                authentication);
        }
        catch (Exception exception)
        {
            return BuildFailure(exception, server, database, authentication);
        }
    }

    private static SqlConnectionDiagnostic BuildFailure(
        Exception exception,
        string server,
        string database,
        string authentication)
    {
        var sqlError = FindSqlException(exception);
        var sqlErrorNumber = sqlError?.Number;
        var message = exception.ToString();

        if (sqlErrorNumber is 18452 or 18456 or 18470 || Contains(message, "login failed"))
        {
            return Failure("CLAVE", "No se pudo autenticar en SQL Server. Revise User Id y Password.");
        }

        if (sqlErrorNumber == 4060 || Contains(message, "cannot open database"))
        {
            return Failure("BASE_DE_DATOS", "No se pudo abrir la base de datos configurada. Revise Database.");
        }

        if (sqlErrorNumber == 26 || Contains(message, "error locating server/instance specified"))
        {
            return Failure("INSTANCIA", "No se encontró la instancia SQL. Revise Server=SERVIDOR\\INSTANCIA y SQL Browser.");
        }

        if (FindSocketException(exception) is not null ||
            sqlErrorNumber is 40 or 53 or 258 ||
            Contains(message, "network-related") ||
            Contains(message, "server was not found") ||
            Contains(message, "connection attempt failed") ||
            Contains(message, "timed out") ||
            Contains(message, "no such host"))
        {
            return Failure("IP_O_RED", "No se pudo alcanzar SQL Server. Revise IP o nombre, puerto, red y firewall.");
        }

        return Failure("DESCONOCIDO", "No se pudo conectar a SQL Server. Revise la configuración y el registro del servidor.");

        SqlConnectionDiagnostic Failure(string errorType, string friendlyMessage) =>
            new(false, errorType, friendlyMessage, server, database, authentication, sqlErrorNumber);
    }

    private static SqlException? FindSqlException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException)
                return sqlException;
        }

        return null;
    }

    private static SocketException? FindSocketException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SocketException socketException)
                return socketException;
        }

        return null;
    }

    private static bool Contains(string value, string text) =>
        value.Contains(text, StringComparison.OrdinalIgnoreCase);
}
