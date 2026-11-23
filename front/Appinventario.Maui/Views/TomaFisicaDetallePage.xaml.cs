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
        private readonly bool esToma2;

         public TomaFisicaDetallePage(int numeroPagina, string url,string Obs, bool esToma2 = false)
        {
            InitializeComponent();
            this.esToma2 = esToma2;
            Title = esToma2 ? "Detalle Toma F\u00EDsica 2" : "Detalle Toma F\u00EDsica 1";
            BindingContext = _viewModel = new TomaFisicaViewModel();
            this.numeroPaginaL = numeroPagina;
            this.urlL = url;
            this.obsL = Obs;
            LoadTomaFisica();
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
               
                var baseUrl = Util.NormalizeServiceUrl(this.urlL);
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    await DisplayAlert("Advertencia", "Configure el servidor en Parámetros.", "Aceptar");
                    return;
                }
                var detailRoute = esToma2 ? "/api/TomaFisicaDet2/byParameters" : "/api/TomaFisicaDet/byParameters";
                request.RequestUri = new Uri(baseUrl + detailRoute + "?numeroPagina=" + this.numeroPaginaL.ToString() + "&tipo=DETALLE&imei=" + imei);
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                var client = new HttpClient();
                //client.Timeout = TimeSpan.FromSeconds(2);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var resultado = DeserializeTomasFisicas(content);
                    this.ListTomaDetalle.ItemsSource = resultado;                    
                    LoadTotal();
                }
                if (imei == "*")
                    imei = "";
            }
            catch (Exception fail)
            {
                await DisplayAlert("Error", fail.Message, "Aceptar");
            }
        }
        private async void LoadVariables()
        {
            try
            {
                var baseUrl = Util.NormalizeServiceUrl(this.urlL);
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return;

                using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl + (esToma2 ? "/api/Valores2" : "/api/Valores"));
                request.Headers.Add("Accept", "application/json");
                using var client = Util.CreateHttpClient();
                var response = await client.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                    return;

                var content = await response.Content.ReadAsStringAsync();
                listValores = JsonConvert.DeserializeObject<List<Valores>>(content) ?? new List<Valores>();

                foreach (var ls in listValores)
                {
                    esEditableCantidad = ls.Descripcion != null && Convert.ToBoolean(ls.Descripcion);
                }
            }
            catch (Exception)
            {
                esEditableCantidad = false;
            }
        }
        private static List<TomaFisica> DeserializeTomasFisicas(string content)
        {
            var items = Newtonsoft.Json.Linq.JArray.Parse(content);
            foreach (var item in items)
            {
                var cantidadTexto = item["Cantidad"]?.ToString();
                if (decimal.TryParse(cantidadTexto, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var cantidad))
                {
                    item["Cantidad"] = (int)Math.Round(cantidad, 0, MidpointRounding.AwayFromZero);
                }
            }

            return items.ToObject<List<TomaFisica>>() ?? new List<TomaFisica>();
        }
        private async void LoadTotal()
        {
            try
            {
                string imei = Util.Imei;
                var request = new HttpRequestMessage();
                if (Util.EstatusImei == false)
                    imei = "*";

                var baseUrl = Util.NormalizeServiceUrl(this.urlL);
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return;
                var totalRoute = esToma2 ? "/api/TomaFisicaDet2/byParameters" : "/api/TomaFisicaDet/byParameters";
                request.RequestUri = new Uri(baseUrl + totalRoute + "?numeroPagina=" + this.numeroPaginaL.ToString() + "&tipo=TOTAL&imei=" + imei);
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                var client = Util.CreateHttpClient();
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    listToma = DeserializeTomasFisicas(content);

                    var totalCantidad = listToma.Sum(ls => ls.Cantidad);
                    this.lblExistencia.Text = "TOTAL CANTIDAD: " + totalCantidad;
                }
                if (imei == "*")
                    imei = "";
            }
            catch (Exception fail)
            {
                await DisplayAlert("Error", fail.Message, "Aceptar");
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
                    await DisplayAlert("Advertencia", "Configure el servidor en Parámetros.", "Aceptar");
                    return;
                }
                if (String.IsNullOrWhiteSpace(this.cantidadN))
                {
                    await FeedbackService.ShowAsync("Indique la cantidad.");
                    return;
                }
                if (Convert.ToInt32(this.cantidadN) > 50000)
                {
                    await FeedbackService.ShowAsync("La cantidad no puede superar 50000.");
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

                var baseUrl = Util.NormalizeServiceUrl(this.urlL);
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    await DisplayAlert("Advertencia", "Configure el servidor en Parámetros.", "Aceptar");
                    return;
                }
                var postRoute = esToma2 ? "/api/TomaFisica2" : "/api/TomaFisica";
                Uri RequestUri = new Uri(baseUrl + postRoute);
                var client = Util.CreateHttpClient();
                if (!TomaFisicaAuthService.ConfigureAuthorization(client))
                {
                    await VolverAlLoginAsync("Debe iniciar sesión para modificar la toma física.");
                    return;
                }
                var json = JsonConvert.SerializeObject(toma);
                var contentJson = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(RequestUri, contentJson);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await VolverAlLoginAsync("La sesión venció. Inicie sesión nuevamente.");
                    return;
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    LoadTomaFisica();
                }
                else
                {
                   await DisplayAlert("Error", "No se pudo guardar la cantidad. Intente de nuevo (código " + (int)response.StatusCode + ").", "Aceptar");
                   return;
                }
            }
            catch (Exception fail)
            {
                await DisplayAlert("Error", fail.Message, "Aceptar");
            }
        }

        private async System.Threading.Tasks.Task VolverAlLoginAsync(string mensaje)
        {
            TomaFisicaAuthService.Clear();
            await DisplayAlert("Sesión", mensaje, "Aceptar");
            await Navigation.PopToRootAsync();
        }




    }
}

