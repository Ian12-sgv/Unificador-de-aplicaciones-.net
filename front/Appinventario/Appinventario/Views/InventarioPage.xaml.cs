
using Appinventario.Models;
using Appinventario.Interfaces;
using Appinventario.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
//using Android.Views;
using Appinventario.Services;

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
                _viewModel.SelectCommand.Execute(null);
            EtiquetaInVisible();
        }

        private  void EtiquetaVisible()
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
              //  ResultLabel.Text = "You entered: " + BarcodeEntry.Text;
                BarcodeEntry.Text = string.Empty;
            }
           // DependencyService.Get<IKeyboardHelper>().HideKeyboard();
            BarcodeEntry.Focus();
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
                request.RequestUri = new Uri("http://" + lblUrl.Text + "/api/Articulo/" + BarcodeEntry.Text.Trim() /*txtCodigo.Text*/);
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                HttpResponseMessage response = await client.SendAsync(request);
                Task.Delay(100).Wait();

                //  if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError)
                if (response.StatusCode == HttpStatusCode.OK )
                {
                    string content = await response.Content.ReadAsStringAsync();
                    listArt = JsonConvert.DeserializeObject<List<Articulo>>(content);
                
                    foreach(var ls in listArt)
                    {
                        this.lblArticulo.Text = ls.Nombre;
                        this.lblExistencia.Text = ls.Existencia.ToString();
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
                    this.BarcodeEntry.Focus();
                    this.BarcodeEntry.Text = "";
                }
            }
            catch (Exception fail)
            {
                this.BarcodeEntry.Text = "";
                this.BarcodeEntry.Focus();
                DependencyService.Get<IAudio>().PlayAudioFile("no-valido.mp3");
                await DisplayAlert("Error", "Error: " + fail.Message, "ok");
            }
        }

       

    }
    }
