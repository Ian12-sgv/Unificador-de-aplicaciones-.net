using Appinventario.Interfaces;     
using Appinventario.Models;         
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Appinventario.ViewModels;
using System.Collections;
using System.Collections.ObjectModel;
using Appinventario.Services;
using System.Security.Cryptography.X509Certificates;
using Appinventario.Data;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConsultarTransferenciaPage : ContentPage
    {
        TransferenciaViewModel _viewModel;
        public List<Transferencia> listTranfer;
        public string[] listaTemp = {};

        //public string url = "192.168.0.176:8000";
        public string ImeiL;
        public bool EstatusImeiL = false;
        //Omitidi por angel para prueba
        //public ObservableCollection<Transferencia> Items { get; set; }
        public ConsultarTransferenciaPage()

        {
            InitializeComponent();
            BindingContext = _viewModel = new TransferenciaViewModel();
            if (_viewModel.SelectCommand.CanExecute(null))
                _viewModel.SelectCommand.Execute(null);
            
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadTransferencia();

        }
        private async void LoadTransferencia()
        {
            try
            {
                if (String.IsNullOrWhiteSpace(this.lblUrl.Text) == true)
                {
                    await DisplayAlert("Advertencia", "No se ha configurado url del servicio en parámetros. IP:" + lblUrl.Text, "ok");
                    return;
                }


                var request = new HttpRequestMessage();
                //await DisplayAlert("Titulo", "Advertencia " + lblUrl.Text + "", "ok");
                
                request.RequestUri = new Uri("http://" + lblUrl.Text + "/api/Transferencia/" + "3300000038032");
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                HttpResponseMessage response = await client.SendAsync(request);
                //Task.Delay(100).Wait();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    listTranfer = JsonConvert.DeserializeObject<List<Transferencia>>(content);
                    listaTranferencia.ItemsSource = listTranfer;
                    Task.Delay(200).Wait();
                    //await DisplayAlert("Entre",listTranfer[1].ToString() , "ok");
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