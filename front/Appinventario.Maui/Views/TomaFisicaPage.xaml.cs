using Appinventario.Models;
using Appinventario.ViewModels;
using Appinventario.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Appinventario.Interfaces;
using Appinventario.Services;
using Microsoft.Maui.ApplicationModel;

namespace Appinventario.Views
{
    public partial class ItemsPage : ContentPage
    {
        TomaFisica toma;
        TomaFisicaViewModel _viewModel;
        public List<TomaFisica> listToma;
        string ImeiL = "";
        

        public ItemsPage()
        {
            InitializeComponent();
            toma = new TomaFisica();
            BindingContext = _viewModel = new TomaFisicaViewModel();
            if (_viewModel.SelectCommand.CanExecute(null))
                _viewModel.SelectCommand.Execute(null);
            EtiquetaInVisible();
            ImeiL =  Util.Imei;
            
        }

        protected async override void OnAppearing()
        {
          //  base.OnAppearing();
            _viewModel.OnSelect();

            //***Valida que en parametros este activado o no este atributo
            if (Util.NumPagG.Length > 0)
            {
                this.txtPagina.Text = Services.Util.NumPagG;
                Util.NumPagG = "";
            }

            if (Util.ObsG.Length > 0)
            {
                this.txtObs.Text = Services.Util.ObsG;
                Util.ObsG = "";
            }
            SelectTextCodigo();
            txtCodigo.Text = "";

        }


        private async void OnButtonClicked(object sender, EventArgs e)
        {
            try
            {
                bool result = Int32.TryParse(this.txtPagina.Text, out int n);
                var viewModel = (TomaFisicaViewModel)BindingContext;
                this.lblArticulo.Text = "";
                this.lblExistencia.Text = "";

                if (String.IsNullOrWhiteSpace(this.lblUrl.Text))
                {
                    await DisplayAlert("Advertencia", "No hay servidor configurado.", "ok");
                    return;
                }

                if (String.IsNullOrWhiteSpace(this.txtPagina.Text))
                {
                    await DisplayAlert("Advertencia", "Debe insertar página.", "ok");
                    this.txtCodigo.Text = "";
                    this.txtPagina.Focus();
                    return;
                }
                if (String.IsNullOrWhiteSpace(this.txtObs.Text))
                {
                    await DisplayAlert("Advertencia", "Debe insertar observación.", "ok");
                    this.txtCodigo.Text = "";
                    this.txtObs.Focus();
                    return;
                }

                if (!result)
                {
                    await this.DisplayAlert( "Advertencia", "La página debe ser valor numérico válido.", "ok");
                    this.txtCodigo.Text = "";
                    this.txtPagina.Text = "";
                    this.txtPagina.Focus();
                    return ;
                }

                if ( Convert.ToInt32(this.txtPagina.Text) > 50000)
                {
                    await DisplayAlert("Advertencia", "No puede haber página mayor a 50000.", "ok");
                    this.txtPagina.Text = "";
                    this.txtCodigo.Text = "";
                    this.txtPagina.Focus();
                    return;
                }
                
                if (String.IsNullOrWhiteSpace(this.txtCodigo.Text))
                {
                    await DisplayAlert( "Advertencia", "Debe insertar código de barra.", "ok");
                    this.txtCodigo.Focus();
                    return;
                }
               
                 toma = new TomaFisica
                {
                    NumeroPagina = _viewModel.NumeroPagina,
                    Fecha = DateTime.Now,
                    Observacion = _viewModel.Observacion,
                    Referencia = "",
                    Cantidad = 1,
                    Usuario = "INVE",
                    Status = 0,
                    CodigoBarra = _viewModel.CodigoBarra.Trim(),
                    Prenda = "",
                    Id = "",
                    Text = "",
                    Nombre = "",
                    Valor = false,
                    Imei = Util.EstatusImei == true ? Util.Imei : ""
                 };
              
                Uri RequestUri = new Uri("http://" + this.lblUrl.Text + "/api/TomaFisica");
                var client = Util.CreateHttpClient();
                var json = JsonConvert.SerializeObject(toma);
                var contentJson = new StringContent(json,Encoding.UTF8,"application/json");
                var response = await client.PostAsync(RequestUri,contentJson);

             //   if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError)
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                    GetPost(Convert.ToInt32(toma.NumeroPagina), toma.CodigoBarra);
                   
                     if (content == "-1")
                    {
                        DependencyService.Get<IAudio>().PlayAudioFile("no-valido.mp3");
                        await DisplayAlert("Información", "Código de barra no existe o Toma física existente en otro dispositivo.", "ok");
                        SelectTextCodigo();
                        txtCodigo.Text = "";
                        txtCodigo.Focus();
                        return;
                    }

                    SelectTextCodigo();
                    txtCodigo.Text = "";
                    txtCodigo.Focus();
                    return;
                    }
                    else
                    {
                        DependencyService.Get<IAudio>().PlayAudioFile("no-valido.mp3");
                        await DisplayAlert("Error", response.StatusCode.ToString(), "ok");
                        SelectTextCodigo();
                        txtCodigo.Text = "";
                        txtCodigo.Focus();
                        return;
                    }
            }
            catch (Exception fail)
            {
                DependencyService.Get<IAudio>().PlayAudioFile("no-valido.mp3");
                await DisplayAlert( "Error", "Error: " + fail.Message, "ok");
                txtCodigo.Text = "";
                txtCodigo.Focus();
                SelectTextCodigo();
            }
        }

        private async void GetPost(int numeroPagina ,string codigoBarra)
        {
            try
            {
              
                LimpiarArticulo();
                var request = new HttpRequestMessage();
                if (Util.EstatusImei == false)
                    ImeiL = "*";
                else
                    ImeiL = Util.Imei;
                request.RequestUri = new Uri("http://" + lblUrl.Text + "/api/TomaFisica/byParameters?numeroPagina=" + numeroPagina + "&codigoBarra=" + codigoBarra + "&imei=" + ImeiL);
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                var client = new HttpClient();
                HttpResponseMessage response = await client.SendAsync(request);
                Task.Delay(200).Wait();
                toma = new TomaFisica();
                if (response.StatusCode == HttpStatusCode.OK )
                {
                    string content = await response.Content.ReadAsStringAsync();
                    listToma = JsonConvert.DeserializeObject<List<TomaFisica>>(content);

                    foreach (var ls in listToma)
                    {
                        toma.Fecha = ls.Fecha;
                        toma.Status = ls.Status;
                        toma.CodigoBarra = ls.CodigoBarra;
                        toma.Prenda = ls.Prenda;
                        toma.NumeroPagina = ls.NumeroPagina;
                        toma.Nombre = ls.Nombre;
                        toma.Cantidad = ls.Cantidad;
                        toma.Id = ls.Id;
                        toma.Observacion = ls.Observacion;
                        toma.Referencia = ls.Referencia;
                        toma.Text = ls.Text;    
                        toma.Usuario = ls.Usuario;  

                        this.lblArticulo.Text = ls.Nombre;
                        this.lblExistencia.Text = ls.Cantidad.ToString();
                    }
                    if (listToma.Count > 0)
                        EtiquetaVisible();
                    else
                        EtiquetaInVisible();
                }
                if (ImeiL == "*")
                    ImeiL = "";
               
            }
            catch (Exception fail)
            {
                await DisplayAlert("Error", "Error: " + fail.Message, "ok");
            }
        }

        private void EtiquetaVisible()
        {
            lblArt.IsVisible = true;
            lblArticulo.IsVisible = true;
            lblExis.IsVisible = true;
            lblExistencia.IsVisible = true;
        }

        private void EtiquetaInVisible()
        {
            lblArt.IsVisible = false;
            lblArticulo.IsVisible = false;
            lblExis.IsVisible = false;
            lblExistencia.IsVisible = false;
        }

        private void LimpiarArticulo()
        {
            lblArt.Text = "";
            lblArticulo.Text = "";
            lblExis.Text = "";
            lblExistencia.Text = "";          
        }
        private void SelectTextCodigo()
        {
            try
            {
                if (txtCodigo.Text.Length > 0)
                {
                    txtCodigo.CursorPosition = 0;
                    txtCodigo.SelectionLength = txtCodigo.Text.Length;
                }
                this.txtCodigo.Focus();
            }
            catch
            {
                this.txtCodigo.Focus();
                return;
            }
        }

        private async void OnButtonClean(object sender, EventArgs e)
        {
            try
            {
                this.txtPagina.Text = "";
                this.txtCodigo.Text = "";
                this.txtObs.Text = "";             
                EtiquetaInVisible();
            }
            catch (Exception fail)
            {
                await DisplayAlert("Error", "Error: " + fail.Message, "ok");
            }
        }

        private  void SearchCatalogo(object sender, EventArgs e)
        {
            try
            {
                LimpiarArticulo();
                if (String.IsNullOrWhiteSpace( this.lblUrl.Text) == true)
                {
                    DisplayAlert("Advertencia", "No se ha configurado url del servicio en parámetros.", "ok");
                    return;
                }
                this.Navigation.PushAsync(new BuscarTomaPage(this.lblUrl.Text));
            }
            catch (Exception fail)
            {
                 DisplayAlert("Error", "Error: " + fail.Message, "ok");
            }
        }

        private void LoadDetalle(object sender, EventArgs e)
        {
            try
            {
                bool result = Int32.TryParse(this.txtPagina.Text, out int n);

                if (String.IsNullOrWhiteSpace(this.lblUrl.Text) == true)
                {
                    DisplayAlert("Advertencia", "No se ha configurado url del servicio en parámetros.", "ok");
                    return;
                }
                if (String.IsNullOrWhiteSpace(this.txtPagina.Text))
                {
                    DisplayAlert("Advertencia", "Debe insertar página.", "ok");
                    this.txtPagina.Focus();
                    return;
                }
                if (!result)
                {
                    this.DisplayAlert("Advertencia", "Debe insertar solo números.", "ok");
                    txtPagina.Text = "";
                    this.txtPagina.Focus();
                    return;
                }
                if (Convert.ToInt32(this.txtPagina.Text) > 50000)
                {
                    DisplayAlert("Advertencia", "No puede haber página mayor a 50000.", "ok");
                    this.txtPagina.Focus();
                    return;
                }
                this.Navigation.PushAsync(new TomaFisicaDetallePage(Convert.ToInt32(this.txtPagina.Text),
                                                                                    this.lblUrl.Text,
                                                                                    this.txtObs.Text));

                // var modelo = DeviceInfo.
              //  var modelo = CrossDeviceInfo.Current
            }
            catch (Exception fail)
            {
                DisplayAlert("Error", "Error: " + fail.Message, "ok");
            }

        }
        


    }
}
