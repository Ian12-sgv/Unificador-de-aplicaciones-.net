using Appinventario.Models;
using Appinventario.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.ApplicationModel;
using System.Text;
using Appinventario.Services;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TomaFisicaDetallePage : ContentPage
    {

        TomaFisicaViewModel _viewModel;
        public List<TomaFisica> listToma;
        public List<Valores> listValores;
        public ObservableCollection<TomaFisica>  contentList = new ObservableCollection<TomaFisica>();
        public int numeroPaginaL = 0;
        public string urlL = "";
        public string cantidadN;
        public string obsL = "";
        public string codigoBarra;
        public bool esEditableCantidad= false;

         public TomaFisicaDetallePage(int numeroPagina, string url,string Obs)
        {
            InitializeComponent();
            BindingContext = _viewModel = new TomaFisicaViewModel();
            this.numeroPaginaL = numeroPagina;
            this.urlL = url;
            this.obsL = Obs;
            LoadTomaFisica();
            this.ListTomaDetalle.SelectionChanged += itemClicked;
            LoadVariables();
        }

        private async void LoadTomaFisica()
        {
            try
            {
                string imei = Util.Imei; 
                var request = new HttpRequestMessage();
                if (Util.EstatusImei == false)
                    imei = "*";
               
                request.RequestUri = new Uri("http://" + this.urlL + "/api/TomaFisicaDet/byParameters?numeroPagina=" + this.numeroPaginaL.ToString() + "&tipo=DETALLE&" + "imei=" + imei);
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                var client = new HttpClient();
                //client.Timeout = TimeSpan.FromSeconds(2);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var resultado = JsonConvert.DeserializeObject<List<TomaFisica>>(content);
                    this.ListTomaDetalle.ItemsSource = resultado;                    
                    LoadTotal();
                }
                if (imei == "*")
                    imei = "";
            }
            catch (Exception fail)
            {
                await DisplayAlert("Error", "Error: " + fail.Message, "ok");
            }
        }
        private async void LoadVariables()
        {
            var request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://" + this.urlL + "/api/Valores");
            request.Method = HttpMethod.Get;
            request.Headers.Add("Accept", "application/json");
            var client = new HttpClient();
            //client.Timeout = TimeSpan.FromSeconds(2);
            HttpResponseMessage response = await client.SendAsync(request);
            string content = await response.Content.ReadAsStringAsync();
            listValores = JsonConvert.DeserializeObject<List<Valores>>(content);
            try
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    foreach (var ls in listValores)
                    {
                        if (ls.Descripcion != null) 
                            esEditableCantidad = Convert.ToBoolean(ls.Descripcion);
                        else
                            esEditableCantidad = false;
                    }
                }
            }
            catch (Exception fail)
            {
                foreach (var ls in listValores)
                {
                   esEditableCantidad = false;
                }
                //  await DisplayAlert("Error", "Error: " + fail.Message, "ok");
            }
        }
        private async void LoadTotal()
        {
            try
            {
                string imei = Util.Imei;
                var request = new HttpRequestMessage();
                if (Util.EstatusImei == false)
                    imei = "*";

                request.RequestUri = new Uri("http://" + this.urlL + "/api/TomaFisicaDet/byParameters?numeroPagina=" + this.numeroPaginaL.ToString() + "&tipo=TOTAL&imei=" + imei);
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                var client = Util.CreateHttpClient();
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    listToma = JsonConvert.DeserializeObject<List<TomaFisica>>(content);

                    foreach (var ls in listToma)
                    {
                        this.lblExistencia.Text = "TOTAL CANTIDAD:  " + ls.Cantidad.ToString();
                    }
                }
                if (imei == "*")
                    imei = "";
            }
            catch (Exception fail)
            {
                await DisplayAlert("Error", "Error: " + fail.Message, "ok");
                return;
            }
        }

        private async void itemClicked(object sender, SelectionChangedEventArgs e)
        {
            if (esEditableCantidad == false)
                return;
            if (e.CurrentSelection.Count == 0)
                return;

            var itemclicked = e.CurrentSelection.FirstOrDefault();
            TomaFisica tomaFisica1 = (TomaFisica)itemclicked;

                this.codigoBarra = tomaFisica1.CodigoBarra;
                this.cantidadN = await DisplayPromptAsync("Cantidad",
                                                          "Inserte la cantidad",
                                                          placeholder: tomaFisica1.Cantidad.ToString(),
                                                          maxLength: 10,
                                                          keyboard: Keyboard.Numeric
                                                          );
              Guardar();
              ListTomaDetalle.SelectedItem = null;
          
        }

        private async void Guardar()
        {
            try
            {
                if (String.IsNullOrWhiteSpace(this.urlL))
                {
                    await DisplayAlert("Advertencia", "No hay servidor configurado.", "ok");
                    return;
                }
                if (String.IsNullOrWhiteSpace(this.cantidadN))
                {
                    await DisplayAlert("Advertencia", "Debe insertar cantidad.", "ok");
                    return;
                }
                if (Convert.ToInt32(this.cantidadN) > 50000)
                {
                    await DisplayAlert("Advertencia", "No puede insertar cantidad mayor a 50000.", "ok");
                    return;
                }

                TomaFisica toma = new TomaFisica
                {
                    NumeroPagina = Convert.ToInt32(this.numeroPaginaL),
                    Fecha = DateTime.Now,
                    Observacion = this.obsL,
                    Referencia = "",
                    Cantidad = Convert.ToInt32(this.cantidadN),
                    Usuario = "INVE",
                    Status = 0,
                    CodigoBarra = this.codigoBarra,
                    Prenda = "",
                    Id = "",
                    Text = "",
                    Nombre = "",                    
                    Valor = true,
                    Imei = Util.EstatusImei == true? Util.Imei: ""
                };

                Uri RequestUri = new Uri("http://" + this.urlL + "/api/TomaFisica");
                var client = Util.CreateHttpClient();
                var json = JsonConvert.SerializeObject(toma);
                var contentJson = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(RequestUri, contentJson);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    LoadTomaFisica();
                }
                else
                {
                   await DisplayAlert("Error", response.StatusCode.ToString(), "ok");
                   return;
                }
            }
            catch (Exception fail)
            {
                await DisplayAlert("Error", "Error: " + fail.Message, "ok");
            }
        }




    }
}

