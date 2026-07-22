using Appinventario.Models;
using Appinventario.Services;
using Appinventario.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PrecioPage : ContentPage
    {
        private readonly ArticuloViewModel _viewModel;
        private readonly HttpClient _httpClient;
        public List<Articulo> listArt;

        public PrecioPage()
        {
            InitializeComponent();
            BindingContext = _viewModel = new ArticuloViewModel();
            _httpClient = Util.CreateHttpClient();
            EtiquetaInVisible();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                await _viewModel.LoadAsync();
            }
            catch (Exception ex)
            {
                await CrashReportService.LogExceptionAsync(ex, "PrecioPage.OnAppearing");
            }

            BarcodeEntry.Focus();
        }

        private void EtiquetaVisible()
        {
            lblArt.IsVisible = true;
            lblArticulo.IsVisible = true;
            lblInserte.IsVisible = false;

            lblExi.IsVisible = true;
            lblExistencia.IsVisible = true;

            lblPrec.IsVisible = true;
            lblPrecio.IsVisible = true;
            lblDol.IsVisible = true;
            lblDolar.IsVisible = true;
        }

        private void EtiquetaInVisible()
        {
            lblArt.IsVisible = false;
            lblArticulo.IsVisible = false;
            lblInserte.IsVisible = true;

            lblExi.IsVisible = false;
            lblExistencia.IsVisible = false;

            lblPrec.IsVisible = false;
            lblPrecio.IsVisible = false;
            lblDol.IsVisible = false;
            lblDolar.IsVisible = false;
            lblPromo.IsVisible = false;
            lblPromocion.IsVisible = false;
            lblPorcentaje.IsVisible = false;
        }

        private async void LoadInventario(object sender, EventArgs e)
        {
            var codigoBarra = (BarcodeEntry.Text ?? string.Empty).Trim();
            var baseUrl = Util.NormalizeServiceUrl(lblUrl.Text);

            try
            {
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    await DisplayAlert("Advertencia", "No se ha configurado url del servicio en parametros.", "ok");
                    return;
                }

                if (string.IsNullOrWhiteSpace(codigoBarra))
                {
                    await DisplayAlert("Advertencia", "Debe insertar codigo de barra.", "ok");
                    return;
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUrl + "/api/Articulo/" + Uri.EscapeDataString(codigoBarra)));
                request.Headers.Add("Accept", "application/json");

                using var response = await _httpClient.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    string errorMessage = "No fue posible consultar el precio. Verifica el escucha o la conexion.";
                    string errorContent = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrWhiteSpace(errorContent))
                    {
                        try
                        {
                            var payload = JObject.Parse(errorContent);
                            var backendMessage = payload.Value<string>("Message");
                            var backendDetail = payload.Value<string>("Detail");

                            if (!string.IsNullOrWhiteSpace(backendDetail))
                            {
                                errorMessage = backendDetail;
                            }
                            else if (!string.IsNullOrWhiteSpace(backendMessage))
                            {
                                errorMessage = backendMessage;
                            }
                        }
                        catch
                        {
                            // Si el backend devuelve texto plano o HTML, se mantiene el mensaje generico.
                        }
                    }

                    EtiquetaInVisible();
                    Util.TryPlayAudioFile("no-valido.mp3");
                    await DisplayAlert("Informacion", errorMessage, "ok");
                    return;
                }

                string content = await response.Content.ReadAsStringAsync();
                listArt = JsonConvert.DeserializeObject<List<Articulo>>(content) ?? new List<Articulo>();

                if (listArt.Count == 0)
                {
                    Util.TryPlayAudioFile("no-valido.mp3");
                    await DisplayAlert("Error", "Error de codigo de barra.", "ok");
                    EtiquetaInVisible();
                    return;
                }

                foreach (var ls in listArt)
                {
                    lblArticulo.Text = ls.Nombre;
                    lblExistencia.Text = "Existencia: " + Math.Round(ls.Existencia, 2).ToString("0.##");
                    lblPrecio.Text = "Precio Deatal: " + Math.Round(ls.PrecioDetal, 2).ToString("0.00");
                    lblDolar.Text = "Precio Dolar: " + Math.Round(ls.PrecioDolaresDetal, 2).ToString("0.00");
                    lblPorcentaje.Text = "Descuento: " + Math.Round(ls.PorcentajeDescuento, 2).ToString("0") + "%";
                    lblPromocion.Text = "Promocion: " + Math.Round(ls.PrecioPromocion, 2).ToString("0.00");

                    var tienePromocion = ls.PrecioPromocion > 0;
                    lblPromo.IsVisible = tienePromocion;
                    lblPromocion.IsVisible = tienePromocion;
                    lblPorcentaje.IsVisible = tienePromocion;
                }

                EtiquetaVisible();
            }
            catch (Exception fail)
            {
                EtiquetaInVisible();
                Util.TryPlayAudioFile("no-valido.mp3");
                await DisplayAlert("Error", "Error: " + Util.NormalizeHttpExceptionMessage(fail, "El escucha tardó demasiado en responder. Intenta de nuevo."), "ok");
            }
            finally
            {
                BarcodeEntry.Text = string.Empty;
                BarcodeEntry.Focus();
            }
        }
    }
}
