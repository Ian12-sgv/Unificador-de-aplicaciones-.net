using Microsoft.Maui.Controls;
using Appinventario.Services;
using System;

namespace Appinventario.Views
{
    public partial class TomaFisicaMenuPage : ContentPage
    {
        public TomaFisicaMenuPage()
        {
            InitializeComponent();
            lblUsuario.Text = "Usuario: " + TomaFisicaAuthService.CodUsuario;
            AplicarPermisosToma();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            AplicarPermisosToma();
            if (!TomaFisicaAuthService.IsAuthenticated)
                await Navigation.PopToRootAsync();
        }

        private async void OnToma1Clicked(object sender, EventArgs e)
        {
            if (!AuthService.CanUseTomaFisica1())
                return;

            await Navigation.PushAsync(new ItemsPage());
        }

        private async void OnToma2Clicked(object sender, EventArgs e)
        {
            if (!AuthService.CanUseTomaFisica2())
                return;

            await Navigation.PushAsync(new TomaFisica2Page());
        }

        private void AplicarPermisosToma()
        {
            btnToma1.IsVisible = AuthService.CanUseTomaFisica1();
            btnToma2.IsVisible = AuthService.CanUseTomaFisica2();
        }
        private async void OnCerrarSesionClicked(object sender, EventArgs e)
        {
            TomaFisicaAuthService.Clear();
            await Navigation.PopToRootAsync();
        }
    }
}
