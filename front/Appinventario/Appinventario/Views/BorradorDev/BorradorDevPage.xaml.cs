using Appinventario.Services.BorradorDev;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Appinventario.Views.BorradorDev
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BorradorDevPage : ContentPage
    {
        private const string UsuarioFijo = "admi";
        private string baseUrl;
        private bool isBusy;

        public BorradorDevPage()
        {
            InitializeComponent();
            ResetForNew();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadConfigurationAsync();
            UpdateActionState();
            await FocusBarcodeAsync();
        }

        private async System.Threading.Tasks.Task LoadConfigurationAsync()
        {
            baseUrl = await BorradorDevSettings.GetBaseUrlAsync();
            lblBaseUrl.Text = "API: " + baseUrl + " | Usuario: " + UsuarioFijo;
        }

        private async void OnParametrosClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new BorradorDevParametersPage());
        }

        private void OnNuevoClicked(object sender, EventArgs e)
        {
            ResetForNew();
            SetStatus("Nuevo borrador listo. Escanea y guarda para crear el numero.", false);
            _ = FocusBarcodeAsync();
        }

        private async void OnBuscarClicked(object sender, EventArgs e)
        {
            if (isBusy)
                return;

            var service = await CreateServiceAsync();
            if (service == null)
                return;

            var searchPage = new BorradorDevSearchPage(service);
            searchPage.NumeroSelected += numero =>
            {
                txtNumero.Text = numero.ToString();
                UpdateModeText();
                SetStatus("Numero seleccionado: " + numero, false);
                _ = FocusBarcodeAsync();
            };

            await Navigation.PushModalAsync(new NavigationPage(searchPage));
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            await SaveCurrentAsync();
        }

        private async void OnBarcodeCompleted(object sender, EventArgs e)
        {
            await SaveCurrentAsync();
        }

        private async void OnAprobarClicked(object sender, EventArgs e)
        {
            var numero = GetCurrentNumero();
            if (!numero.HasValue)
            {
                SetStatus("Primero crea un numero o usa Buscar.", true);
                return;
            }

            var service = await CreateServiceAsync();
            if (service == null)
                return;

            try
            {
                isBusy = true;
                UpdateActionState();
                SetStatus("Aprobando numero...", false);
                var result = await service.RunAsync(numero.Value, true);
                var procesados = result != null && result.Info != null ? result.Info.NumerosProcesados : 0;
                SetStatus(procesados > 0 ? "Aprobado con exito." : "No se encontro el numero o no habia registros.", false);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, true);
            }
            finally
            {
                isBusy = false;
                UpdateActionState();
            }
        }

        private async void OnDetalleClicked(object sender, EventArgs e)
        {
            var numero = GetCurrentNumero();
            if (!numero.HasValue)
            {
                SetStatus("Primero crea un numero o usa Buscar.", true);
                return;
            }

            var service = await CreateServiceAsync();
            if (service == null)
                return;

            await Navigation.PushAsync(new BorradorDevDetailPage(service, numero.Value));
        }

        private void OnNumeroCajaUnfocused(object sender, FocusEventArgs e)
        {
            txtNumeroCaja.Text = GetNumeroCaja().ToString();
        }

        private async System.Threading.Tasks.Task SaveCurrentAsync()
        {
            if (isBusy)
                return;

            var codigo = (txtCodigoBarra.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(codigo))
            {
                SetStatus("CodigoBarra es requerido.", true);
                await FocusBarcodeAsync();
                return;
            }

            var service = await CreateServiceAsync();
            if (service == null)
                return;

            try
            {
                isBusy = true;
                UpdateActionState();
                var numero = GetCurrentNumero();
                var caja = GetNumeroCaja();

                if (!numero.HasValue)
                {
                    var created = await service.CreateAsync(UsuarioFijo, txtObservacion.Text, dpFecha.Date, codigo, caja, 1m, 0);
                    txtNumero.Text = created.Numero.ToString();
                    UpdateModeText();
                    SetStatus("Creado #" + created.Numero + ". Linea agregada en caja " + caja + ".", false);
                }
                else
                {
                    var scanned = await service.ScanAsync(numero.Value, codigo, caja, 1m);

                    if (scanned.Updated > 0)
                        SetStatus("Cantidad incrementada en #" + numero.Value + " (Caja " + caja + ").", false);
                    else if (scanned.Inserted > 0)
                        SetStatus("Linea agregada en #" + numero.Value + " (Caja " + caja + ").", false);
                    else
                        SetStatus("Sin cambios en #" + numero.Value + ".", false);
                }

                txtCodigoBarra.Text = string.Empty;
                await FocusBarcodeAsync();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, true);
            }
            finally
            {
                isBusy = false;
                UpdateActionState();
            }
        }

        private long? GetCurrentNumero()
        {
            long value;
            return long.TryParse((txtNumero.Text ?? string.Empty).Trim(), out value) ? value : (long?)null;
        }

        private int GetNumeroCaja()
        {
            int value;
            return int.TryParse((txtNumeroCaja.Text ?? string.Empty).Trim(), out value) && value > 0 ? value : 1;
        }

        private async System.Threading.Tasks.Task<BorradorDevApiService> CreateServiceAsync()
        {
            await LoadConfigurationAsync();

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                SetStatus("Configura primero la URL del modulo Borrador Dev.", true);
                return null;
            }

            return new BorradorDevApiService(baseUrl);
        }

        private void ResetForNew()
        {
            txtNumero.Text = string.Empty;
            txtObservacion.Text = string.Empty;
            txtCodigoBarra.Text = string.Empty;
            txtNumeroCaja.Text = "1";
            dpFecha.Date = DateTime.Today;
            UpdateModeText();
            UpdateActionState();
        }

        private void UpdateModeText()
        {
            var numero = GetCurrentNumero();

            if (!numero.HasValue)
                lblMode.Text = "Modo: NUEVO. El numero se crea al guardar el primer escaneo.";
            else
                lblMode.Text = "Modo: ESCANEAR a #" + numero.Value + ". Si repites codigo y caja, el backend suma cantidad.";

            UpdateActionState();
        }

        private void SetStatus(string message, bool isError)
        {
            lblStatus.Text = message;
            lblStatus.TextColor = isError ? Color.FromHex("#9C2F27") : Color.FromHex("#166534");
            frameStatus.IsVisible = !string.IsNullOrWhiteSpace(message);
            frameStatus.BackgroundColor = isError ? Color.FromHex("#FDECEA") : Color.FromHex("#E8F5EE");
            frameStatus.BorderColor = isError ? Color.FromHex("#F4C7C3") : Color.FromHex("#B7E1C3");
        }

        private async System.Threading.Tasks.Task FocusBarcodeAsync()
        {
            await System.Threading.Tasks.Task.Delay(120);
            Device.BeginInvokeOnMainThread(() => txtCodigoBarra.Focus());
        }

        private void UpdateActionState()
        {
            var hasNumero = GetCurrentNumero().HasValue;
            var canInteract = !isBusy;

            btnParametrosTop.IsEnabled = canInteract;
            btnNuevoTop.IsEnabled = canInteract;
            btnBuscarTop.IsEnabled = canInteract;
            btnBuscarNumero.IsEnabled = canInteract;
            btnNuevoNumero.IsEnabled = canInteract;
            btnGuardar.IsEnabled = canInteract;
            btnAprobar.IsEnabled = canInteract && hasNumero;
            btnDetalle.IsEnabled = canInteract && hasNumero;
            txtCodigoBarra.IsEnabled = canInteract;
            txtNumeroCaja.IsEnabled = canInteract;
            txtObservacion.IsEnabled = canInteract;
            dpFecha.IsEnabled = canInteract;
        }
    }
}
