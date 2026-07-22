using Appinventario.Models;
using Appinventario.Services;
using Appinventario.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BuscarTomaPage : ContentPage
    {
       // TomaFisicaViewModel _viewModel;
        public ObservableCollection<TomaFisica> Items { get; set; }
        public TomaFisica tomaFisica;
        public List<TomaFisica> listT;
        public string urlL;
        public string ImeiL;
        public bool EstatusImeiL = false;
       

        public BuscarTomaPage(string url)
        {
            InitializeComponent();
            this.ImeiL = Util.Imei;
            this.EstatusImeiL= Util.EstatusImei;
            this.urlL = url;
            LoadTomaFisica();
        }

         void Handle_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            try
            {
                if (e.Item == null)
                    return;

                var m = (TomaFisica)e.Item;

                Util.NumPagG = m.NumeroPagina.ToString();
                Util.ObsG = m.Observacion;
                Shell.Current.GoToAsync("..");
            }
            catch (Exception fail)
            {
                DisplayAlert("Error", "Error: " + fail.Message, "ok");
            }
            //wait DisplayAlert("Item Tapped", m.NumeroPagina.ToString(), "OK");
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Search.Text !="")
            {
                var search = listT.Where(x => x.NumeroPagina.ToString() == Search.Text);
                this.ListToma.ItemsSource = search;
            }
            else 
            {
                this.ListToma.ItemsSource = listT;
            }
        }

        private async void LoadTomaFisica()
        {
            try
            {
                string imei = ImeiL;
                var request = new HttpRequestMessage();
                //if (Util.EstatusImei == false)
                //     Util.Imei = "*";
                if (EstatusImeiL == false)
                    imei = "*";
                else
                    imei = Util.Imei;
                request.RequestUri = new Uri("http://" + this.urlL + "/api/TomaFisica/" + imei);
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var resultado = JsonConvert.DeserializeObject<List<TomaFisica>>(content);
                    listT = resultado;
                    this.ListToma.ItemsSource = resultado;

                }
                if (imei == "*")
                    imei = "";
            }
            catch (Exception fail)
            {
                await DisplayAlert("Error", "Error: " + fail.Message, "ok");

            }
        }


    }
}
