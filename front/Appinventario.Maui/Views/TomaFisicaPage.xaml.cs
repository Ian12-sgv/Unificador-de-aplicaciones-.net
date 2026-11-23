using Appinventario.Models;
using Appinventario.ViewModels;
using Appinventario.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Maui.Controls;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Appinventario.Interfaces;
using Appinventario.Services;
using Microsoft.Maui.ApplicationModel;

namespace Appinventario.Views
{
    public partial class ItemsPage : ContentPage
    {
        TomaFisica toma;
        TomaFisicaViewModel _viewModel;
        public List<TomaFisica> listToma;
        string ImeiL = "";
        private CancellationTokenSource _scanDebounce;
        private bool _registrandoCodigo;
        

        public ItemsPage()
        {
            InitializeComponent();
            toma = new TomaFisica();
            BindingContext = _viewModel = new TomaFisicaViewModel();
            if (_viewModel.SelectCommand.CanExecute(null))
                _viewModel.SelectCommand.Execute(null);
            EtiquetaInVisible();
            ImeiL =  Util.Imei;
            
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();
            if (!AuthService.CanUseTomaFisica1() || !TomaFisicaAuthService.IsAuthenticated)
            {
                await Navigation.PopToRootAsync();
                return;
            }
            _viewModel.OnSelect();

            //***Valida que en parametros este activado o no este atributo
            if (Util.NumPagG.Length > 0)
            {
                this.txtPagina.Text = Services.Util.NumPagG;
                Util.NumPagG = "";
            }

            if (Util.ObsG.Length > 0)
            {
                this.txtObs.Text = Services.Util.ObsG;
                Util.ObsG = "";
            }
            SelectTextCodigo();
            txtCodigo.Text = "";

        }


        private async void CodigoTextChanged(object sender, TextChangedEventArgs e)
        {
            _scanDebounce?.Cancel();
            var codigo = (e.NewTextValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(codigo) || _registrandoCodigo)
                return;

            var cts = _scanDebounce = new CancellationTokenSource();
            try
            {
                await Task.Delay(250, cts.Token);
                if (!cts.IsCancellationRequested && string.Equals(codigo, (txtCodigo.Text ?? string.Empty).Trim(), StringComparison.Ordinal))
                    OnButtonClicked(sender, EventArgs.Empty);
            }
            catch (TaskCanceledException)
            {
            }
        }
        private async void OnButtonClicked(object sender, EventArgs e)
        {
            if (_registrandoCodigo)
                return;
            _registrandoCodigo = true;
            try
            {
                bool result = Int32.TryParse(this.txtPagina.Text, out int n);
                var viewModel = (TomaFisicaViewModel)BindingContext;
                this.lblArticulo.Text = "";
                this.lblExistencia.Text = "";

                if (String.IsNullOrWhiteSpace(this.lblUrl.Text))
                {
                    await DisplayAlert("Advertencia", "Configure el servidor en Parámetros.", "Aceptar");
                    return;
                }

                if (String.IsNullOrWhiteSpace(this.txtPagina.Text))
                {
                    await FeedbackService.ShowAsync("Indique el número de página.");
                    this.txtCodigo.Text = "";
                    this.txtPagina.Focus();
                    return;
                }
                if (String.IsNullOrWhiteSpace(this.txtObs.Text))
                {
                    await FeedbackService.ShowAsync("Escriba una observación.");
                    this.txtCodigo.Text = "";
                    this.txtObs.Focus();
                    return;
                }

                if (!result)
                {
                    await FeedbackService.ShowAsync("La página solo admite números.");
                    this.txtCodigo.Text = "";
                    this.txtPagina.Text = "";
                    this.txtPagina.Focus();
                    return ;
                }

                if ( Convert.ToInt32(this.txtPagina.Text) > 50000)
                {
                    await FeedbackService.ShowAsync("La página debe ser un número entre 1 y 50000.");
                    this.txtPagina.Text = "";
                    this.txtCodigo.Text = "";
                    this.txtPagina.Focus();
                    return;
                }
                
                if (String.IsNullOrWhiteSpace(this.txtCodigo.Text))
                {
                    await FeedbackService.ShowAsync("Escanee o escriba un código de barra.");
                    this.txtCodigo.Focus();
                    return;
                }
               
                 toma = new TomaFisica
                {
                    NumeroPagina = _viewModel.NumeroPagina,
                    Fecha = DateTime.Now,
                    Observacion = _viewModel.Observacion,
                    Referencia = "",
                    Cantidad = 1,
                    Usuario = "INVE",
                    Status = 0,
                    CodigoBarra = _viewModel.CodigoBarra.Trim(),
                    Prenda = "",
                    Id = "",
                    Text = "",
                    Nombre = "",
                    Valor = false,
                    Imei = Util.EstatusImei == true ? Util.Imei : ""
                 };
              
                var baseUrl = Util.NormalizeServiceUrl(this.lblUrl.Text);
                Uri RequestUri = new Uri(baseUrl + "/api/TomaFisica");
                using var client = Util.CreateHttpClient();
                if (!TomaFisicaAuthService.ConfigureAuthorization(client))
                {
                    await VolverAlLoginAsync("Debe iniciar sesion para registrar una toma fisica.");
                    return;
                }
                var json = JsonConvert.SerializeObject(toma);
                using var contentJson = new StringContent(json,Encoding.UTF8,"application/json");
                using var response = await client.PostAsync(RequestUri,contentJson);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Token vencido o backend reiniciado: no tiene sentido seguir escaneando.
                    Util.TryPlayAudioFile("no-valido.mp3");
                    await VolverAlLoginAsync("La sesion vencio. Inicie sesion nuevamente.");
                    return;
                }

             //   if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError)
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                   
                     if (content == "-1")
                    {
                        Util.TryPlayAudioFile("no-valido.mp3");
                        await DisplayAlert("Información", "Código de barra no existe o Toma física existente en otro dispositivo.", "Aceptar");
                        SelectTextCodigo();
                        txtCodigo.Text = "";
                        txtCodigo.Focus();
                        return;
                    }

                    await MostrarResultadoEscaneoAsync(Convert.ToInt32(toma.NumeroPagina), toma.CodigoBarra);
                    SelectTextCodigo();
                    txtCodigo.Text = "";
                    txtCodigo.Focus();
                    return;
                    }
                    else
                    {
                        Util.TryPlayAudioFile("no-valido.mp3");
                        var mensaje = response.StatusCode == HttpStatusCode.Conflict
                            ? "Este número de Toma Física ya pertenece a otro usuario."
                            : "El servidor respondió con error (código " + (int)response.StatusCode + ").";
                        await DisplayAlert("Toma Física", mensaje, "Aceptar");
                        SelectTextCodigo();
                        txtCodigo.Text = "";
                        txtCodigo.Focus();
                        return;
                    }
            }
            catch (Exception fail)
            {
                Util.TryPlayAudioFile("no-valido.mp3");
                await DisplayAlert("Error", fail.Message, "Aceptar");
                txtCodigo.Text = "";
                txtCodigo.Focus();
                SelectTextCodigo();
            }
            finally
            {
                _registrandoCodigo = false;
            }
        }

        private sealed class ArticuloEscaneado
        {
            public string Nombre { get; set; }
        }

        private async Task MostrarResultadoEscaneoAsync(int numeroPagina, string codigoBarra)
        {
            LimpiarArticulo();
            var baseUrl = Util.NormalizeServiceUrl(lblUrl.Text);
            var imei = Util.EstatusImei ? Util.Imei : "*";

            using var client = Util.CreateHttpClient();
            if (!TomaFisicaAuthService.ConfigureAuthorization(client))
            {
                await VolverAlLoginAsync("Debe iniciar sesion para consultar el resultado del escaneo.");
                return;
            }

            var tomaUrl = baseUrl + "/api/TomaFisica/byParameters?numeroPagina=" + numeroPagina
                + "&codigoBarra=" + Uri.EscapeDataString(codigoBarra)
                + "&imei=" + Uri.EscapeDataString(imei);
            using var tomaResponse = await client.GetAsync(tomaUrl);
            if (tomaResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                await VolverAlLoginAsync("La sesion vencio. Inicie sesion nuevamente.");
                return;
            }
            tomaResponse.EnsureSuccessStatusCode();

            var tomas = Newtonsoft.Json.Linq.JArray.Parse(await tomaResponse.Content.ReadAsStringAsync());
            var cantidad = tomas.Sum(item =>
            {
                var valor = item["Cantidad"]?.ToString();
                return decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var cantidadLeida)
                    ? cantidadLeida
                    : 0m;
            });

            using var articuloResponse = await client.GetAsync(baseUrl + "/api/Articulo/" + Uri.EscapeDataString(codigoBarra));
            articuloResponse.EnsureSuccessStatusCode();
            var articuloJson = Newtonsoft.Json.Linq.JToken.Parse(await articuloResponse.Content.ReadAsStringAsync());
            var articulo = articuloJson.Type == Newtonsoft.Json.Linq.JTokenType.Array
                ? articuloJson.First?.ToObject<ArticuloEscaneado>()
                : articuloJson.ToObject<ArticuloEscaneado>();

            lblArticulo.Text = string.IsNullOrWhiteSpace(articulo?.Nombre)
                ? "Articulo sin nombre en INVENTARIO"
                : articulo.Nombre;
            lblExistencia.Text = cantidad.ToString("0.##", CultureInfo.InvariantCulture);
            EtiquetaVisible();
        }

        private async Task VolverAlLoginAsync(string mensaje)
        {
            TomaFisicaAuthService.Clear();
            await DisplayAlert("Sesión", mensaje, "Aceptar");
            await Navigation.PopToRootAsync();
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
            lblArticulo.Text = "";
            lblExistencia.Text = "";
            EtiquetaInVisible();
        }
        private void SelectTextCodigo()
        {
            try
            {
                if (txtCodigo.Text.Length > 0)
                {
                    txtCodigo.CursorPosition = 0;
                    txtCodigo.SelectionLength = txtCodigo.Text.Length;
                }
                this.txtCodigo.Focus();
            }
            catch
            {
                this.txtCodigo.Focus();
                return;
            }
        }

        private async void OnButtonClean(object sender, EventArgs e)
        {
            try
            {
                this.txtPagina.Text = "";
                this.txtCodigo.Text = "";
                this.txtObs.Text = "";             
                EtiquetaInVisible();
            }
            catch (Exception fail)
            {
                await DisplayAlert("Error", fail.Message, "Aceptar");
            }
        }

        private  void SearchCatalogo(object sender, EventArgs e)
        {
            try
            {
                LimpiarArticulo();
                if (String.IsNullOrWhiteSpace( this.lblUrl.Text) == true)
                {
                    DisplayAlert("Advertencia", "Configure el servidor en Parámetros.", "Aceptar");
                    return;
                }
                this.Navigation.PushAsync(new BuscarTomaPage(this.lblUrl.Text));
            }
            catch (Exception fail)
            {
                 DisplayAlert("Error", fail.Message, "Aceptar");
            }
        }

        private void LoadDetalle(object sender, EventArgs e)
        {
            try
            {
                bool result = Int32.TryParse(this.txtPagina.Text, out int n);

                if (String.IsNullOrWhiteSpace(this.lblUrl.Text) == true)
                {
                    DisplayAlert("Advertencia", "Configure el servidor en Parámetros.", "Aceptar");
                    return;
                }
                if (String.IsNullOrWhiteSpace(this.txtPagina.Text))
                {
                    _ = FeedbackService.ShowAsync("Indique el número de página.");
                    this.txtPagina.Focus();
                    return;
                }
                if (!result)
                {
                    _ = FeedbackService.ShowAsync("La página solo admite números.");
                    txtPagina.Text = "";
                    this.txtPagina.Focus();
                    return;
                }
                if (Convert.ToInt32(this.txtPagina.Text) > 50000)
                {
                    _ = FeedbackService.ShowAsync("La página debe ser un número entre 1 y 50000.");
                    this.txtPagina.Focus();
                    return;
                }
                this.Navigation.PushAsync(new TomaFisicaDetallePage(Convert.ToInt32(this.txtPagina.Text),
                                                                                    this.lblUrl.Text,
                                                                                    this.txtObs.Text));

                // var modelo = DeviceInfo.
              //  var modelo = CrossDeviceInfo.Current
            }
            catch (Exception fail)
            {
                DisplayAlert("Error", fail.Message, "Aceptar");
            }

        }
        


    }
}
