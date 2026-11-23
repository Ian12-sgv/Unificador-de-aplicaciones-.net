using Microsoft.Maui.Controls;
using Appinventario.Services;
using Appinventario.Services.BorradorDev;
using System;
using System.Threading.Tasks;

namespace Appinventario.Views
{
    public partial class TomaFisicaLoginPage : ContentPage
    {
        public TomaFisicaLoginPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            TomaFisicaAuthService.Clear();

            var parameter = await App.Database.GetParameterAsync();
            var listener = BorradorDevSettings.BuildBaseUrlFromPrecioEndpoint(parameter?.IP);
            lblServidor.Text = string.IsNullOrWhiteSpace(listener)
                ? "Servidor sin configurar"
                : "Servidor: " + listener;

            txtPassword.Text = string.Empty;
            txtUsuario.Focus();
        }

        private void OnUsuarioCompleted(object sender, EventArgs e)
        {
            txtPassword.Focus();
        }

        private async void OnPasswordCompleted(object sender, EventArgs e)
        {
            await IngresarAsync();
        }

        private async void OnIngresarClicked(object sender, EventArgs e)
        {
            await IngresarAsync();
        }

        private async Task IngresarAsync()
        {
            CambiarEstadoCarga(true);
            lblMensaje.IsVisible = false;

            var result = await TomaFisicaAuthService.LoginAsync(txtUsuario.Text, txtPassword.Text);
            CambiarEstadoCarga(false);

            if (!result.IsSuccess)
            {
                lblMensaje.Text = result.Message;
                lblMensaje.IsVisible = true;
                txtPassword.Text = string.Empty;
                txtPassword.Focus();
                return;
            }

            await Navigation.PushAsync(new TomaFisicaMenuPage());
        }

        private void CambiarEstadoCarga(bool cargando)
        {
            btnIngresar.IsEnabled = !cargando;
            indicador.IsVisible = cargando;
            indicador.IsRunning = cargando;
        }
    }
}
