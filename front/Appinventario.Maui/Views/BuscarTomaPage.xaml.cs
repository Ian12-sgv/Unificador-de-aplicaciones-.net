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

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

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
        private readonly bool esToma2;
        public string ImeiL;
        public bool EstatusImeiL = false;
       

        public BuscarTomaPage(string url, bool esToma2 = false)
        {
            InitializeComponent();
            this.esToma2 = esToma2;
            Title = "Cat\u00E1logo";
            this.ImeiL = Util.Imei;
            this.EstatusImeiL= Util.EstatusImei;
            this.urlL = url;
            LoadTomaFisica();
        }

         private async void Handle_ItemTapped(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.CurrentSelection.Count == 0)
                    return;

                var tomaSeleccionada = e.CurrentSelection.FirstOrDefault() as TomaFisica;
                if (tomaSeleccionada == null || !tomaSeleccionada.NumeroPagina.HasValue)
                    return;

                ListToma.SelectedItem = null;
                Util.NumPagG = tomaSeleccionada.NumeroPagina.Value.ToString();
                Util.ObsG = tomaSeleccionada.Observacion ?? string.Empty;
                await Navigation.PopAsync();
            }
            catch (Exception fail)
            {
                await DisplayAlert("Error", fail.Message, "Aceptar");
            }
        }
        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            var source = listT ?? new List<TomaFisica>();
            var query = (Search.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                ListToma.ItemsSource = source;
                return;
            }

            ListToma.ItemsSource = source
                .Where(x => x.NumeroPagina.ToString().Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
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
                var baseUrl = Util.NormalizeServiceUrl(this.urlL);
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    await DisplayAlert("Advertencia", "Configure el servidor en Parámetros.", "Aceptar");
                    return;
                }
                var route = esToma2 ? "/api/TomaFisica2/" : "/api/TomaFisica/";
                request.RequestUri = new Uri(baseUrl + route + imei);
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                var client = Util.CreateHttpClient();
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var resultado = JsonConvert.DeserializeObject<List<TomaFisica>>(content) ?? new List<TomaFisica>();
                    // La API puede devolver varios articulos del mismo numero.
                    // La lupa muestra una sola fila por numero de toma.
                    listT = resultado
                        .GroupBy(x => x.NumeroPagina)
                        .Select(group => group.First())
                        .OrderBy(x => x.NumeroPagina)
                        .ToList();
                    this.ListToma.ItemsSource = listT;

                }
                if (imei == "*")
                    imei = "";
            }
            catch (Exception fail)
            {
                await DisplayAlert("Error", fail.Message, "Aceptar");

            }
        }


    }
}

