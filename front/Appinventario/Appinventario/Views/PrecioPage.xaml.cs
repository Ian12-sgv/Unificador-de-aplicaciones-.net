using Appinventario.Interfaces;
using Appinventario.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Appinventario.ViewModels;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PrecioPage : ContentPage
    {
        ArticuloViewModel _viewModel;
        public List<Articulo> listArt;

        public PrecioPage()
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

            // Existencia
            lblExi.IsVisible = true;
            lblExistencia.IsVisible = true;

            lblPrec.IsVisible = true;
            lblPrecio.IsVisible = true;
            lblDol.IsVisible = true;
            lblDolar.IsVisible = true;
        }

        private void EtiquetaInVisible()
        {
            lblArt.IsVisible = false;
            lblArticulo.IsVisible = false;
            lblInserte.IsVisible = true;

            // Existencia
            lblExi.IsVisible = false;
            lblExistencia.IsVisible = false;

            lblPrec.IsVisible = false;
            lblPrecio.IsVisible = false;
            lblDol.IsVisible = false;
            lblDolar.IsVisible = false;
            lblPromo.IsVisible = false;
            lblPromocion.IsVisible = false;
            lblPorcentaje.IsVisible = false;
        }

        private async void LoadInventario(object sender, EventArgs e)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(this.lblUrl.Text))
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

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    listArt = JsonConvert.DeserializeObject<List<Articulo>>(content);

                    this.BarcodeEntry.Focus();
                    this.BarcodeEntry.Text = "";

                    if (listArt != null && listArt.Count > 0)
                    {
                        foreach (var ls in listArt)
                        {
                            // Nombre
                            this.lblArticulo.Text = ls.Nombre;

                            // Existencia (después de nombre)
                            // Si tu modelo es decimal? usa: (ls.Existencia ?? 0)
                            
                            this.lblExistencia.Text = "Existencia: " + Math.Round(ls.Existencia, 2).ToString("0.##");


                            // Precio
                            this.lblPrecio.Text ="Precio Deatal: " + Math.Round(ls.PrecioDetal, 2).ToString("0.00");
                            this.lblDolar.Text ="Precio Dolar: " + Math.Round(ls.PrecioDolaresDetal, 2).ToString("0.00");

                            // Promo
                            this.lblPorcentaje.Text ="Descuento: " + Math.Round(ls.PorcentajeDescuento, 2).ToString("0") + "%";
                            this.lblPromocion.Text = "Promocion: " + Math.Round(ls.PrecioPromocion, 2).ToString("0.00");

                            // Evita que quede visible del artículo anterior
                            if (ls.PrecioPromocion > 0)
                            {
                                lblPromo.IsVisible = true;
                                lblPromocion.IsVisible = true;
                                lblPorcentaje.IsVisible = true;
                            }
                            else
                            {
                                lblPromo.IsVisible = false;
                                lblPromocion.IsVisible = false;
                                lblPorcentaje.IsVisible = false;
                            }
                        }

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
