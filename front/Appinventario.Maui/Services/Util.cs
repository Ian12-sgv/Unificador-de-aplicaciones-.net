using System;
using Appinventario.Interfaces;
using Microsoft.Maui.Controls;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
#if ANDROID
using Xamarin.Android.Net;
#endif

namespace Appinventario.Services
{
    static public class Util
    {
        // static public string url = "";
        static public string NumPagG = "";
        static public string ObsG = "";
        static public string CodigoDeBarra = "";
        static public bool TestGuardar = false;
        static public string Imei = "";
        static public bool EstatusImei = false;
        public static readonly TimeSpan DefaultHttpTimeout = TimeSpan.FromSeconds(15);
        private static readonly HttpMessageHandler SharedHttpMessageHandler = CreateHttpMessageHandler(DefaultHttpTimeout);

        public static string NormalizeServiceUrl(string configuredUrl)
        {
            var value = (configuredUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                value = "http://" + value;
            }

            return value.TrimEnd('/');
        }

        public static void TryPlayAudioFile(string fileName)
        {
            try
            {
                DependencyService.Get<IAudio>()?.PlayAudioFile(fileName);
            }
            catch
            {
                // Evita que la app se cierre si el audio falla o no esta registrado.
            }
        }

        public static HttpClient CreateHttpClient(TimeSpan? timeout = null)
        {
            var effectiveTimeout = timeout ?? DefaultHttpTimeout;
            var useSharedHandler = effectiveTimeout == DefaultHttpTimeout;
            var handler = useSharedHandler
                ? SharedHttpMessageHandler
                : CreateHttpMessageHandler(effectiveTimeout);

            return new HttpClient(handler, disposeHandler: !useSharedHandler)
            {
                Timeout = effectiveTimeout
            };
        }

        private static HttpMessageHandler CreateHttpMessageHandler(TimeSpan timeout)
        {
#if ANDROID
            return new AndroidMessageHandler
            {
                ConnectTimeout = timeout,
                ReadTimeout = timeout,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                UseCookies = false
            };
#else
            return new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                UseCookies = false
            };
#endif
        }

        public static string NormalizeHttpExceptionMessage(Exception exception, string timeoutMessage = null)
        {
            if (exception == null)
            {
                return "Error desconocido.";
            }

            if (exception is TaskCanceledException ||
                exception is TimeoutException ||
                (!string.IsNullOrWhiteSpace(exception.Message) &&
                 exception.Message.IndexOf("HttpClient.Timeout", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return string.IsNullOrWhiteSpace(timeoutMessage)
                    ? "El escucha tardó demasiado en responder. Intenta nuevamente."
                    : timeoutMessage;
            }

            return exception.Message;
        }
    }
}
