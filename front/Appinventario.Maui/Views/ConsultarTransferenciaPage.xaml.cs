using Appinventario.Models;
using Appinventario.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConsultarTransferenciaPage : ContentPage
    {
        private readonly ObservableCollection<TransferenciaHistorialItem> transferenciasFiltradas;
        private List<TransferenciaHistorialItem> transferencias;
        private string filtroEstado = "TODAS";
        private string url = "";
        private bool cargandoTransferencias;
        private bool datosCargados;
        private bool statusOneBased;
        private bool navegando;
        private System.Threading.CancellationTokenSource filtroCts;

        public ObservableCollection<TransferenciaHistorialItem> TransferenciasFiltradas => transferenciasFiltradas;

        public ConsultarTransferenciaPage()
        {
            InitializeComponent();
            transferencias = new List<TransferenciaHistorialItem>();
            transferenciasFiltradas = new ObservableCollection<TransferenciaHistorialItem>();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!datosCargados)
            {
                try
                {
                    await LoadTransferencia();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"OnAppearing LoadTransferencia unexpected: {ex}");
                    MostrarEstado("Error cargando transferencias.", true);
                }
            }
        }

        private async Task LoadTransferencia()
        {
            if (cargandoTransferencias)
                return;

            cargandoTransferencias = true;
            try
            {
                await Task.Yield();

                estadoCarga.Text = "Cargando transferencias...";
                estadoCarga.TextColor = Color.FromArgb("#475569");
                estadoCarga.IsVisible = true;

                var parameter = await App.Database.GetParameterAsync();
                url = parameter?.IP ?? string.Empty;

                var serviceUrl = Util.NormalizeServiceUrl(url);
                Debug.WriteLine($"LoadTransferencia: serviceUrl={serviceUrl}");
                if (string.IsNullOrWhiteSpace(serviceUrl))
                {
                    MostrarEstado("No se ha configurado url del servicio en parametros.", true);
                    return;
                }

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(serviceUrl + "/api/Transferencia"),
                    Method = HttpMethod.Get
                };
                request.Headers.Add("Accept", "application/json");

                var client = Util.CreateHttpClient(TimeSpan.FromSeconds(3));
                var response = await client.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    MostrarEstado("No se pudo consultar el historial de transferencias.", true);
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                transferencias = JsonConvert.DeserializeObject<List<TransferenciaHistorialItem>>(content) ?? new List<TransferenciaHistorialItem>();
                NormalizeStatusValues();
                datosCargados = true;
                estadoCarga.IsVisible = false;
                AplicarFiltro();
            }
            catch (Exception fail)
            {
                Debug.WriteLine($"LoadTransferencia exception: {fail}");
                Util.TryPlayAudioFile("no-valido.mp3");
                MostrarEstado("Error: " + Util.NormalizeHttpExceptionMessage(fail), true);
            }
            finally
            {
                cargandoTransferencias = false;
            }
        }

        private void BuscarTransferencia_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ = AplicarFiltroAsync();
        }

        private void FiltroTodas_Clicked(object sender, EventArgs e)
        {
            filtroEstado = "TODAS";
            AplicarFiltro();
        }

        private void FiltroAbierta_Clicked(object sender, EventArgs e)
        {
            filtroEstado = "GUARDADA";
            AplicarFiltro();
        }

        private void FiltroConfirmada_Clicked(object sender, EventArgs e)
        {
            filtroEstado = "APROBADA";
            AplicarFiltro();
        }

        private void FiltroProcesada_Clicked(object sender, EventArgs e)
        {
            filtroEstado = "EXPORTADA";
            AplicarFiltro();
        }

        private async System.Threading.Tasks.Task AplicarFiltroAsync()
        {
            var swLocal = System.Diagnostics.Stopwatch.StartNew();
            filtroCts?.Cancel();
            filtroCts = new System.Threading.CancellationTokenSource();
            var token = filtroCts.Token;

            Debug.WriteLine($"FiltroRequest: started at {DateTime.Now:HH:mm:ss.fff}");

            try
            {
                // debounce breve para evitar filtrados repetidos durante tipeo
                await System.Threading.Tasks.Task.Delay(180, token);

                var texto = (buscarTransferencia?.Text ?? string.Empty).Trim();
                IEnumerable<TransferenciaHistorialItem> source = transferencias;

                await System.Threading.Tasks.Task.Yield();

                var result = await System.Threading.Tasks.Task.Run(() =>
                {
                    IEnumerable<TransferenciaHistorialItem> query = source;

                    if (filtroEstado == "GUARDADA")
                        query = query.Where(x => x.Status == 0);
                    else if (filtroEstado == "APROBADA")
                        query = query.Where(x => x.Status == 1);
                    else if (filtroEstado == "EXPORTADA")
                        query = query.Where(x => x.Status == 2);

                    if (!string.IsNullOrWhiteSpace(texto))
                    {
                        var textoNormalizado = NormalizarTextoBusqueda(texto).ToUpperInvariant();
                        var digitosBusqueda = SoloDigitos(texto);

                        query = query.Where(x =>
                            (x.NumeroNorm != null && x.NumeroNorm.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase)) ||
                            (x.NumeroDisplayNorm != null && x.NumeroDisplayNorm.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase)) ||
                            (x.SucursalNorm != null && x.SucursalNorm.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase)) ||
                            (x.ObservacionNorm != null && x.ObservacionNorm.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase)) ||
                            (x.UsuarioNorm != null && x.UsuarioNorm.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(digitosBusqueda) && (x.DigitosNumero ?? string.Empty).Contains(digitosBusqueda)) ||
                            ContieneEstado(x, textoNormalizado)
                        );
                    }

                    return query.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.NumeroOrden).ToList();
                }, token);

                if (token.IsCancellationRequested)
                    return;

                // actualizar UI una vez con el resultado
                transferenciasFiltradas.Clear();
                foreach (var item in result)
                    transferenciasFiltradas.Add(item);

                ActualizarBotonesFiltro();

                swLocal.Stop();
                Debug.WriteLine($"FiltroCompleted: elapsed={swLocal.ElapsedMilliseconds}ms results={result.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AplicarFiltroAsync exception: {ex}");
            }
        }

        // Compat shim para llamadas existentes que esperan método síncrono
        private void AplicarFiltro()
        {
            _ = AplicarFiltroAsync();
        }

        private void ActualizarBotonesFiltro()
        {
            PintarBoton(btnTodas, filtroEstado == "TODAS");
            PintarBoton(btnAbierta, filtroEstado == "GUARDADA");
            PintarBoton(btnConfirmada, filtroEstado == "APROBADA");
            PintarBoton(btnProcesada, filtroEstado == "EXPORTADA");
        }

        private static void PintarBoton(Button boton, bool activo)
        {
            boton.BackgroundColor = activo ? Color.FromArgb("#2447B8") : Color.FromArgb("#E9EEF6");
            boton.TextColor = activo ? Colors.White : Color.FromArgb("#475569");
        }

        private void MostrarEstado(string mensaje, bool error)
        {
            if (estadoCarga == null)
            {
                Debug.WriteLine($"MostrarEstado invoked but estadoCarga is null: {mensaje}");
                return;
            }

            estadoCarga.Text = mensaje;
            estadoCarga.TextColor = error ? Color.FromArgb("#9C2F27") : Color.FromArgb("#475569");
            estadoCarga.IsVisible = !string.IsNullOrWhiteSpace(mensaje);
        }

        private async void Transferencia_Tapped(object sender, TappedEventArgs e)
        {
            if (navegando)
                return;

            var item = (sender as BindableObject)?.BindingContext as TransferenciaHistorialItem;
            if (item == null)
                return;

            if (item.Status != 0)
            {
                await DisplayAlert("Transferencia", "Solo se puede acceder a transferencias guardadas.", "ok");
                return;
            }

            navegando = true;
            try
            {
                if (!int.TryParse(item.Numero, out var numeroTransferencia))
                {
                    await DisplayAlert("Transferencia", "El numero de transferencia no es valido: " + item.Numero, "ok");
                    return;
                }

                await Navigation.PushAsync(new NuevaTransferenciaPage(numeroTransferencia), false);
            }
            finally
            {
                navegando = false;
            }
        }

        private static bool ContieneBusqueda(string texto, string textoNormalizado, string digitosBusqueda)
        {
            var valor = NormalizarTextoBusqueda(texto);
            if (!string.IsNullOrWhiteSpace(textoNormalizado) &&
                valor.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.IsNullOrWhiteSpace(digitosBusqueda))
                return false;

            var digitosValor = SoloDigitos(texto);
            return digitosValor.Contains(digitosBusqueda, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContieneEstado(TransferenciaHistorialItem item, string textoEstado)
        {
            if (string.IsNullOrWhiteSpace(textoEstado))
                return false;

            if (textoEstado.Contains("GUARDADA", StringComparison.OrdinalIgnoreCase) || textoEstado.Contains("TODAS", StringComparison.OrdinalIgnoreCase))
                return item.Status == 0;

            if (textoEstado.Contains("APROBADA", StringComparison.OrdinalIgnoreCase) || textoEstado.Contains("APROBADO", StringComparison.OrdinalIgnoreCase))
                return item.Status == 1;

            if (textoEstado.Contains("EXPORTADA", StringComparison.OrdinalIgnoreCase) || textoEstado.Contains("EXPORTADO", StringComparison.OrdinalIgnoreCase))
                return item.Status == 2;

            return false;
        }

        private static string NormalizarTextoBusqueda(string texto)
        {
            return (texto ?? string.Empty)
                .Trim()
                .Replace("N°", "N", StringComparison.OrdinalIgnoreCase)
                .Replace("Nº", "N", StringComparison.OrdinalIgnoreCase)
                .Replace("#", string.Empty)
                .Replace("  ", " ");
        }

        private static string SoloDigitos(string texto)
        {
            return new string((texto ?? string.Empty).Where(char.IsDigit).ToArray());
        }

        private void NormalizeStatusValues()
        {
            if (transferencias.All(x => x.Status >= 1 && x.Status <= 3))
            {
                statusOneBased = true;
                foreach (var item in transferencias)
                {
                    item.Status -= 1;
                }
            }
            else
            {
                statusOneBased = false;
            }
            PrecomputeNormalizedFields();
        }

        private void PrecomputeNormalizedFields()
        {
            foreach (var item in transferencias)
            {
                item.NumeroNorm = NormalizarTextoBusqueda(item.Numero)?.ToUpperInvariant();
                item.NumeroDisplayNorm = NormalizarTextoBusqueda(item.NumeroDisplay)?.ToUpperInvariant();
                item.SucursalNorm = NormalizarTextoBusqueda(item.SucursalNombreDisplay)?.ToUpperInvariant();
                item.ObservacionNorm = NormalizarTextoBusqueda(item.ObservacionDisplay)?.ToUpperInvariant();
                item.UsuarioNorm = NormalizarTextoBusqueda(item.Usuario)?.ToUpperInvariant();
                item.DigitosNumero = SoloDigitos(item.Numero);
            }
        }

        public class TransferenciaHistorialItem
        {
            public string Numero { get; set; }
            public DateTime Fecha { get; set; }
            public string CodigoRecibe { get; set; }
            public string CodigoEnvia { get; set; }
            public string Observacion { get; set; }
            public int Status { get; set; }
            public string Usuario { get; set; }
            // normalized/cached fields for faster search
            public string NumeroNorm { get; set; }
            public string NumeroDisplayNorm { get; set; }
            public string SucursalNorm { get; set; }
            public string ObservacionNorm { get; set; }
            public string UsuarioNorm { get; set; }
            public string DigitosNumero { get; set; }

            public int NumeroOrden => int.TryParse(Numero, out var value) ? value : 0;
            public string NumeroDisplay => "N " + Numero;
            public string EstadoTexto
            {
                get
                {
                    if (Status == 0)
                        return "GUARDADA";

                    if (Status == 1)
                        return "APROBADA";

                    if (Status == 2)
                        return "EXPORTADA";

                    return "STATUS " + Status;
                }
            }

            public Color EstadoColor
            {
                get
                {
                    if (Status == 0)
                        return Color.FromArgb("#F59E0B");

                    if (Status == 1)
                        return Color.FromArgb("#00965F");

                    if (Status == 2)
                        return Color.FromArgb("#1D4ED8");

                    return Color.FromArgb("#475569");
                }
            }

            public Color EstadoFondo
            {
                get
                {
                    if (Status == 0)
                        return Color.FromArgb("#FFF3D8");

                    if (Status == 1)
                        return Color.FromArgb("#E6FFF4");

                    if (Status == 2)
                        return Color.FromArgb("#EEF4FF");

                    return Color.FromArgb("#E9EEF6");
                }
            }

            public string SucursalNombreDisplay => CodigoRecibe ?? CodigoEnvia ?? string.Empty;
            public string ObservacionDisplay => string.IsNullOrWhiteSpace(Observacion) ? "Sin observacion" : Observacion;
            public string FechaDisplay => Fecha == DateTime.MinValue ? string.Empty : Fecha.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            public string ResumenItems => string.IsNullOrWhiteSpace(Usuario) ? "Sin usuario" : Usuario;
        }
    }
}
