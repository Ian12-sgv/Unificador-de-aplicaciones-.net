using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using WebServicesApiNetCor.Util;

namespace WebServicesApiNetCor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var basePath = AppContext.BaseDirectory;
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile(Path.Combine("config", "escucha.json"), optional: true, reloadOnChange: true)
                    .AddCommandLine(args)
                    .Build();

                Modulo.connectionString = GetSqlConnectionString(configuration);

                var hostUrl = BuildHostUrl(configuration);

                var host = new WebHostBuilder()
                    .UseKestrel()
                    .UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "true")
                    .UseUrls(hostUrl)
                    .UseContentRoot(basePath)
                    .UseIISIntegration()
                    .UseStartup<Startup>()
                    .UseConfiguration(configuration)
                    .Build();

                PrintStartupMessage(hostUrl);
                host.Run();
            }
            catch (Exception fail)
            {
                Console.WriteLine("Error al iniciar el escucha: " + fail.Message);
                WaitForExitWhenInteractive();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

        private static void PrintStartupMessage(string hostUrl)
        {
            var ip = GetDisplayIp(hostUrl);
            var port = GetDisplayPort(hostUrl);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine();
                Console.WriteLine("Cancelando escucha...");
            };

            Console.WriteLine("Escucha levantado.");
            Console.WriteLine("IP: " + ip);
            Console.WriteLine("Puerto: " + port);
            Console.WriteLine("Presiona Ctrl + C para cancelar.");
        }

        private static string BuildHostUrl(IConfiguration configuration)
        {
            var configuredUrls = GetPrimaryUrl(configuration["Urls"]);
            if (!string.IsNullOrWhiteSpace(configuredUrls))
            {
                return configuredUrls;
            }

            var configIp = configuration["Escucha:EquipoIp"];
            var configPort = configuration["Escucha:Puerto"];

            if (!string.IsNullOrWhiteSpace(configIp))
            {
                if (string.IsNullOrWhiteSpace(configPort))
                {
                    configPort = "8000";
                }

                return "http://" + configIp.Trim() + ":" + configPort.Trim();
            }

            var hostUrl = GetPrimaryUrl(configuration["Kestrel:EndPoints:Http:Url"]);
            if (string.IsNullOrEmpty(hostUrl))
            {
                var preferredIp = GetPreferredLocalIpv4Address();
                if (string.IsNullOrWhiteSpace(preferredIp))
                {
                    preferredIp = "127.0.0.1";
                }

                hostUrl = "http://" + preferredIp + ":8000";
            }

            return hostUrl;
        }

        private static string GetSqlConnectionString(IConfiguration configuration)
        {
            var sqlConnectionString = configuration["ConnectionStrings:SqlServer"];
            if (!string.IsNullOrWhiteSpace(sqlConnectionString))
            {
                return sqlConnectionString.Trim();
            }

            sqlConnectionString = configuration["ConnectionString"];
            if (!string.IsNullOrWhiteSpace(sqlConnectionString))
            {
                return sqlConnectionString.Trim();
            }

            return string.Empty;
        }

        private static string GetPrimaryUrl(string urlValue)
        {
            if (string.IsNullOrWhiteSpace(urlValue))
            {
                return string.Empty;
            }

            var urls = urlValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (urls.Length == 0)
            {
                return string.Empty;
            }

            return urls[0].Trim();
        }

        private static string GetDisplayIp(string hostUrl)
        {
            if (Uri.TryCreate(hostUrl, UriKind.Absolute, out var uri))
            {
                if (uri.Host != "0.0.0.0" && uri.Host != "*" && uri.Host != "+" && uri.Host != "localhost")
                {
                    return uri.Host;
                }
            }

            var preferredIp = GetPreferredLocalIpv4Address();
            if (!string.IsNullOrWhiteSpace(preferredIp))
            {
                return preferredIp;
            }

            return "127.0.0.1";
        }

        private static string GetDisplayPort(string hostUrl)
        {
            if (Uri.TryCreate(hostUrl, UriKind.Absolute, out var uri))
            {
                return uri.Port.ToString();
            }

            return "8000";
        }

        private static void WaitForExitWhenInteractive()
        {
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                return;
            }

            Console.WriteLine("Presiona una tecla para cerrar.");
            Console.ReadKey(intercept: true);
        }

        private static string GetPreferredLocalIpv4Address()
        {
            try
            {
                var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
                var candidate = hostEntry.AddressList
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .Where(ip => !IPAddress.IsLoopback(ip))
                    .OrderByDescending(ip => IsPrivateIpv4(ip))
                    .ThenBy(ip => ip.ToString().StartsWith("192.168.", StringComparison.Ordinal))
                    .FirstOrDefault();

                return candidate?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsPrivateIpv4(IPAddress ipAddress)
        {
            var bytes = ipAddress.GetAddressBytes();
            if (bytes.Length != 4)
            {
                return false;
            }

            if (bytes[0] == 10)
            {
                return true;
            }

            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            return bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31;
        }
    }
}
