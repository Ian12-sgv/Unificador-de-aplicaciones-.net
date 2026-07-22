using Appinventario.Models;
using System;
using System.Threading.Tasks;

namespace Appinventario.Services.BorradorDev
{
    public static class BorradorDevSettings
    {
        private const string DefaultBaseUrl = "http://192.168.0.117:5001";

        public static async Task<string> GetBaseUrlAsync()
        {
            var parameter = await App.Database.GetParameterAsync();

            if (parameter != null && !string.IsNullOrWhiteSpace(parameter.BorradorDevUrl))
                return NormalizeBaseUrl(parameter.BorradorDevUrl);

            if (parameter != null && !string.IsNullOrWhiteSpace(parameter.IP))
                return BuildDefaultFromPrecioIp(parameter.IP);

            return DefaultBaseUrl;
        }

        public static async Task SaveBaseUrlAsync(string baseUrl)
        {
            var parameter = await App.Database.GetParameterAsync();

            if (parameter == null)
                parameter = new Parameter();

            parameter.BorradorDevUrl = NormalizeBaseUrl(baseUrl);
            await App.Database.SaveParameterAsync(parameter);
        }

        public static string NormalizeBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return string.Empty;

            var clean = baseUrl.Trim().TrimEnd('/');

            if (!clean.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !clean.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                clean = "http://" + clean;
            }

            return clean;
        }

        private static string BuildDefaultFromPrecioIp(string precioIp)
        {
            try
            {
                var normalized = NormalizeBaseUrl(precioIp);
                Uri uri;

                if (Uri.TryCreate(normalized, UriKind.Absolute, out uri) && !string.IsNullOrWhiteSpace(uri.Host))
                    return "http://" + uri.Host + ":5001";
            }
            catch
            {
                return DefaultBaseUrl;
            }

            return DefaultBaseUrl;
        }
    }
}
