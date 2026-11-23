using Appinventario.Models;
using Appinventario.Services;
using Appinventario.ViewModels;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Appinventario.Views
{
    public partial class TomaFisica2Page : ContentPage
    {
        private readonly TomaFisicaViewModel viewModel;
        private CancellationTokenSource scanDebounce;
        private bool registrandoCodigo;

        private sealed class ArticuloEscaneado
        {
            public string Nombre { get; set; }
        }

        public TomaFisica2Page()
        {
            InitializeComponent();
            BindingContext = viewModel = new TomaFisicaViewModel();
            viewModel.OnSelect();
            EtiquetaInVisible();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!AuthService.CanUseTomaFisica2() || !TomaFisicaAuthService.IsAuthenticated)
            {
                await Navigation.PopToRootAsync();
                return;
            }

            viewModel.OnSelect();
            if (!string.IsNullOrWhiteSpace(Util.NumPagG))
            {
                txtPagina.Text = Util.NumPagG;
                Util.NumPagG = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(Util.ObsG))
            {
                txtObs.Text = Util.ObsG;
                Util.ObsG = string.Empty;
            }

            txtCodigo.Text = string.Empty;
            txtCodigo.Focus();
        }

        private async void SearchCatalogo2(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(lblUrl.Text))
            {
                await DisplayAlert("Advertencia", "Configure el servidor en Parámetros.", "Aceptar");
                return;
            }

            await Navigation.PushAsync(new BuscarTomaPage(lblUrl.Text, true));
        }

        private void OnPaginaCompleted(object sender, EventArgs e) => txtObs.Focus();
        private void OnObservacionCompleted(object sender, EventArgs e) => txtCodigo.Focus();

        private async void CodigoTextChanged(object sender, TextChangedEventArgs e)
        {
            scanDebounce?.Cancel();
            var codigo = (e.NewTextValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(codigo) || registrandoCodigo)
                return;

            var cancellation = scanDebounce = new CancellationTokenSource();
            try
            {
                await Task.Delay(250, cancellation.Token);
                if (!cancellation.IsCancellationRequested &&
                    string.Equals(codigo, (txtCodigo.Text ?? string.Empty).Trim(), StringComparison.Ordinal))
                {
                    await RegistrarAsync();
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception fail)
            {
                Util.TryPlayAudioFile("no-valido.mp3");
                await DisplayAlert("Error", Util.NormalizeHttpExceptionMessage(fail), "Aceptar");
            }
        }

        private async void OnCodigoCompleted(object sender, EventArgs e) => await RegistrarAsync();

        private async Task RegistrarAsync()
        {
            if (registrandoCodigo)
                return;

            if (string.IsNullOrWhiteSpace(lblUrl.Text))
            {
                await DisplayAlert("Advertencia", "Configure el servidor en Parámetros.", "Aceptar");
                return;
            }

            if (!int.TryParse(txtPagina.Text, out var numeroPagina) || numeroPagina <= 0 || numeroPagina > 50000)
            {
                await FeedbackService.ShowAsync("La página debe ser un número entre 1 y 50000.");
                txtPagina.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtObs.Text))
            {
                await FeedbackService.ShowAsync("Escriba una observación.");
                txtObs.Focus();
                return;
            }

            var codigoBarra = (txtCodigo.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(codigoBarra))
            {
                await FeedbackService.ShowAsync("Escanee o escriba un código de barra.");
                txtCodigo.Focus();
                return;
            }

            registrandoCodigo = true;
            try
            {
                var toma = new TomaFisica
                {
                    NumeroPagina = numeroPagina,
                    Fecha = DateTime.Now,
                    Observacion = txtObs.Text.Trim(),
                    Referencia = string.Empty,
                    Cantidad = 1,
                    Usuario = "INVE",
                    Status = 0,
                    CodigoBarra = codigoBarra,
                    Prenda = string.Empty,
                    Id = string.Empty,
                    Text = string.Empty,
                    Nombre = string.Empty,
                    Valor = false,
                    Imei = Util.EstatusImei ? Util.Imei : string.Empty
                };

                var baseUrl = Util.NormalizeServiceUrl(lblUrl.Text);
                using var client = Util.CreateHttpClient();
                if (!TomaFisicaAuthService.ConfigureAuthorization(client))
                {
                    await VolverAlLoginAsync("Debe iniciar sesión para registrar una toma física.");
                    return;
                }

                using var content = new StringContent(JsonConvert.SerializeObject(toma), Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(baseUrl + "/api/TomaFisica2", content);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await VolverAlLoginAsync("La sesión venció. Inicie sesión nuevamente.");
                    return;
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    Util.TryPlayAudioFile("no-valido.mp3");
                    await DisplayAlert("Toma Física", "Este número de Toma Física ya pertenece a otro usuario.", "Aceptar");
                    return;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode || responseContent.Trim() == "-1")
                {
                    Util.TryPlayAudioFile("no-valido.mp3");
                    await DisplayAlert("Error", "No se pudo procesar la Toma Física 2.", "Aceptar");
                    return;
                }

                await MostrarResultadoEscaneoAsync(numeroPagina, codigoBarra);
                txtCodigo.Text = string.Empty;
                txtCodigo.Focus();
            }
            catch (Exception fail)
            {
                Util.TryPlayAudioFile("no-valido.mp3");
                await DisplayAlert("Error", Util.NormalizeHttpExceptionMessage(fail), "Aceptar");
                txtCodigo.Text = string.Empty;
                txtCodigo.Focus();
            }
            finally
            {
                registrandoCodigo = false;
            }
        }

        private async Task MostrarResultadoEscaneoAsync(int numeroPagina, string codigoBarra)
        {
            LimpiarArticulo();
            var baseUrl = Util.NormalizeServiceUrl(lblUrl.Text);
            var imei = Util.EstatusImei ? Util.Imei : "*";
            using var client = Util.CreateHttpClient();
            if (!TomaFisicaAuthService.ConfigureAuthorization(client))
            {
                await VolverAlLoginAsync("Debe iniciar sesión para consultar el resultado del escaneo.");
                return;
            }

            var detalleUrl = baseUrl + "/api/TomaFisica2/byParameters?numeroPagina=" + numeroPagina
                + "&codigoBarra=" + Uri.EscapeDataString(codigoBarra)
                + "&imei=" + Uri.EscapeDataString(imei);
            using var detalleResponse = await client.GetAsync(detalleUrl);
            detalleResponse.EnsureSuccessStatusCode();
            var detalles = JArray.Parse(await detalleResponse.Content.ReadAsStringAsync());
            var cantidad = detalles.Sum(item => decimal.TryParse(item["Cantidad"]?.ToString(), NumberStyles.Number,
                CultureInfo.InvariantCulture, out var valor) ? valor : 0m);

            using var articuloResponse = await client.GetAsync(baseUrl + "/api/Articulo/" + Uri.EscapeDataString(codigoBarra));
            articuloResponse.EnsureSuccessStatusCode();
            var articuloJson = JToken.Parse(await articuloResponse.Content.ReadAsStringAsync());
            var articulo = articuloJson.Type == JTokenType.Array
                ? articuloJson.First?.ToObject<ArticuloEscaneado>()
                : articuloJson.ToObject<ArticuloEscaneado>();

            lblArticulo.Text = string.IsNullOrWhiteSpace(articulo?.Nombre)
                ? "Artículo sin nombre en INVENTARIO"
                : articulo.Nombre;
            lblExistencia.Text = cantidad.ToString("0.##", CultureInfo.InvariantCulture);
            EtiquetaVisible();
        }

        private async void LoadDetalle(object sender, EventArgs e)
        {
            if (!int.TryParse(txtPagina.Text, out var numeroPagina) || numeroPagina <= 0)
            {
                await FeedbackService.ShowAsync("Indique una página válida.");
                return;
            }

            if (string.IsNullOrWhiteSpace(lblUrl.Text))
            {
                await DisplayAlert("Advertencia", "Configure el servidor en Parámetros.", "Aceptar");
                return;
            }

            await Navigation.PushAsync(new TomaFisicaDetallePage(numeroPagina, lblUrl.Text, txtObs.Text ?? string.Empty, true));
        }

        private async Task VolverAlLoginAsync(string mensaje)
        {
            TomaFisicaAuthService.Clear();
            await DisplayAlert("Sesión", mensaje, "Aceptar");
            await Navigation.PopToRootAsync();
        }

        private void OnLimpiarClicked(object sender, EventArgs e)
        {
            txtPagina.Text = string.Empty;
            txtObs.Text = string.Empty;
            txtCodigo.Text = string.Empty;
            LimpiarArticulo();
            txtPagina.Focus();
        }

        private void EtiquetaVisible()
        {
            lblArt.IsVisible = true;
            lblArticulo.IsVisible = true;
            lblExis.IsVisible = true;
            lblExistencia.IsVisible = true;
        }

        private void EtiquetaInVisible()
        {
            lblArt.IsVisible = false;
            lblArticulo.IsVisible = false;
            lblExis.IsVisible = false;
            lblExistencia.IsVisible = false;
        }

        private void LimpiarArticulo()
        {
            lblArticulo.Text = string.Empty;
            lblExistencia.Text = string.Empty;
            EtiquetaInVisible();
        }
    }
}