using Appinventario.Services;
using Appinventario.Services.BorradorDev;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Appinventario.Views.BorradorDev
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BorradorDevPage : ContentPage
    {
        private const string UsuarioFijo = "admi";
        private string baseUrl;
        private BorradorDevApiService apiService;
        private bool isBusy;
        private CancellationTokenSource _scanDebounce;

        public BorradorDevPage()
        {
            InitializeComponent();
            ResetForNew();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await LoadConfigurationAsync(true);
                UpdateActionState();
                await FocusBarcodeAsync();
            }
            catch (Exception ex)
            {
                await CrashReportService.LogExceptionAsync(ex, "BorradorDevPage.OnAppearing");
                SetStatus("Error al cargar la pantalla. Reinicia la app e intenta de nuevo.", true);
            }
        }

        private async System.Threading.Tasks.Task LoadConfigurationAsync(bool forceReload = false)
        {
            if (forceReload || string.IsNullOrWhiteSpace(baseUrl))
            {
                var configuredBaseUrl = await BorradorDevSettings.GetBaseUrlAsync();
                if (!string.Equals(baseUrl, configuredBaseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = configuredBaseUrl;
                    apiService = string.IsNullOrWhiteSpace(baseUrl)
                        ? null
                        : new BorradorDevApiService(baseUrl);
                }
            }

            lblBaseUrl.Text = "Configuracion compartida: " + baseUrl + " | Usuario: " + UsuarioFijo;
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

            try
            {
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
            catch (Exception ex)
            {
                await CrashReportService.LogExceptionAsync(ex, "BorradorDevPage.OnBuscarClicked");
                SetStatus("No se pudo abrir la búsqueda. Intenta de nuevo.", true);
            }
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            await SaveCurrentAsync();
        }

        private async void BarcodeTextChanged(object sender, TextChangedEventArgs e)
        {
            _scanDebounce?.Cancel();
            var codigo = (e.NewTextValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(codigo) || isBusy)
                return;

            var cts = _scanDebounce = new CancellationTokenSource();
            try
            {
                await Task.Delay(250, cts.Token);
                if (!cts.IsCancellationRequested && string.Equals(codigo, (txtCodigoBarra.Text ?? string.Empty).Trim(), StringComparison.Ordinal))
                    await SaveCurrentAsync();
            }
            catch (TaskCanceledException)
            {
            }
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
                await CrashReportService.LogExceptionAsync(ex, "BorradorDevPage.OnAprobarClicked");
                SetStatus("Error al aprobar. Revisa tu conexión o intenta nuevamente.", true);
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

            try
            {
                var service = await CreateServiceAsync();
                if (service == null)
                    return;

                await Navigation.PushAsync(new BorradorDevDetailPage(service, numero.Value));
            }
            catch (Exception ex)
            {
                await CrashReportService.LogExceptionAsync(ex, "BorradorDevPage.OnDetalleClicked");
                SetStatus("No se pudo abrir el detalle. Intenta de nuevo.", true);
            }
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
                SetStatus("Configura primero la ruta compartida en Parametros.", true);
                return null;
            }

            if (apiService == null)
                apiService = new BorradorDevApiService(baseUrl);

            return apiService;
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
