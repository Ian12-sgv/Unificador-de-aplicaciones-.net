using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var localIp = GetLocalIpAddress();
var listenUrls = string.Empty;

// Leer URL/puerto desde appsettings.json (clave "Urls")
var urls = builder.Configuration["Urls"];
if (!string.IsNullOrWhiteSpace(urls))
{
    if (!ValidateConfiguredIp(urls, localIp, out var invalidIp))
    {
        WriteStartupLine("IP ERROR: La IP configurada " + invalidIp + " no coincide con la IP LOCAL " + localIp + ". El escucha no se iniciará.");
        LogStartupInfo(builder.Configuration, localIp);
        WaitForExitWhenInteractive();
        Environment.ExitCode = 1;
        return;
    }

    var stopExistingSetting = builder.Configuration["Startup:StopExistingBackBorradorDevOnSamePort"];
    var stopExisting = !bool.TryParse(stopExistingSetting, out var parsedStopExisting) || parsedStopExisting;
    if (stopExisting)
    {
        StopExistingBackBorradorDevOnSamePorts(urls);
    }

    listenUrls = BuildListenUrls(urls, localIp);
    builder.WebHost.UseUrls(listenUrls);
}

// Controllers + JSON (util para Flutter y evitar problemas con mayusculas/minusculas)
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        // Mantiene los nombres EXACTOS como tus propiedades/records (Numero, Fecha, etc.)
        // asi no te cambia a camelCase si no quieres.
        o.JsonSerializerOptions.PropertyNamingPolicy = null;
        o.JsonSerializerOptions.DictionaryKeyPolicy = null;
    });
builder.Services.AddSingleton<TomaFisicaSessionStore>();

// ==============================
// CORS (NECESARIO para Flutter Web / Debug en navegador)
// En Android/iOS nativo usualmente no molesta, pero no hace dano dejarlo.
// ==============================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllDev", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();
LogStartupInfo(app.Configuration, localIp);
app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = LogStartupStatusAsync(app.Configuration, listenUrls);
});

// CORS antes de MapControllers
app.UseCors("AllowAllDev");

// app.UseHttpsRedirection(); // opcional

app.MapControllers();
app.Run();

static void LogStartupInfo(IConfiguration configuration, string localIp)
{
    WriteStartupLine("EJECUTANDOSE");
    WriteStartupLine("APP: " + (configuration["Startup:ApplicationName"] ?? "ServicioUnificado"));
    WriteStartupLine("VERSION: " + (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0"));
    WriteStartupLine("SISTEMA OPERATIVO: " + Environment.OSVersion);
    WriteStartupLine("BD: " + GetDatabaseDisplayName(configuration.GetConnectionString("SqlServer")));
    WriteStartupLine("IP LOCAL: " + localIp);
}

static void WaitForExitWhenInteractive()
{
    if (Console.IsInputRedirected || Console.IsOutputRedirected)
        return;

    using var cancellation = new ManualResetEventSlim(false);
    ConsoleCancelEventHandler onCancel = (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellation.Set();
    };

    Console.CancelKeyPress += onCancel;
    try
    {
        Console.WriteLine("Presiona Ctrl+C para cerrar.");
        cancellation.Wait();
    }
    finally
    {
        Console.CancelKeyPress -= onCancel;
    }
}
static void WriteStartupLine(string message)
{
    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff} [1] INFO  WebServicesApiNetCor.Startup - {message}");
}

static IEnumerable<string> GetDisplayListenUrls(string listenUrls)
{
    if (string.IsNullOrWhiteSpace(listenUrls))
        return Array.Empty<string>();

    return listenUrls.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

static bool ValidateConfiguredIp(string urls, string localIp, out string invalidIp)
{
    invalidIp = string.Empty;
    var entries = urls.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    foreach (var entry in entries)
    {
        if (!Uri.TryCreate(entry, UriKind.Absolute, out var uri))
            continue;

        var host = uri.Host.Trim();
        if (IsWildcardOrLocalhost(host))
            continue;

        if (!IPAddress.TryParse(host, out var configuredAddress))
            continue;

        if (!string.Equals(configuredAddress.ToString(), localIp, StringComparison.OrdinalIgnoreCase))
        {
            invalidIp = configuredAddress.ToString();
            return false;
        }
    }

    return true;
}

static bool IsWildcardOrLocalhost(string host)
{
    return string.Equals(host, "0.0.0.0", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(host, "*", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(host, "+", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase);
}

static string BuildListenUrls(string urls, string localIp)
{
    var entries = urls.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var resolved = entries.Select(entry =>
    {
        if (!Uri.TryCreate(entry, UriKind.Absolute, out var uri))
            return entry;

        var host = uri.Host;
        if (host is "*" or "+")
            host = "0.0.0.0";

        var uriBuilder = new UriBuilder(uri)
        {
            Host = host
        };

        return uriBuilder.Uri.ToString().TrimEnd('/');
    });

    return string.Join(';', resolved);
}

static string GetDatabaseDisplayName(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
        return "SIN_CONFIGURAR";

    try
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var server = (builder.DataSource ?? string.Empty).Trim();
        var database = (builder.InitialCatalog ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(server))
            return database;

        if (string.IsNullOrWhiteSpace(database))
            return server;

        return server + "\\" + database;
    }
    catch
    {
        return connectionString;
    }
}

static string GetLocalIpAddress()
{
    var candidates = NetworkInterface.GetAllNetworkInterfaces()
        .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
        .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
        .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
        .Select(ip => ip.Address)
        .Where(ip => !IPAddress.IsLoopback(ip))
        .Select(ip => ip.ToString())
        .ToList();

    var privateIp = candidates.FirstOrDefault(ip =>
        ip.StartsWith("192.168.", StringComparison.Ordinal) ||
        ip.StartsWith("10.", StringComparison.Ordinal) ||
        Is172PrivateIp(ip));

    return privateIp ?? candidates.FirstOrDefault() ?? "127.0.0.1";
}

static bool Is172PrivateIp(string ip)
{
    var parts = ip.Split('.');
    return parts.Length == 4 &&
           parts[0] == "172" &&
           int.TryParse(parts[1], out var second) &&
           second >= 16 &&
           second <= 31;
}

static async Task LogStartupStatusAsync(IConfiguration configuration, string listenUrls)
{
    var shouldCheckDatabase = configuration.GetValue("DatabaseDiagnostics:LogOnStartup", true);
    if (shouldCheckDatabase)
    {
        var timeout = configuration.GetValue<int?>("DatabaseDiagnostics:ConnectTimeoutSeconds") ?? 3;
        var diagnostic = await SqlConnectionDiagnostics.CheckAsync(
            configuration.GetConnectionString("SqlServer"),
            timeout);

        if (!diagnostic.DatabaseOk)
        {
            WriteStartupLine($"SQL ERROR {diagnostic.ErrorType}: {diagnostic.Message}");
            if (!string.IsNullOrWhiteSpace(diagnostic.Server))
            {
                WriteStartupLine($"SQL CONFIGURADO: Servidor={diagnostic.Server}; Base={diagnostic.Database}; Autenticación={diagnostic.Authentication}.");
            }

            return;
        }

        WriteStartupLine("SQL: CONEXIÓN SATISFACTORIA.");
    }

    foreach (var url in GetDisplayListenUrls(listenUrls))
        Console.WriteLine("Now listening on: " + url);

    Console.WriteLine("Application started. Press Ctrl+C to shut down.");
}
static void StopExistingBackBorradorDevOnSamePorts(string urls)
{
    var ports = GetConfiguredPorts(urls);
    if (ports.Count == 0) return;

    var currentPid = Environment.ProcessId;
    var currentName = Process.GetCurrentProcess().ProcessName;

    foreach (var port in ports)
    {
        foreach (var pid in GetListeningPids(port))
        {
            if (pid == currentPid) continue;

            try
            {
                using var process = Process.GetProcessById(pid);
                if (!string.Equals(process.ProcessName, currentName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"El puerto {port} esta ocupado por {process.ProcessName} (PID {pid}). No se cerrara porque no es {currentName}.");
                    continue;
                }

                Console.WriteLine($"Cerrando instancia anterior de {currentName} en puerto {port} (PID {pid})...");
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No se pudo cerrar el proceso {pid} del puerto {port}: {ex.Message}");
            }
        }
    }
}

static HashSet<int> GetConfiguredPorts(string urls)
{
    var ports = new HashSet<int>();
    var entries = urls.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    foreach (var entry in entries)
    {
        if (Uri.TryCreate(entry, UriKind.Absolute, out var uri) && uri.Port > 0)
        {
            ports.Add(uri.Port);
        }
    }

    return ports;
}

static IEnumerable<int> GetListeningPids(int port)
{
    using var netstat = new Process();
    netstat.StartInfo = new ProcessStartInfo
    {
        FileName = "netstat",
        Arguments = "-ano -p TCP",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    netstat.Start();
    var output = netstat.StandardOutput.ReadToEnd();
    netstat.WaitForExit(5000);

    foreach (var line in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
    {
        if (!line.Contains("LISTENING", StringComparison.OrdinalIgnoreCase)) continue;

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5) continue;

        var localAddress = parts[1];
        var pidText = parts[^1];
        if (!localAddress.EndsWith($":{port}", StringComparison.OrdinalIgnoreCase)) continue;
        if (int.TryParse(pidText, out var pid)) yield return pid;
    }
}
