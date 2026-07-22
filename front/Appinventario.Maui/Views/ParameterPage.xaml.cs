using Appinventario.Models;
using Appinventario.Services;
using Appinventario.Services.BorradorDev;
using Appinventario.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Appinventario.Views
{
    public partial class NewItemPage : ContentPage
    {
        Parameter parameter;

        public NewItemPage()
        {
            InitializeComponent();
            BindingContext = new ParameterViewModel();

            parameter = new Parameter();

            var viewModel = (ParameterViewModel)BindingContext;
            if (viewModel.SelectCommand.CanExecute(null))
                viewModel.SelectCommand.Execute(null);
        }

        private async void OnButtonClicked(object sender, EventArgs e)
        {
            var viewModel = (ParameterViewModel)BindingContext;

            try
            {
                string borradorBaseUrl = BorradorDevSettings.BuildBaseUrlFromPrecioEndpoint(viewModel.IP);

                lblMessage.Text = "Probando escucha de Borrador Dev...";
                var borradorService = new BorradorDevApiService(borradorBaseUrl);
                var borradorPing = await borradorService.GetPingAsync();

                Util.TestGuardar = true;
                viewModel.Message = BuildMainMessage(borradorPing);
                viewModel.Sucursal = BuildSummary(borradorPing);

                await DisplayAlert(
                    "Informacion",
                    BuildDetail(borradorPing),
                    "ok");
            }
            catch (Exception fail)
            {
                Util.TestGuardar = false;
                viewModel.Sucursal = "";
                viewModel.Message = "No se pudo conectar al escucha compartido.";
                await DisplayAlert("Error", "Error: " + NormalizeExceptionMessage(fail), "ok");
            }
        }

        private static string BuildMainMessage(ServiceHealthResponse borradorHealth)
        {
            if (borradorHealth.ListenerOk)
                return "Conexion satisfactoria con Borrador Dev.";

            return "Borrador Dev no respondio como se esperaba.";
        }

        private static string BuildSummary(ServiceHealthResponse borradorHealth)
        {
            var summary = new List<string>
            {
                "Borrador Dev: Listener OK",
                "SQL: no validado en esta prueba"
            };

            return string.Join(" | ", summary);
        }

        private static string BuildDetail(ServiceHealthResponse borradorHealth)
        {
            var lines = new List<string>
            {
                "Borrador Dev listener: " + (borradorHealth.ListenerOk ? "OK" : "Sin respuesta"),
                "Base de datos: no validada en esta prueba.",
                "Con VPN activa es normal que SQL no responda, pero la app si debe poder llegar al escucha."
            };

            return string.Join(Environment.NewLine, lines);
        }

        private static string CleanMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Sin detalle";

            var clean = message.Replace(Environment.NewLine, " ").Trim();
            return clean.Length > 160 ? clean.Substring(0, 160) + "..." : clean;
        }

        private static string NormalizeExceptionMessage(Exception exception)
        {
            if (exception == null)
                return "Error desconocido.";

            if (exception.Message == "Socket closed")
                return "El listener cerro la conexion antes de responder.";

            return Util.NormalizeHttpExceptionMessage(exception, "El escucha tardó demasiado en responder durante la prueba.");
        }
    }
}
