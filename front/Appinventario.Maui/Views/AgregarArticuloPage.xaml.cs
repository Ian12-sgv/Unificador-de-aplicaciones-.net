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
using System.Threading.Tasks;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AgregarArticuloPage : ContentPage
    {
        ArticuloViewModel _viewModel;
        public List<Articulo> listArt;

        public AgregarArticuloPage()
        {
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
            lblRefer.IsVisible = true;
            lblReferencia.IsVisible = true;
        }

        private void EtiquetaInVisible()
        {
            lblArt.IsVisible = false;
            lblArticulo.IsVisible = false;
            lblInserte.IsVisible = true;
            lblRefer.IsVisible = false;
            lblReferencia.IsVisible = false;
        }

        private async void LoadInventario(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(lblUrl.Text))
                {
                    await DisplayAlert("Advertencia", "Configure el servidor en Parámetros.", "Aceptar");
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

                    BarcodeEntry.Focus();
                    BarcodeEntry.Text = string.Empty;

                    foreach (var ls in listArt)
                    {
                        lblArticulo.Text = ls.CodigoBarra + " | " + ls.Nombre + " Marca: " + ls.CodigoMarca + " " + ls.NombreMarca;
                    }

                    if (listArt.Count > 0)
                    {
                        EtiquetaVisible();
                    }
                    else
                    {
                        DependencyService.Get<IAudio>().PlayAudioFile("no-valido.mp3");
                        await DisplayAlert("Error", "No se encontró ningún artículo con ese código.", "Aceptar");
                        EtiquetaInVisible();
                    }

                    BarcodeEntry.Text = string.Empty;
                    BarcodeEntry.Focus();
                }
            }
            catch (Exception fail)
            {
                BarcodeEntry.Text = string.Empty;
                DependencyService.Get<IAudio>().PlayAudioFile("no-valido.mp3");
                await DisplayAlert("Error", fail.Message, "Aceptar");
                BarcodeEntry.Focus();
            }
        }
    }
}
