using Appinventario.Interfaces;
using Appinventario.Models;
using Appinventario.Services;
using Appinventario.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NuevaTransferenciaPage : ContentPage
    {
        private SucursalesViewModel _viewModel;
        private ObservableCollection<TransferenciaDetalleLinea> detalles;
        private List<Sucursales> listSucur;
        private Articulo articuloActual;
        private string url = "";
        private bool parametrosCargados;
        private bool preparandoDatos;
        private bool sucursalesCargadas;
        private bool despachosCargados;
        private bool lotesCargados;
        private bool cabeceraVisible = true;
        private bool escaneoVisible;
        private int cajaSeleccionada = 1;
        private readonly int? numeroParaCargar;
        private bool transferenciaCargada;
        private bool agregandoDetalle;
        private int codigoBarraVersion;
        private List<TransferenciaDespachoItem> listaDespachos = new List<TransferenciaDespachoItem>();
        private List<TransferenciaLoteItem> listaLotes = new List<TransferenciaLoteItem>();

        // Borrador local y confirmacion al salir.
        private bool hayCambios;
        private bool suspenderSeguimiento;
        private bool borradorRevisado;
        private bool lineaBaseFijada;
        private bool saliendo;
        private bool EsNueva => !numeroParaCargar.HasValue;
        private static string ClaveBorrador =>
            "transferencia_nueva_borrador_" + (AuthService.CurrentUser?.Username ?? "anonimo");

        public NuevaTransferenciaPage()
        {
            InitializeComponent();
            numeroParaCargar = null;
            InicializarPagina();
        }

        public NuevaTransferenciaPage(int numeroTransferencia)
        {
            InitializeComponent();
            numeroParaCargar = numeroTransferencia;
            InicializarPagina();
        }

        private void InicializarPagina()
        {
            _viewModel = new SucursalesViewModel();
            BindingContext = _viewModel;
            detalles = new ObservableCollection<TransferenciaDetalleLinea>();
            detalleGrid.ItemsSource = detalles;
            totalCaja.Text = "0.00";
            cantidadTotal.Text = "0.00";
            SeleccionarCaja(1);
            ActualizarEncabezado();

            documentoOrigen.TextChanged += (_, _) => NotificarCambio();
            observacion.TextChanged += (_, _) => NotificarCambio();
            fecha.DateSelected += (_, _) => NotificarCambio();
            fechaEmision.DateSelected += (_, _) => NotificarCambio();
            transferenciaCorreccion.Toggled += (_, _) => NotificarCambio();
            despacho.SelectedIndexChanged += (_, _) => NotificarCambio();
            lote.SelectedIndexChanged += (_, _) => NotificarCambio();

            // Flecha "atras" de la barra de Shell (el boton fisico va por OnBackButtonPressed).
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior
            {
                Command = new Command(async () => await IntentarSalirAsync())
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = PrepararDatosAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Cubre cambios de pestaña/flyout sin pasar por "atras".
            GuardarBorradorLocal();
        }

        protected override bool OnBackButtonPressed()
        {
            if (saliendo || !TieneCambiosSinGuardar())
                return base.OnBackButtonPressed();

            Dispatcher.Dispatch(async () => await IntentarSalirAsync());
            return true;
        }

        private async Task PrepararDatosAsync()
        {
            if (preparandoDatos)
                return;

            preparandoDatos = true;
            try
            {
                await Task.Yield();

                if (!parametrosCargados)
                {
                    await LoadParametroAsync();
                    parametrosCargados = true;
                }

                if (!sucursalesCargadas)
                {
                    await LoadSucursalesAsync();
                }

                if (!despachosCargados)
                {
                    await LoadDespachosAsync();
                }

                if (!lotesCargados)
                {
                    await LoadLotesAsync();
                }

                if (numeroParaCargar.HasValue && !transferenciaCargada)
                {
                    await LoadTransferenciaGuardadaAsync(numeroParaCargar.Value);
                }

                if (numeroParaCargar.HasValue && !lineaBaseFijada)
                {
                    // Lo que disparo la carga inicial (pickers, campos) no cuenta como cambio del usuario.
                    hayCambios = false;
                    lineaBaseFijada = true;
                }

                if (EsNueva && !borradorRevisado)
                {
                    await OfrecerRecuperarBorradorAsync();
                    borradorRevisado = true;
                }
            }
            finally
            {
                preparandoDatos = false;
            }
        }

        private async Task LoadParametroAsync()
        {
            var parameter = await App.Database.GetParameterAsync();
            url = parameter?.IP ?? string.Empty;
        }

        private async Task LoadSucursalesAsync()
        {
            try
            {
                var serviceUrl = Util.NormalizeServiceUrl(url);
                if (string.IsNullOrWhiteSpace(serviceUrl))
                {
                    MostrarEstadoConexion("Configure el servidor en Parámetros.");
                    return;
                }

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(serviceUrl + "/api/Sucursales/"),
                    Method = HttpMethod.Get
                };
                request.Headers.Add("Accept", "application/json");

                var client = Util.CreateHttpClient(TimeSpan.FromSeconds(6));
                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    MostrarEstadoConexion("No se pudieron cargar las sucursales: " + ObtenerDetalleError(content) + "\nServidor: " + serviceUrl);
                    return;
                }

                listSucur = JsonConvert.DeserializeObject<List<Sucursales>>(content) ?? new List<Sucursales>();
                listaSucursal.ItemsSource = listSucur;
                sucursalesCargadas = true;

                if (listSucur.Count == 0)
                {
                    MostrarEstadoConexion("El servidor no devolvió sucursales. Servidor: " + serviceUrl);
                }
                else
                {
                    MostrarEstadoConexion(string.Empty);
                }

                ActualizarEncabezado();
            }
            catch (Exception fail)
            {
                Util.TryPlayAudioFile("no-valido.mp3");
                MostrarEstadoConexion("Sin conexión con el servidor. Revise la red y Parámetros (" + Util.NormalizeHttpExceptionMessage(fail) + ").");
            }
        }

        private void MostrarEstadoConexion(string mensaje)
        {
            estadoConexion.Text = mensaje;
            estadoConexion.IsVisible = !string.IsNullOrWhiteSpace(mensaje);
        }

        private async Task LoadDespachosAsync()
        {
            try
            {
                var serviceUrl = Util.NormalizeServiceUrl(url);
                if (string.IsNullOrWhiteSpace(serviceUrl))
                {
                    MostrarEstadoConexion("Configure el servidor en Parámetros.");
                    return;
                }

                var client = Util.CreateHttpClient(TimeSpan.FromSeconds(6));
                var response = await client.GetAsync(serviceUrl + "/api/Transferencia/despachos");
                var content = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    MostrarEstadoConexion("No se pudieron cargar los despachos: " + ObtenerDetalleError(content));
                    return;
                }

                listaDespachos = JsonConvert.DeserializeObject<List<TransferenciaDespachoItem>>(content) ?? new List<TransferenciaDespachoItem>();
                despacho.ItemsSource = listaDespachos;
                despachosCargados = true;
            }
            catch (Exception fail)
            {
                MostrarEstadoConexion("Sin conexión: no se pudieron cargar los despachos (" + Util.NormalizeHttpExceptionMessage(fail) + ").");
            }
        }

        private async Task LoadLotesAsync()
        {
            try
            {
                var serviceUrl = Util.NormalizeServiceUrl(url);
                if (string.IsNullOrWhiteSpace(serviceUrl))
                {
                    MostrarEstadoConexion("Configure el servidor en Parámetros.");
                    return;
                }

                var client = Util.CreateHttpClient(TimeSpan.FromSeconds(6));
                var response = await client.GetAsync(serviceUrl + "/api/Transferencia/lotes");
                var content = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    MostrarEstadoConexion("No se pudieron cargar los lotes: " + ObtenerDetalleError(content));
                    return;
                }

                listaLotes = JsonConvert.DeserializeObject<List<TransferenciaLoteItem>>(content) ?? new List<TransferenciaLoteItem>();
                lote.ItemsSource = listaLotes;
                lotesCargados = true;
            }
            catch (Exception fail)
            {
                MostrarEstadoConexion("Sin conexión: no se pudieron cargar los lotes (" + Util.NormalizeHttpExceptionMessage(fail) + ").");
            }
        }

        private void ToggleCabecera_Tapped(object sender, EventArgs e)
        {
            cabeceraVisible = !cabeceraVisible;
            cabeceraPanel.IsVisible = cabeceraVisible;
            toggleCabeceraText.Text = cabeceraVisible ? "Ocultar datos de cabecera" : "Ver datos de cabecera";
            toggleCabeceraIcon.Text = cabeceraVisible ? "^" : "v";
        }

        private void ToggleEscaneo_Tapped(object sender, EventArgs e)
        {
            escaneoVisible = !escaneoVisible;
            escaneoPanel.IsVisible = escaneoVisible;
            if (escaneoVisible)
            {
                codigoBarra.Focus();
            }
        }

        private void Numero_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarEncabezado();
            NotificarCambio();
        }

        private void Sucursal_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActualizarEncabezado();
            NotificarCambio();
        }

        private async Task LoadTransferenciaGuardadaAsync(int numeroTransferencia)
        {
            try
            {
                var serviceUrl = Util.NormalizeServiceUrl(url);
                if (string.IsNullOrWhiteSpace(serviceUrl))
                {
                    MostrarEstadoConexion("Configure el servidor en Parámetros.");
                    return;
                }

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(serviceUrl + "/api/Transferencia/" + numeroTransferencia),
                    Method = HttpMethod.Get
                };
                request.Headers.Add("Accept", "application/json");

                var client = Util.CreateHttpClient(TimeSpan.FromSeconds(4));
                var response = await client.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    MostrarEstadoConexion("No se pudo cargar la transferencia guardada.");
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(content);
                var header = data["header"] ?? data["Header"];
                var detallesToken = data["detalles"] ?? data["Detalles"];

                if (header == null)
                {
                    MostrarEstadoConexion("El servidor devolvió la transferencia incompleta.");
                    return;
                }

                numero.Text = Convert.ToString(header["numero"] ?? header["Numero"] ?? numeroTransferencia.ToString());
                documentoOrigen.Text = Convert.ToString(header["documentoOrigen"] ?? header["DocumentoOrigen"] ?? string.Empty);
                observacion.Text = Convert.ToString(header["observacion"] ?? header["Observacion"] ?? string.Empty);
                transferenciaCorreccion.IsToggled = Convert.ToBoolean(header["correccion"] ?? header["Correccion"] ?? false);

                if (DateTime.TryParse(Convert.ToString(header["fecha"] ?? header["Fecha"]), out var fechaValue))
                    fecha.Date = fechaValue;

                if (DateTime.TryParse(Convert.ToString(header["fechaEmision"] ?? header["FechaEmision"]), out var fechaEmisionValue))
                    fechaEmision.Date = fechaEmisionValue;

                SeleccionarSucursalPorCodigo(Convert.ToString(header["codigoRecibe"] ?? header["CodigoRecibe"] ?? string.Empty));
                SeleccionarDespachoPorId(Convert.ToInt32(header["idDespacho"] ?? header["IdDespacho"] ?? 0));
                SeleccionarLotePorId(Convert.ToInt32(header["idLote"] ?? header["IdLote"] ?? 0));

                detalles.Clear();
                if (detallesToken != null)
                {
                    foreach (var detalle in detallesToken)
                    {
                        var codigo = Convert.ToString(detalle["codigoBarra"] ?? detalle["CodigoBarra"] ?? string.Empty);
                        detalles.Add(new TransferenciaDetalleLinea
                        {
                            Item = Convert.ToInt32(detalle["item"] ?? detalle["Item"] ?? detalles.Count + 1),
                            CodigoBarra = codigo,
                            Referencia = codigo,
                            Nombre = string.IsNullOrWhiteSpace(codigo) ? "Articulo guardado" : codigo,
                            NumeroCaja = Convert.ToInt32(detalle["numeroCaja"] ?? detalle["NumeroCaja"] ?? 1),
                            Cantidad = Convert.ToInt32(detalle["cantidad"] ?? detalle["Cantidad"] ?? 0),
                            Valor = Convert.ToDecimal(detalle["valor"] ?? detalle["Valor"] ?? 0)
                        });
                    }
                }

                transferenciaCargada = true;
                Guardar.IsEnabled = true;
                Guardar.Text = "Guardar";
                MostrarEstadoConexion("Transferencia guardada cargada para editar.");
                ActualizarTotales();
                ActualizarEncabezado();
            }
            catch (Exception fail)
            {
                MostrarEstadoConexion("No se pudo cargar la transferencia (" + Util.NormalizeHttpExceptionMessage(fail) + ").");
            }
        }

        private void SeleccionarSucursalPorCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo) || listSucur == null)
                return;

            var sucursal = listSucur.FirstOrDefault(x => string.Equals(x.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
            if (sucursal != null)
                listaSucursal.SelectedItem = sucursal;
        }

        private void SeleccionarDespachoPorId(int idDespacho)
        {
            if (idDespacho <= 0 || listaDespachos == null)
                return;

            var item = listaDespachos.FirstOrDefault(x => x.IdDespacho == idDespacho);
            if (item != null)
                despacho.SelectedItem = item;
        }

        private void SeleccionarLotePorId(int idLote)
        {
            if (idLote <= 0 || listaLotes == null)
                return;

            var item = listaLotes.FirstOrDefault(x => x.IdLote == idLote);
            if (item != null)
                lote.SelectedItem = item;
        }

        private void ActualizarEncabezado()
        {
            var numeroManual = int.TryParse((numero?.Text ?? string.Empty).Trim(), out var numeroValue);
            var numeroTexto = numeroManual && numeroValue > 0 ? " N " + numeroValue : string.Empty;
            tituloTransferencia.Text = "Transferencia" + numeroTexto;

            var sucursal = listaSucursal?.SelectedItem as Sucursales;
            subtituloSucursal.Text = "Sucursal: " + (sucursal?.Nombre ?? string.Empty);
            resumenReferencias.Text = detalles.Count + (detalles.Count == 1 ? " referencia en lista" : " referencias en lista");
        }

        private void SeleccionarCaja(int numeroCaja)
        {
            cajaSeleccionada = numeroCaja;
            cajaDetalle.Text = numeroCaja.ToString();
            PintarCaja(box1Button, numeroCaja == 1);
            PintarCaja(box2Button, numeroCaja == 2);
            PintarCaja(box3Button, numeroCaja == 3);
        }

        private static void PintarCaja(Button boton, bool seleccionada)
        {
            boton.BackgroundColor = seleccionada ? AppColors.Primary : AppColors.SurfaceMuted;
            boton.TextColor = seleccionada ? AppColors.OnPrimary : AppColors.TextSecondary;
        }

        private void BoxButton_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Text, out var numeroCaja))
            {
                SeleccionarCaja(numeroCaja);
            }
        }

        private async void CodigoBarra_Completed(object sender, EventArgs e)
        {
            await AddDetalleAsync();
        }

        private async void CodigoBarra_TextChanged(object sender, TextChangedEventArgs e)
        {
            var codigo = (e.NewTextValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(codigo))
                return;

            var version = ++codigoBarraVersion;
            await Task.Delay(350);

            if (version != codigoBarraVersion)
                return;

            if (!string.Equals(codigo, (codigoBarra.Text ?? string.Empty).Trim(), StringComparison.Ordinal))
                return;

            await AddDetalleAsync();
        }

        private async Task AddDetalleAsync()
        {
            if (agregandoDetalle)
                return;

            if (string.IsNullOrWhiteSpace(codigoBarra.Text))
                return;

            agregandoDetalle = true;
            try
            {
                articuloActual = null;
                await BuscarArticuloAsync();

                if (articuloActual == null)
                    return;

                if (!int.TryParse(cajaDetalle.Text, out var numeroCaja))
                    numeroCaja = cajaSeleccionada;

                if (!int.TryParse(cantidadDetalle.Text, out var cantidad) || cantidad <= 0)
                    cantidad = 1;

                detalles.Add(new TransferenciaDetalleLinea
                {
                    Item = detalles.Count + 1,
                    CodigoBarra = string.IsNullOrWhiteSpace(articuloActual.CodigoBarra) ? codigoBarra.Text?.Trim() : articuloActual.CodigoBarra,
                    Referencia = articuloActual.Referencia,
                    Nombre = articuloActual.Nombre,
                    NumeroCaja = numeroCaja,
                    Cantidad = cantidad,
                    Valor = articuloActual.PrecioDetal
                });

                articuloActual = null;
                codigoBarra.Text = string.Empty;
                cantidadDetalle.Text = "1";
                ActualizarTotales();
                RefrescarDetalle();
                codigoBarra.Focus();
            }
            finally
            {
                agregandoDetalle = false;
            }
        }

        private async Task BuscarArticuloAsync()
        {
            try
            {
                articuloActual = null;
                var codigo = (codigoBarra.Text ?? string.Empty).Trim();
                var serviceUrl = Util.NormalizeServiceUrl(url);

                if (string.IsNullOrWhiteSpace(serviceUrl))
                {
                    await DisplayAlert("Advertencia", "Configure el servidor en Parámetros.", "Aceptar");
                    return;
                }

                if (string.IsNullOrWhiteSpace(codigo))
                {
                    await FeedbackService.ShowAsync("Escanee o escriba un código de barra.");
                    return;
                }

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(serviceUrl + "/api/Articulo/" + codigo),
                    Method = HttpMethod.Get
                };
                request.Headers.Add("Accept", "application/json");

                var client = Util.CreateHttpClient();
                var response = await client.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    DependencyService.Get<IAudio>()?.PlayAudioFile("no-valido.mp3");
                    await DisplayAlert("Error", "No se encontró ningún artículo con ese código.", "Aceptar");
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                var articulos = JsonConvert.DeserializeObject<List<Articulo>>(content) ?? new List<Articulo>();
                articuloActual = articulos.FirstOrDefault();

                if (articuloActual == null)
                {
                    DependencyService.Get<IAudio>()?.PlayAudioFile("no-valido.mp3");
                    await DisplayAlert("Error", "No se encontró ningún artículo con ese código.", "Aceptar");
                }
            }
            catch (Exception fail)
            {
                DependencyService.Get<IAudio>()?.PlayAudioFile("no-valido.mp3");
                await DisplayAlert("Error", Util.NormalizeHttpExceptionMessage(fail), "Aceptar");
            }
        }

        private async void AgregarDetalle_Clicked(object sender, EventArgs e)
        {
            await AddDetalleAsync();
        }

        private async void Guardar_Clicked(object sender, EventArgs e)
        {
            try
            {
                var serviceUrl = Util.NormalizeServiceUrl(url);
                if (string.IsNullOrWhiteSpace(serviceUrl))
                {
                    await DisplayAlert("Advertencia", "Configure el servidor en Parámetros.", "Aceptar");
                    return;
                }

                var sucursal = listaSucursal.SelectedItem as Sucursales;
                if (sucursal == null)
                {
                    await FeedbackService.ShowAsync("Seleccione la sucursal.");
                    return;
                }

                if (detalles.Count == 0)
                {
                    await FeedbackService.ShowAsync("Agregue al menos un artículo.");
                    return;
                }

                var loteSeleccionado = lote.SelectedItem as TransferenciaLoteItem;
                var despachoSeleccionado = despacho.SelectedItem as TransferenciaDespachoItem;
                var idLote = loteSeleccionado?.IdLote ?? 0;
                var idDespacho = despachoSeleccionado?.IdDespacho ?? 0;
                var valorTotal = detalles.Sum(x => x.Valor * x.Cantidad);
                var movimientos = detalles.Select((detalle, index) => new MovTransferencias
                {
                    Fecha = fecha.Date,
                    CodigoBarra = detalle.CodigoBarra,
                    Cantidad = detalle.Cantidad,
                    Valor = detalle.Valor,
                    NumeroCaja = detalle.NumeroCaja,
                    Item = index + 1
                }).ToList();

                var numeroGuardado = int.TryParse(numero.Text, out var numeroExistente) && numeroExistente > 0
                    ? numeroExistente
                    : numeroParaCargar.GetValueOrDefault();

                var transferencia = new Transferencia
                {
                    Numero = numeroGuardado,
                    Fecha = fecha.Date,
                    CodigoEnvia = sucursal.Codigo,
                    CodigoRecibe = sucursal.Codigo,
                    DocumentoOrigen = documentoOrigen.Text ?? string.Empty,
                    TotalValor = valorTotal,
                    Observacion = observacion.Text ?? string.Empty,
                    Status = 0,
                    Usuario = AuthService.CurrentUser?.Username ?? string.Empty,
                    FechaEmision = fechaEmision.Date,
                    IdLote = idLote,
                    IdDespacho = idDespacho,
                    Correccion = transferenciaCorreccion.IsToggled,
                    Detalles = movimientos
                };

                var client = Util.CreateHttpClient();
                var headerContent = new StringContent(JsonConvert.SerializeObject(transferencia), Encoding.UTF8, "application/json");
                var esTransferenciaExistente = transferencia.Numero > 0;
                var headerResponse = esTransferenciaExistente
                    ? await client.PutAsync(serviceUrl + "/api/Transferencia/" + transferencia.Numero, headerContent)
                    : await client.PostAsync(serviceUrl + "/api/Transferencia", headerContent);
                if (!await ValidarInsercionAsync(headerResponse, "El servidor no guardó la transferencia."))
                    return;

                if (EsNueva)
                    EliminarBorradorLocal();

                saliendo = true;
                await FeedbackService.ShowAsync("Transferencia guardada.");
                await Navigation.PopAsync(false);
            }
            catch (Exception fail)
            {
                await DisplayAlert("Error", Util.NormalizeHttpExceptionMessage(fail), "Aceptar");
            }
        }

        private void SumarCantidad_Clicked(object sender, EventArgs e)
        {
            if (ObtenerDetalle(sender) is not TransferenciaDetalleLinea detalle)
                return;

            detalle.Cantidad += 1;
            RefrescarDetalle();
        }

        private void RestarCantidad_Clicked(object sender, EventArgs e)
        {
            if (ObtenerDetalle(sender) is not TransferenciaDetalleLinea detalle)
                return;

            if (detalle.Cantidad > 1)
            {
                detalle.Cantidad -= 1;
                RefrescarDetalle();
            }
        }

        private void EliminarDetalle_Clicked(object sender, EventArgs e)
        {
            if (ObtenerDetalle(sender) is not TransferenciaDetalleLinea detalle)
                return;

            detalles.Remove(detalle);
            ReenumerarDetalles();
            RefrescarDetalle();
        }

        private static TransferenciaDetalleLinea ObtenerDetalle(object sender)
        {
            return (sender as Button)?.CommandParameter as TransferenciaDetalleLinea;
        }

        private void ReenumerarDetalles()
        {
            for (var i = 0; i < detalles.Count; i++)
                detalles[i].Item = i + 1;
        }

        private void RefrescarDetalle()
        {
            detalleGrid.ItemsSource = null;
            detalleGrid.ItemsSource = detalles;
            ActualizarTotales();
            NotificarCambio();
        }

        private async Task<bool> ValidarInsercionAsync(HttpResponseMessage response, string mensaje)
        {
            var resultText = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                await DisplayAlert("Error", mensaje + Environment.NewLine + ObtenerDetalleError(resultText), "Aceptar");
                return false;
            }

            if (!int.TryParse(resultText, out var result) || result < 1)
            {
                try
                {
                    var json = JObject.Parse(resultText);
                    var ok = json.Value<bool?>("ok") ?? false;
                    var numero = json.Value<string>("Numero");

                    if (ok && !string.IsNullOrWhiteSpace(numero))
                        return true;
                }
                catch
                {
                    // La respuesta no vino como JSON del escucha compartido.
                }

                await DisplayAlert("Error", mensaje + Environment.NewLine + ObtenerDetalleError(resultText), "Aceptar");
                return false;
            }

            return true;
        }

        private static string ObtenerDetalleError(string resultText)
        {
            if (string.IsNullOrWhiteSpace(resultText))
                return "Sin detalle del servidor.";

            try
            {
                var json = JObject.Parse(resultText);
                var error = json.Value<string>("error");
                var inner = json.Value<string>("inner");

                if (!string.IsNullOrWhiteSpace(inner))
                    return error + Environment.NewLine + inner;

                if (!string.IsNullOrWhiteSpace(error))
                    return error;
            }
            catch
            {
                // Mantiene abajo el texto limpio de la respuesta.
            }

            return resultText.Length > 240 ? resultText.Substring(0, 240) + "..." : resultText;
        }

        private async void Cancelar_Clicked(object sender, EventArgs e)
        {
            await IntentarSalirAsync();
        }

        #region Borrador local y confirmacion al salir

        private void NotificarCambio()
        {
            if (suspenderSeguimiento)
                return;

            hayCambios = true;
            GuardarBorradorLocal();
        }

        private bool TieneCambiosSinGuardar()
        {
            if (EsNueva)
            {
                return detalles.Count > 0
                    || !string.IsNullOrWhiteSpace(documentoOrigen.Text)
                    || !string.IsNullOrWhiteSpace(observacion.Text);
            }

            return hayCambios;
        }

        private async Task IntentarSalirAsync()
        {
            if (saliendo)
                return;

            if (!TieneCambiosSinGuardar())
            {
                saliendo = true;
                await Navigation.PopAsync(false);
                return;
            }

            if (EsNueva)
            {
                const string conservar = "Salir y conservar borrador";
                const string descartar = "Descartar transferencia";
                var opcion = await DisplayActionSheet(
                    "La transferencia no se ha guardado en el servidor (" + detalles.Count + " artículo(s)).",
                    "Seguir editando", descartar, conservar);

                if (opcion == conservar)
                {
                    GuardarBorradorLocal();
                }
                else if (opcion == descartar)
                {
                    EliminarBorradorLocal();
                }
                else
                {
                    return;
                }
            }
            else
            {
                var salir = await DisplayAlert(
                    "Cambios sin guardar",
                    "¿Salir sin guardar los cambios de la transferencia " + numeroParaCargar + "?",
                    "Salir", "Seguir editando");
                if (!salir)
                    return;
            }

            saliendo = true;
            await Navigation.PopAsync(false);
        }

        private void GuardarBorradorLocal()
        {
            // Solo para transferencias nuevas, y nunca antes de haber ofrecido recuperar el anterior
            // (si no, la carga inicial lo sobrescribiria).
            if (!EsNueva || !borradorRevisado || suspenderSeguimiento || saliendo)
                return;

            try
            {
                if (!TieneCambiosSinGuardar())
                {
                    EliminarBorradorLocal();
                    return;
                }

                var borrador = new BorradorTransferencia
                {
                    GuardadoEn = DateTime.Now,
                    Numero = numero.Text ?? string.Empty,
                    DocumentoOrigen = documentoOrigen.Text ?? string.Empty,
                    Observacion = observacion.Text ?? string.Empty,
                    Correccion = transferenciaCorreccion.IsToggled,
                    Fecha = fecha.Date,
                    FechaEmision = fechaEmision.Date,
                    CodigoSucursal = (listaSucursal.SelectedItem as Sucursales)?.Codigo ?? string.Empty,
                    IdDespacho = (despacho.SelectedItem as TransferenciaDespachoItem)?.IdDespacho ?? 0,
                    IdLote = (lote.SelectedItem as TransferenciaLoteItem)?.IdLote ?? 0,
                    Detalles = detalles.ToList()
                };

                Preferences.Set(ClaveBorrador, JsonConvert.SerializeObject(borrador));
            }
            catch (Exception ex)
            {
                CrashReportService.LogException(ex, "NuevaTransferenciaPage.GuardarBorradorLocal");
            }
        }

        private static void EliminarBorradorLocal()
        {
            Preferences.Remove(ClaveBorrador);
        }

        private async Task OfrecerRecuperarBorradorAsync()
        {
            BorradorTransferencia borrador;
            try
            {
                var json = Preferences.Get(ClaveBorrador, string.Empty);
                if (string.IsNullOrWhiteSpace(json))
                    return;

                borrador = JsonConvert.DeserializeObject<BorradorTransferencia>(json);
            }
            catch (Exception ex)
            {
                CrashReportService.LogException(ex, "NuevaTransferenciaPage.LeerBorradorLocal");
                EliminarBorradorLocal();
                return;
            }

            if (borrador == null)
                return;

            var cantidadLineas = borrador.Detalles?.Count ?? 0;
            var recuperar = await DisplayAlert(
                "Transferencia sin guardar",
                "Hay una transferencia sin guardar del " + borrador.GuardadoEn.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture)
                    + " con " + cantidadLineas + " artículo(s). ¿Desea recuperarla?",
                "Recuperar", "Descartar");

            if (!recuperar)
            {
                EliminarBorradorLocal();
                return;
            }

            suspenderSeguimiento = true;
            try
            {
                numero.Text = borrador.Numero;
                documentoOrigen.Text = borrador.DocumentoOrigen;
                observacion.Text = borrador.Observacion;
                transferenciaCorreccion.IsToggled = borrador.Correccion;
                if (borrador.Fecha != default)
                    fecha.Date = borrador.Fecha;
                if (borrador.FechaEmision != default)
                    fechaEmision.Date = borrador.FechaEmision;

                SeleccionarSucursalPorCodigo(borrador.CodigoSucursal);
                SeleccionarDespachoPorId(borrador.IdDespacho);
                SeleccionarLotePorId(borrador.IdLote);

                detalles.Clear();
                foreach (var linea in borrador.Detalles ?? new List<TransferenciaDetalleLinea>())
                    detalles.Add(linea);
                ReenumerarDetalles();

                detalleGrid.ItemsSource = null;
                detalleGrid.ItemsSource = detalles;
                ActualizarTotales();
            }
            finally
            {
                suspenderSeguimiento = false;
            }

            MostrarEstadoConexion("Transferencia sin guardar recuperada. Recuerde pulsar Guardar.");
        }

        private sealed class BorradorTransferencia
        {
            public DateTime GuardadoEn { get; set; }
            public string Numero { get; set; }
            public string DocumentoOrigen { get; set; }
            public string Observacion { get; set; }
            public bool Correccion { get; set; }
            public DateTime Fecha { get; set; }
            public DateTime FechaEmision { get; set; }
            public string CodigoSucursal { get; set; }
            public int IdDespacho { get; set; }
            public int IdLote { get; set; }
            public List<TransferenciaDetalleLinea> Detalles { get; set; }
        }

        #endregion

        private void ActualizarTotales()
        {
            var cantidad = detalles.Sum(x => x.Cantidad);
            var totalCajaUno = detalles.Where(x => x.NumeroCaja == 1).Sum(x => x.Cantidad);
            cantidadTotal.Text = cantidad.ToString("0.00", CultureInfo.CurrentCulture);
            totalCaja.Text = totalCajaUno.ToString("0.00", CultureInfo.CurrentCulture);
            ActualizarEncabezado();
        }

        public class TransferenciaDetalleLinea
        {
            public int Item { get; set; }
            public string CodigoBarra { get; set; }
            public string Referencia { get; set; }
            public string Nombre { get; set; }
            public int NumeroCaja { get; set; }
            public int Cantidad { get; set; }
            public int Lote { get; set; }
            public decimal Valor { get; set; }
        }

        public class TransferenciaDespachoItem
        {
            public int IdDespacho { get; set; }
            public string Descripcion { get; set; }
        }

        public class TransferenciaLoteItem
        {
            public int IdLote { get; set; }
            public string Lote { get; set; }
        }
    }
}
