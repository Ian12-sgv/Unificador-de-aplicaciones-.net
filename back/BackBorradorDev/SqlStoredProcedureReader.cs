using Microsoft.Data.SqlClient;
using System.Data;

public static class SqlStoredProcedureReader
{
    public static async Task<List<Dictionary<string, object?>>> ExecuteAsync(
        string connectionString,
        string procedure,
        params (string Name, SqlDbType Type, object? Value)[] parameters)
    {
        var rows = new List<Dictionary<string, object?>>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(procedure, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        // Derivar la firma real del procedimiento evita enviar parámetros
        // que no existan en la versión de la base de datos instalada.
        SqlCommandBuilder.DeriveParameters(command);
        var inputParameters = command.Parameters
            .Cast<SqlParameter>()
            .Where(parameter => parameter.Direction != ParameterDirection.ReturnValue)
            .ToList();

        for (var index = 0; index < parameters.Length; index++)
        {
            var supplied = parameters[index];
            var target = inputParameters.FirstOrDefault(parameter =>
                string.Equals(parameter.ParameterName, supplied.Name, StringComparison.OrdinalIgnoreCase));

            // Algunas instalaciones usan nombres distintos (por ejemplo Estacion
            // en lugar de Imei), pero mantienen el mismo orden del procedimiento.
            if (target is null && index < inputParameters.Count)
                target = inputParameters[index];

            if (target is not null)
                target.Value = supplied.Value ?? DBNull.Value;
        }

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

            rows.Add(row);
        }

        return rows;
    }
}
