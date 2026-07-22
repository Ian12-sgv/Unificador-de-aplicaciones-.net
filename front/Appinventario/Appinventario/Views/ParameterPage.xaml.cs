using Appinventario.Models;
using Appinventario.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Appinventario.Services;

namespace Appinventario.Views
{
    public partial class NewItemPage : ContentPage
    {
        Parameter parameter;
        public NewItemPage()
        {
            InitializeComponent();
            BindingContext = new ParameterViewModel();

             parameter =  new Parameter();
            //Llama el metodo para cargar por defecto la data de la url guardada en sqllite
            var viewModel = (ParameterViewModel)BindingContext;
            if (viewModel.SelectCommand.CanExecute(null))
                viewModel.SelectCommand.Execute(null);
            
        }

        private async void OnButtonClicked(object sender, EventArgs e)
        {
            var viewModel = (ParameterViewModel)BindingContext;
            try
            {
                string url = $"http://{viewModel.IP}/api/Sucursales";
                this.lblMessage.Text = "Procesando...";
                // Device.OpenUri(new Uri("http://xamarin.com"));
                var request = new HttpRequestMessage();
                // request.RequestUri = new Uri("http://" + viewModel.IP + "/api/Sucursales");
                request.RequestUri = new Uri(url);
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");

                var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                HttpResponseMessage response = await client.SendAsync(request);

               
                if (response.StatusCode == HttpStatusCode.OK )
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var resultado = JsonConvert.DeserializeObject<List<Sucursales>>(content);
                    viewModel.Message = "Conexión satisfactoria.";
                    viewModel.Sucursal = resultado[0].Nombre;
                    Util.TestGuardar = true;
                    await DisplayAlert("Información", "Conexión satisfactoria.", "ok");
                    viewModel = (ParameterViewModel)BindingContext;//PENDIENTE
                }
                else
                {
                    Util.TestGuardar = false;
                    await DisplayAlert( "Error", "Error de comunicación.", "ok");
                    viewModel.Message = "Error de comunicación.";
                    viewModel.Sucursal = "";
                   
                }
            }
            catch (Exception fail)
            {
                Util.TestGuardar = false;
                await DisplayAlert ("Error", "Error: " + fail.Message, "ok");
                viewModel.Sucursal = "";
                viewModel.Message = "No se pudo conectar.";
            }
        }



    }
}