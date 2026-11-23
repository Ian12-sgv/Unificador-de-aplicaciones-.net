using Appinventario.Models;
using Appinventario.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TransferenciaPage : ContentPage
    {
        private bool navegando;
        private bool paginasPrecalentadas;
        private bool cargandoResumen;
        private bool actualizandoResumen;
        private int resumenLoopVersion;
        private ConsultarTransferenciaPage consultarTransferenciaPage;

        public TransferenciaPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            PrecalentarPaginas();
            IniciarActualizacionResumen();
        }

        protected override void OnDisappearing()
        {
            actualizandoResumen = false;
            resumenLoopVersion++;
            base.OnDisappearing();
        }

        private void IniciarActualizacionResumen()
        {
            if (actualizandoResumen)
                return;

            actualizandoResumen = true;
            resumenLoopVersion++;
            _ = ActualizarResumenEnVivoAsync(resumenLoopVersion);
        }

        private async Task ActualizarResumenEnVivoAsync(int loopVersion)
        {
            while (actualizandoResumen && loopVersion == resumenLoopVersion)
            {
                await LoadResumenAsync();
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        private void PrecalentarPaginas()
        {
            if (paginasPrecalentadas)
                return;

            paginasPrecalentadas = true;
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(200);
                consultarTransferenciaPage ??= new ConsultarTransferenciaPage();
            });
        }

        private async void Button_Nueva(object sender, EventArgs e)
        {
            await NavegarAsync(() => new NuevaTransferenciaPage());
        }

        private async void Button_Consultar(object sender, EventArgs e)
        {
            await NavegarAsync(() => consultarTransferenciaPage ??= new ConsultarTransferenciaPage());
        }

        private async void Button_Modificar(object sender, EventArgs e)
        {
            await NavegarAsync(() => new ModificarTransferenciaPage());
        }

        private async void Button_Eliminar(object sender, EventArgs e)
        {
            await NavegarAsync(() => new EliminarTransferenciaPage());
        }

        private async void NuevaCard_Tapped(object sender, EventArgs e)
        {
            await NavegarAsync(() => new NuevaTransferenciaPage());
        }

        private async void HistorialCard_Tapped(object sender, EventArgs e)
        {
            await NavegarAsync(() => consultarTransferenciaPage ??= new ConsultarTransferenciaPage());
        }

        private async void ContinuarEditando_Clicked(object sender, EventArgs e)
        {
            await NavegarAsync(() => new NuevaTransferenciaPage());
        }

        private async Task NavegarAsync(Func<Page> crearPagina)
        {
            if (navegando)
                return;

            navegando = true;
            try
            {
                var page = crearPagina();
                await Navigation.PushAsync(page, false);
            }
            finally
            {
                navegando = false;
            }
        }

        private async Task LoadResumenAsync()
        {
            if (cargandoResumen)
                return;

            cargandoResumen = true;
            try
            {
                await Task.Yield();
                var parameter = await App.Database.GetParameterAsync();
                var serviceUrl = Util.NormalizeServiceUrl(parameter?.IP ?? string.Empty);
                if (string.IsNullOrWhiteSpace(serviceUrl))
                    return;

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(serviceUrl + "/api/Transferencia/resumen-estados"),
                    Method = HttpMethod.Get
                };
                request.Headers.Add("Accept", "application/json");

                var client = Util.CreateHttpClient(TimeSpan.FromSeconds(3));
                var response = await client.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                    return;

                var content = await response.Content.ReadAsStringAsync();
                var resumen = JsonConvert.DeserializeObject<TransferenciaResumenPortal>(content);
                if (resumen == null)
                    return;

                abiertasNumero.Text = resumen.GuardadasCount.ToString();
                procesadasNumero.Text = resumen.AprobadasCount.ToString();
                exportadasNumero.Text = resumen.Exportadas.ToString();
            }
            catch
            {
                // El portal conserva los valores actuales si el escucha no responde.
            }
            finally
            {
                cargandoResumen = false;
            }
        }

        private class TransferenciaResumenPortal
        {
            public int Guardadas { get; set; }
            public int Aprobadas { get; set; }
            public int Exportadas { get; set; }
            public int Abiertas { get; set; }
            public int Procesadas { get; set; }

            public int GuardadasCount => Guardadas > 0 ? Guardadas : Abiertas;
            public int AprobadasCount => Aprobadas > 0 ? Aprobadas : Procesadas;
        }
    }
}
