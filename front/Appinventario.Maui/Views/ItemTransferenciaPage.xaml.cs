using Appinventario.Interfaces;
using Appinventario.Models;
using Appinventario.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System.Runtime.CompilerServices;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ItemTransferenciaPage : ContentPage
    {

        TransferenciaViewModel _viewModel;   
        public List<Sucursales> listSucursal;  //Hacer lista de Sucursales
        
        public string url = "192.168.10.139:8000";
        public string ImeiL;
        public bool EstatusImeiL = false;

        public ItemTransferenciaPage()
        {
            InitializeComponent();
            BindingContext = _viewModel = new TransferenciaViewModel(); 
            if (_viewModel.SelectCommand.CanExecute(null))
                _viewModel.SelectCommand.Execute(null);
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadSucursal();

        }
        private async void LoadSucursal()
        {
            try
            {
                if (String.IsNullOrWhiteSpace(this.url) == true)
                {
                    await DisplayAlert("Advertencia" + this.url, "No se ha configurado url del servicio en parámetros.", "ok");
                    return;
                }

                var request = new HttpRequestMessage();
                request.RequestUri = new Uri("http://" + this.url + "/api/Transferencia/" + "3300000038032");
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                var client = Appinventario.Services.Util.CreateHttpClient();
                HttpResponseMessage response = await client.SendAsync(request);
                Task.Delay(200).Wait();

                //  if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError)
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    listSucursal = JsonConvert.DeserializeObject<List<Sucursales>>(content);
                    //listaTranferencia.ItemsSource = listSucursal;
                    //Task.Delay(200).Wait();
                }

            }
            catch (Exception fail)
            {
                DependencyService.Get<IAudio>().PlayAudioFile("no-valido.mp3");
                await DisplayAlert("Error", "Error: " + fail.Message, "ok");
            }
        }

    }
}


        
