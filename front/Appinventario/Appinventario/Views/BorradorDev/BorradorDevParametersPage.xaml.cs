using Appinventario.Services.BorradorDev;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Appinventario.Views.BorradorDev
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BorradorDevParametersPage : ContentPage
    {
        private string validatedUrl;
        private bool loading;
        private bool testing;

        public BorradorDevParametersPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (loading)
                return;

            loading = true;
            txtBaseUrl.Text = await BorradorDevSettings.GetBaseUrlAsync();
            validatedUrl = null;
            SetStatus("Prueba la conexion antes de guardar.", false);
            UpdateButtonState();
            loading = false;
        }

        private void OnBaseUrlChanged(object sender, TextChangedEventArgs e)
        {
            if (loading)
                return;

            validatedUrl = null;
            SetStatus("La URL cambio. Vuelve a probar la conexion.", false);
            UpdateButtonState();
        }

        private async void OnTestClicked(object sender, EventArgs e)
        {
            var normalized = BorradorDevSettings.NormalizeBaseUrl(txtBaseUrl.Text);

            if (string.IsNullOrWhiteSpace(normalized))
            {
                SetStatus("Debes indicar una URL valida.", true);
                return;
            }

            try
            {
                testing = true;
                UpdateButtonState();
                var service = new BorradorDevApiService(normalized);
                SetStatus("Probando conexion...", false);
                await service.TestConnectionAsync();
                validatedUrl = normalized;
                SetStatus("Conexion OK. Ya puedes guardar.", false);
            }
            catch (Exception ex)
            {
                validatedUrl = null;
                SetStatus(ex.Message, true);
            }
            finally
            {
                testing = false;
                UpdateButtonState();
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            var normalized = BorradorDevSettings.NormalizeBaseUrl(txtBaseUrl.Text);

            if (string.IsNullOrWhiteSpace(normalized))
            {
                SetStatus("Debes indicar una URL valida.", true);
                return;
            }

            if (!string.Equals(normalized, validatedUrl, StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("Primero prueba la conexion con esta URL.", true);
                return;
            }

            await BorradorDevSettings.SaveBaseUrlAsync(normalized);
            await DisplayAlert("Informacion", "Configuracion guardada.", "OK");
            await Navigation.PopAsync();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void SetStatus(string message, bool isError)
        {
            lblStatus.Text = message;
            lblStatus.TextColor = isError ? Color.FromHex("#9C2F27") : Color.FromHex("#166534");
            frameStatus.IsVisible = !string.IsNullOrWhiteSpace(message);
            frameStatus.BackgroundColor = isError ? Color.FromHex("#FDECEA") : Color.FromHex("#E8F5EE");
            frameStatus.BorderColor = isError ? Color.FromHex("#F4C7C3") : Color.FromHex("#B7E1C3");
        }

        private void UpdateButtonState()
        {
            var normalized = BorradorDevSettings.NormalizeBaseUrl(txtBaseUrl.Text);
            var canSave = !testing &&
                          !string.IsNullOrWhiteSpace(validatedUrl) &&
                          string.Equals(normalized, validatedUrl, StringComparison.OrdinalIgnoreCase);

            btnTest.IsEnabled = !testing;
            btnTest.Text = testing ? "Probando..." : "Probar conexion";
            btnSave.IsEnabled = canSave;
        }
    }
}
