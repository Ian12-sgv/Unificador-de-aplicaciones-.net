using Appinventario.Models.BorradorDev;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Appinventario.Services.BorradorDev
{
    public class BorradorDevApiService
    {
        private readonly string baseUrl;

        public BorradorDevApiService(string baseUrl)
        {
            this.baseUrl = BorradorDevSettings.NormalizeBaseUrl(baseUrl);
        }

        public async Task TestConnectionAsync()
        {
            var payload = new
            {
                numero = 0,
                aprobar = false
            };

            await SendAsync<object>(HttpMethod.Post, "/borrador-dev/run", payload);
        }

        public async Task<List<BorradorDevListItem>> ListAsync(string query = null, int top = 50, int? status = null)
        {
            var queryItems = new List<string>
            {
                "top=" + top
            };

            if (!string.IsNullOrWhiteSpace(query))
                queryItems.Add("q=" + Uri.EscapeDataString(query.Trim()));

            if (status.HasValue)
                queryItems.Add("status=" + status.Value);

            var response = await SendAsync<BorradorDevListResponse>(HttpMethod.Get, "/borrador-dev/list?" + string.Join("&", queryItems), null);
            return response != null && response.Items != null ? response.Items : new List<BorradorDevListItem>();
        }

        public Task<BorradorDevCreateResponse> CreateAsync(string usuario, string observacion, DateTime? fecha, string codigoBarra, int numeroCaja, decimal cantidad, int status)
        {
            var payload = new
            {
                fecha = fecha.HasValue ? fecha.Value.ToString("yyyy-MM-dd") : null,
                observacion = observacion ?? string.Empty,
                usuario = usuario ?? string.Empty,
                status = status,
                detalles = new[]
                {
                    new
                    {
                        codigoBarra = codigoBarra,
                        cantidad = cantidad,
                        numeroCaja = numeroCaja
                    }
                }
            };

            return SendAsync<BorradorDevCreateResponse>(HttpMethod.Post, "/borrador-dev/create", payload);
        }

        public Task<BorradorDevScanResponse> ScanAsync(long numero, string codigoBarra, int numeroCaja, decimal cantidad)
        {
            var payload = new
            {
                detalles = new[]
                {
                    new
                    {
                        codigoBarra = codigoBarra,
                        cantidad = cantidad,
                        numeroCaja = numeroCaja
                    }
                }
            };

            return SendAsync<BorradorDevScanResponse>(HttpMethod.Post, "/borrador-dev/" + numero + "/scan", payload);
        }

        public Task<BorradorDevRunResponse> RunAsync(long numero, bool aprobar)
        {
            var payload = new
            {
                numero = numero,
                aprobar = aprobar
            };

            return SendAsync<BorradorDevRunResponse>(HttpMethod.Post, "/borrador-dev/run", payload);
        }

        public Task<BorradorDevDetailResponse> GetDetalleAsync(long numero)
        {
            return SendAsync<BorradorDevDetailResponse>(HttpMethod.Get, "/borrador-dev/" + numero, null, true);
        }

        public Task<BorradorDevUpdateResponse> UpdateDetalleAsync(long numero, IEnumerable<BorradorDevUpdateRequestItem> detalles)
        {
            var payload = new
            {
                detalles = detalles
            };

            return SendAsync<BorradorDevUpdateResponse>(HttpMethod.Put, "/borrador-dev/" + numero + "/detalle", payload);
        }

        private async Task<T> SendAsync<T>(HttpMethod method, string path, object payload, bool allowNotFound = false)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(method, BuildUri(path)))
            {
                client.Timeout = TimeSpan.FromSeconds(12);

                if (payload != null)
                {
                    var json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
                    return default(T);

                if (!response.IsSuccessStatusCode)
                    throw BuildException(response.StatusCode, content);

                if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(content))
                    return default(T);

                return JsonConvert.DeserializeObject<T>(content);
            }
        }

        private Uri BuildUri(string path)
        {
            return new Uri(baseUrl.TrimEnd('/') + path);
        }

        private Exception BuildException(HttpStatusCode statusCode, string body)
        {
            return new BorradorDevApiException((int)statusCode, ExtractErrorMessage(body), body);
        }

        private string ExtractErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "Error desconocido.";

            var trimmed = body.Trim();

            try
            {
                var obj = JObject.Parse(trimmed);
                var messages = new List<string>
                {
                    ValueOrNull(obj["error"]),
                    ValueOrNull(obj["message"]),
                    ValueOrNull(obj["title"]),
                    ValueOrNull(obj["detail"]),
                    ValueOrNull(obj["inner"])
                };

                var errors = obj["errors"] as JObject;
                if (errors != null)
                {
                    foreach (var property in errors.Properties())
                    {
                        var values = property.Value as JArray;
                        if (values == null)
                            continue;

                        foreach (var item in values)
                        {
                            var value = ValueOrNull(item);
                            if (!string.IsNullOrWhiteSpace(value))
                                messages.Add(property.Name + ": " + value);
                        }
                    }
                }

                var message = messages.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }
            catch
            {
                return trimmed;
            }

            return trimmed;
        }

        private string ValueOrNull(JToken token)
        {
            if (token == null)
                return null;

            var value = token.ToString().Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
