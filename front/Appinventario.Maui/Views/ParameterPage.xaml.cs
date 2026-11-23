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

                lblMessage.Text = "Probando conexión con el servidor…";
                var borradorService = new BorradorDevApiService(borradorBaseUrl);
                var borradorPing = await borradorService.GetPingAsync();

                Util.TestGuardar = true;
                viewModel.Message = BuildMainMessage(borradorPing);
                viewModel.Sucursal = BuildSummary(borradorPing);

                await DisplayAlert(
                    "Informacion",
                    BuildDetail(borradorPing), "Aceptar");
            }
            catch (Exception fail)
            {
                Util.TestGuardar = false;
                viewModel.Sucursal = "";
                viewModel.Message = "No se pudo conectar con el servidor.";
                await DisplayAlert("Error", NormalizeExceptionMessage(fail), "Aceptar");
            }
        }

        private static string BuildMainMessage(ServiceHealthResponse borradorHealth)
        {
            if (borradorHealth.ListenerOk)
                return "Conexion satisfactoria";

            return "Borrador Dev no respondio como se esperaba.";
        }

        private static string BuildSummary(ServiceHealthResponse borradorHealth)
        {
            return string.Empty;
        }

        private static string BuildDetail(ServiceHealthResponse borradorHealth)
        {
            return borradorHealth.ListenerOk ? "Conexion satisfactoria" : "Sin respuesta";
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
                return "El servidor cerró la conexión antes de responder.";

            return Util.NormalizeHttpExceptionMessage(exception, "El servidor tardó demasiado en responder durante la prueba.");
        }
    }
}
