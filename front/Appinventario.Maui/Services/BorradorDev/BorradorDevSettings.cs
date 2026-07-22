using Appinventario.Models;
using System;
using System.Threading.Tasks;

namespace Appinventario.Services.BorradorDev
{
    public static class BorradorDevSettings
    {
        private const string DefaultBaseUrl = "http://192.168.0.135:5001";

        public static async Task<string> GetBaseUrlAsync()
        {
            var parameter = await App.Database.GetParameterAsync();
            return BuildBaseUrlFromPrecioEndpoint(parameter != null ? parameter.IP : null);
        }

        public static async Task SaveBaseUrlAsync(string baseUrl)
        {
            var parameter = await App.Database.GetParameterAsync();

            if (parameter == null)
                parameter = new Parameter();

            parameter.IP = BuildPrecioEndpointFromBorradorBaseUrl(baseUrl);
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

        public static string BuildBaseUrlFromPrecioEndpoint(string precioIp)
        {
            var normalized = NormalizeBaseUrl(precioIp);
            if (string.IsNullOrWhiteSpace(normalized))
                return DefaultBaseUrl;

            try
            {
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

        public static string BuildPrecioEndpointFromBorradorBaseUrl(string baseUrl)
        {
            try
            {
                var normalized = NormalizeBaseUrl(baseUrl);
                Uri uri;

                if (Uri.TryCreate(normalized, UriKind.Absolute, out uri) && !string.IsNullOrWhiteSpace(uri.Host))
                    return uri.IsDefaultPort ? uri.Host : uri.Host + ":" + uri.Port;
            }
            catch
            {
                return string.Empty;
            }

            return string.Empty;
        }
    }
}

