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
    public partial class InventarioPage : ContentPage
    {
        ArticuloViewModel _viewModel;
        public List<Articulo> listArt;

        public InventarioPage()
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
            lblExis.IsVisible = true;
            lblExistencia.IsVisible = true;
            lblInserte.IsVisible = false;
        }

        private void EtiquetaInVisible()
        {
            lblArt.IsVisible = false;
            lblArticulo.IsVisible = false;
            lblExis.IsVisible = false;
            lblExistencia.IsVisible = false;
            lblInserte.IsVisible = true;
        }

        private void BarcodeEntryCompleted(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(BarcodeEntry.Text))
            {
                BarcodeEntry.Text = string.Empty;
            }

            BarcodeEntry.Focus();
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

                    foreach (var ls in listArt)
                    {
                        lblArticulo.Text = ls.Nombre;
                        lblExistencia.Text = ls.Existencia.ToString();
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

                    BarcodeEntry.Focus();
                    BarcodeEntry.Text = string.Empty;
                }
            }
            catch (Exception fail)
            {
                BarcodeEntry.Text = string.Empty;
                BarcodeEntry.Focus();
                DependencyService.Get<IAudio>().PlayAudioFile("no-valido.mp3");
                await DisplayAlert("Error", fail.Message, "Aceptar");
            }
        }
    }
}
