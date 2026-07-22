using Appinventario.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Appinventario.Models;
using Newtonsoft.Json;
using System.Net.Http;
using Appinventario.Interfaces;
using System.Net;
using Appinventario.ViewModels;
using System.Collections.ObjectModel;


namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NuevaTransferenciaPage : ContentPage
    {

        SucursalesViewModel _viewModel;   
        public List<Sucursales> listSucur; 

        public string url = "192.168.0.176:8000";
        public ObservableCollection<Sucursales> Items { get; set; }
        public NuevaTransferenciaPage()
        {
            InitializeComponent();
            
            BindingContext = _viewModel = new SucursalesViewModel(); 
            if (_viewModel.SelectCommand.CanExecute(null))
               _viewModel.SelectCommand.Execute(null);
         

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            loadSucursales();
        }
        private async void loadSucursales()
        {
            try
            {
                if (String.IsNullOrWhiteSpace(this.url) == true)
                {
                    await DisplayAlert("Advertencia" + this.url, "No se ha configurado url del servicio en parámetros.", "ok");
                    return;
                }

                var request = new HttpRequestMessage();
                request.RequestUri = new Uri("http://" + this.url + "/api/Sucursales/" + "");
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                HttpResponseMessage response = await client.SendAsync(request);
                //Task.Delay(200).Wait();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    listSucur = JsonConvert.DeserializeObject<List<Sucursales>>(content);
                    listaRecibe.ItemsSource = listSucur;
                    listaEnvia.ItemsSource = listSucur;
                    Task.Delay(200).Wait();
                }

            }
            catch (Exception fail)
            {
                DependencyService.Get<IAudio>().PlayAudioFile("no-valido.mp3");
                await DisplayAlert("Error", "Error: " + fail.Message, "ok");
            }
        }

        private async void Guardar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MovTransferenciasPage());
            
        }
    }
}
