using Appinventario.Interfaces;
using Appinventario.Models;
using Appinventario.Services;
using Appinventario.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MovTransferenciasPage : ContentPage
    {
        ArticuloViewModel _viewModel;
        public List<Articulo> listArt;
        private readonly int numeroTransferencia;
        private readonly DateTime fechaTransferencia;
        private int item = 1;
        private string codigoBarraActual = string.Empty;
        private CancellationTokenSource _scanDebounce;
        private bool _loadingBarcode;

        public MovTransferenciasPage()
            : this(0, DateTime.Now)
        {
        }

        public MovTransferenciasPage(int numeroTransferencia, DateTime fechaTransferencia)
        {
            this.numeroTransferencia = numeroTransferencia;
            this.fechaTransferencia = fechaTransferencia;

            InitializeComponent();
            BindingContext = _viewModel = new ArticuloViewModel();
            if (_viewModel.SelectCommand.CanExecute(null))
            {
                _viewModel.SelectCommand.Execute(null);
            }

            EtiquetaInVisible();
        }

        private void EtiquetaVisible()
        {
            lblArt.IsVisible = true;
            lblArticulo.IsVisible = true;
            lblInserte.IsVisible = false;
            lbCant.IsVisible = true;
            lblCaja.IsVisible = true;
        }

        private void EtiquetaInVisible()
        {
            lblArt.IsVisible = false;
            lblArticulo.IsVisible = false;
            lblInserte.IsVisible = true;
            lbCant.IsVisible = false;
            lblCaja.IsVisible = false;
        }

        private async void BarcodeEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            _scanDebounce?.Cancel();
            var codigo = (e.NewTextValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(codigo) || _loadingBarcode)
                return;

            var cts = _scanDebounce = new CancellationTokenSource();
            try
            {
                await Task.Delay(250, cts.Token);
                if (!cts.IsCancellationRequested && string.Equals(codigo, (BarcodeEntry.Text ?? string.Empty).Trim(), StringComparison.Ordinal))
                    LoadInventario(sender, EventArgs.Empty);
            }
            catch (TaskCanceledException)
            {
            }
        }

        private async void LoadInventario(object sender, EventArgs e)
        {
            if (_loadingBarcode)
                return;
            _loadingBarcode = true;

try
            {
                if (string.IsNullOrWhiteSpace(lblUrl.Text))
                {
                    await DisplayAlert("Advertencia", "No se ha configurado url del servicio en parametros.", "ok");
                    return;
                }

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri("http://" + lblUrl.Text + "/api/Articulo/" + BarcodeEntry.Text.Trim()),
                    Method = HttpMethod.Get
                };
                request.Headers.Add("Accept", "application/json");

                var client = Util.CreateHttpClient();

                HttpResponseMessage response = await client.SendAsync(request);
                Task.Delay(100).Wait();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    listArt = JsonConvert.DeserializeObject<List<Articulo>>(content) ?? new List<Articulo>();

                    foreach (var ls in listArt)
                    {
                        lblArticulo.Text = ls.Nombre;
                    }

                    if (listArt.Count > 0)
                    {
                        codigoBarraActual = BarcodeEntry.Text.Trim();
                        EtiquetaVisible();
                    }
                    else
                    {
                        DependencyService.Get<IAudio>().PlayAudioFile("no-valido.mp3");
                        await DisplayAlert("Error", "Error de codigo de barra.", "ok");
                        EtiquetaInVisible();
                    }

                    BarcodeEntry.Focus();
                    BarcodeEntry.Text = string.Empty;
                }
            }
            catch (Exception fail)
            {
                BarcodeEntry.Text = string.Empty;
                BarcodeEntry.Focus();
                DependencyService.Get<IAudio>().PlayAudioFile("no-valido.mp3");
                await DisplayAlert("Error", "Error: " + fail.Message, "ok");
            }            finally
            {
                _loadingBarcode = false;
            }
        }

        private async void Agregar_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (numeroTransferencia <= 0)
                {
                    await DisplayAlert("Advertencia", "Primero debe crear la transferencia.", "ok");
                    return;
                }

                var serviceUrl = Util.NormalizeServiceUrl(lblUrl.Text);
                if (string.IsNullOrWhiteSpace(serviceUrl))
                {
                    await DisplayAlert("Advertencia", "No se ha configurado url del servicio en parametros.", "ok");
                    return;
                }

                if (string.IsNullOrWhiteSpace(codigoBarraActual))
                {
                    await DisplayAlert("Advertencia", "Debe leer un código de barra válido.", "ok");
                    return;
                }

                if (!int.TryParse(txtCantidad.Text, out var cantidad) || cantidad <= 0)
                {
                    await DisplayAlert("Advertencia", "Debe indicar una cantidad válida.", "ok");
                    return;
                }

                if (!int.TryParse(txtCaja.Text, out var numeroCaja))
                {
                    await DisplayAlert("Advertencia", "Debe indicar un número de caja válido.", "ok");
                    return;
                }

                var movimiento = new MovTransferencias
                {
                    Numero = numeroTransferencia,
                    Fecha = fechaTransferencia,
                    CodigoBarra = codigoBarraActual,
                    Cantidad = cantidad,
                    Valor = 0,
                    NumeroCaja = numeroCaja,
                    Item = item
                };

                var json = JsonConvert.SerializeObject(movimiento);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var client = Util.CreateHttpClient();
                var response = await client.PostAsync(serviceUrl + "/api/Transferencia/detalle", content);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    await DisplayAlert("Error", "No se pudo guardar el artículo.", "ok");
                    return;
                }

                var resultText = await response.Content.ReadAsStringAsync();
                if (!int.TryParse(resultText, out var result) || result < 1)
                {
                    await DisplayAlert("Error", "El escucha no insertó el artículo.", "ok");
                    return;
                }

                item++;
                codigoBarraActual = string.Empty;
                txtCantidad.Text = string.Empty;
                txtCaja.Text = string.Empty;
                lblArticulo.Text = string.Empty;
                EtiquetaInVisible();
                BarcodeEntry.Focus();
            }
            catch (Exception fail)
            {
                BarcodeEntry.Focus();
                await DisplayAlert("Error", "Error: " + Util.NormalizeHttpExceptionMessage(fail), "ok");
            }            finally
            {
                _loadingBarcode = false;
            }
        }
    }
}
