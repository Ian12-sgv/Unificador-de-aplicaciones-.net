using Appinventario.Models;
using Appinventario.Services.BorradorDev;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Appinventario.Services
{
    public static class TomaFisicaAuthService
    {
        public static string Token { get; private set; } = string.Empty;
        public static string CodUsuario { get; private set; } = string.Empty;
        public static bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

        public static async Task<TomaFisicaLoginResult> LoginAsync(string usuario, string password)
        {
            var parameter = await App.Database.GetParameterAsync();
            var baseUrl = BorradorDevSettings.BuildBaseUrlFromPrecioEndpoint(parameter?.IP);

            if (string.IsNullOrWhiteSpace(baseUrl))
                return TomaFisicaLoginResult.Error("Configure el escucha en Parámetros.");

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrEmpty(password))
                return TomaFisicaLoginResult.Error("Indique usuario y contraseña.");

            try
            {
                using var client = Util.CreateHttpClient(TimeSpan.FromSeconds(10));
                var json = JsonConvert.SerializeObject(new
                {
                    usuario = usuario.Trim(),
                    password
                });
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(baseUrl + "/api/Login", content);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return TomaFisicaLoginResult.Error("Usuario o contraseña inválidos.");

                if (!response.IsSuccessStatusCode)
                    return TomaFisicaLoginResult.Error("No se pudo iniciar sesión (" + (int)response.StatusCode + ").");

                var responseJson = await response.Content.ReadAsStringAsync();
                var login = JsonConvert.DeserializeObject<LoginRespuesta>(responseJson);

                if (login == null || string.IsNullOrWhiteSpace(login.Token) ||
                    string.IsNullOrWhiteSpace(login.CodUsuario))
                    return TomaFisicaLoginResult.Error("La respuesta de inicio de sesión no es válida.");

                Token = login.Token;
                CodUsuario = login.CodUsuario;
                return TomaFisicaLoginResult.Success();
            }
            catch (Exception fail)
            {
                return TomaFisicaLoginResult.Error(
                    Util.NormalizeHttpExceptionMessage(fail, "El escucha tardó demasiado en responder."));
            }
        }

        public static bool ConfigureAuthorization(HttpClient client)
        {
            if (client == null || !IsAuthenticated)
                return false;

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Token);
            return true;
        }

        public static void Clear()
        {
            Token = string.Empty;
            CodUsuario = string.Empty;
        }
    }

    public class TomaFisicaLoginResult
    {
        public bool IsSuccess { get; private set; }
        public string Message { get; private set; }

        public static TomaFisicaLoginResult Success() =>
            new TomaFisicaLoginResult { IsSuccess = true, Message = string.Empty };

        public static TomaFisicaLoginResult Error(string message) =>
            new TomaFisicaLoginResult { IsSuccess = false, Message = message };
    }
}
