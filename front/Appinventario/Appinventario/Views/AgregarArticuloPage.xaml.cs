using Appinventario.Interfaces;
using Appinventario.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Appinventario.ViewModels;

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
                _viewModel.SelectCommand.Execute(null);
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
                if (String.IsNullOrWhiteSpace(this.lblUrl.Text) == true)
                {
                    await DisplayAlert("Advertencia", "No se ha configurado url del servicio en parámetros.", "ok");
                    return;
                }
                var request = new HttpRequestMessage();
                request.RequestUri = new Uri("http://" + lblUrl.Text + "/api/Articulo/" + BarcodeEntry.Text.Trim());
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                HttpResponseMessage response = await client.SendAsync(request);
                Task.Delay(100).Wait();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    listArt = JsonConvert.DeserializeObject<List<Articulo>>(content);

                    this.BarcodeEntry.Focus();
                    this.BarcodeEntry.Text = "";

                    foreach (var ls in listArt)
                    {
                        this.lblArticulo.Text = ls.CodigoBarra + " | " + ls.Nombre + " Marca: " + ls.CodigoMarca + " " + ls.NombreMarca;
                    }
                    if (listArt.Count > 0)
                    {
                        EtiquetaVisible();
                    }
                    else
                    {
                        DependencyService.Get<IAudio>().PlayAudioFile("no-valido.mp3");
                        await DisplayAlert("Información", "Código no existe", "ok");
                        EtiquetaInVisible();
                    }
                    this.BarcodeEntry.Text = "";
                    this.BarcodeEntry.Focus();
                    
                }
            }
            catch (Exception fail)
            {
                this.BarcodeEntry.Text = "";
                DependencyService.Get<IAudio>().PlayAudioFile("no-valido.mp3");
                await DisplayAlert("Error", "Error: " + fail.Message, "ok");
                this.BarcodeEntry.Focus();
            }
        }


    }
}