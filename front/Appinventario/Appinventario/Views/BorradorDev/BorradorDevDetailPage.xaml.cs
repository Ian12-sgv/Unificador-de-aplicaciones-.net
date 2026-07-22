using Appinventario.Models.BorradorDev;
using Appinventario.Services.BorradorDev;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Appinventario.Views.BorradorDev
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BorradorDevDetailPage : ContentPage
    {
        private readonly BorradorDevApiService apiService;
        private readonly long numero;
        private readonly List<DetailRowState> rows = new List<DetailRowState>();
        private bool locked;
        private bool isBusy;

        public BorradorDevDetailPage(BorradorDevApiService apiService, long numero)
        {
            InitializeComponent();
            this.apiService = apiService;
            this.numero = numero;
            Title = "Detalle #" + numero;

            ToolbarItems.Add(new ToolbarItem("Recargar", null, async () =>
            {
                await LoadAsync(true);
            }));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAsync(false);
        }

        private async System.Threading.Tasks.Task LoadAsync(bool force)
        {
            if (isBusy && !force)
                return;

            try
            {
                isBusy = true;
                SetStatus("Cargando detalle...", false);

                var response = await apiService.GetDetalleAsync(numero);
                if (response == null || response.Header == null)
                {
                    SetStatus("Ese numero no existe en la base de datos.", true);
                    stackDetails.Children.Clear();
                    frameLock.IsVisible = false;
                    lblEmpty.IsVisible = true;
                    btnSave.IsEnabled = false;
                    btnSave.Text = "Guardar cambios";
                    return;
                }

                FillHeader(response.Header);
                locked = response.Header.Status.HasValue && response.Header.Status.Value == 1;
                frameLock.IsVisible = locked;
                lblLock.Text = locked ? "Aprobado (Status=1): solo lectura." : string.Empty;
                btnSave.IsEnabled = !locked;
                btnSave.Text = locked ? "Aprobado (solo lectura)" : "Guardar cambios";
                lblEmpty.IsVisible = false;
                RenderDetails(response.Detalles ?? new List<BorradorDevDetailItem>());

                SetStatus("Lineas cargadas: " + rows.Count, false);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, true);
            }
            finally
            {
                isBusy = false;
            }
        }

        private void FillHeader(BorradorDevHeader header)
        {
            lblHeaderNumero.Text = "Numero: " + header.Numero;
            lblHeaderFecha.Text = "Fecha: " + (header.Fecha.HasValue ? header.Fecha.Value.ToString("yyyy-MM-dd") : "-");
            lblHeaderUsuario.Text = "Usuario: " + (string.IsNullOrWhiteSpace(header.Usuario) ? "-" : header.Usuario);
            lblHeaderObservacion.Text = "Observacion: " + (string.IsNullOrWhiteSpace(header.Observacion) ? "-" : header.Observacion);
            lblHeaderStatus.Text = "Status: " + (header.Status.HasValue ? header.Status.Value.ToString() : "-");
        }

        private void RenderDetails(IList<BorradorDevDetailItem> detalles)
        {
            rows.Clear();
            stackDetails.Children.Clear();

            foreach (var item in detalles.OrderBy(x => x.NumeroCaja).ThenBy(x => x.CodigoBarra))
            {
                var cantidadEntry = new Entry
                {
                    Text = item.Cantidad.HasValue ? item.Cantidad.Value.ToString("0.##", CultureInfo.InvariantCulture) : "0",
                    Keyboard = Keyboard.Numeric,
                    IsEnabled = !locked,
                    BackgroundColor = Color.FromHex("#FAFBFC"),
                    TextColor = Color.FromHex("#1D2E35")
                };

                var tagsLayout = new FlexLayout
                {
                    Direction = FlexDirection.Row,
                    Wrap = FlexWrap.Wrap,
                    AlignItems = FlexAlignItems.Start,
                    JustifyContent = FlexJustify.Start
                };

                tagsLayout.Children.Add(CreateBadge("Codigo", item.CodigoBarra ?? "-"));
                tagsLayout.Children.Add(CreateBadge("Caja", item.NumeroCaja.HasValue ? item.NumeroCaja.Value.ToString() : "-"));

                var frame = new Frame
                {
                    CornerRadius = 10,
                    HasShadow = false,
                    BorderColor = Color.FromHex("#D9E1E5"),
                    Padding = 12,
                    BackgroundColor = Color.White,
                    Content = new StackLayout
                    {
                        Spacing = 8,
                        Children =
                        {
                            tagsLayout,
                            new Label { Text = "Referencia: " + (item.Referencia ?? "-"), FontAttributes = FontAttributes.Bold, TextColor = Color.FromHex("#24343A") },
                            new Label { Text = "Nombre: " + (item.Nombre ?? "-"), TextColor = Color.FromHex("#4F646D") },
                            new Label { Text = "Cantidad", FontSize = 12, TextColor = Color.FromHex("#60727A") },
                            cantidadEntry
                        }
                    }
                };

                ((StackLayout)frame.Content).Children.Add(new Label
                {
                    Text = "Solo se puede editar cantidad.",
                    FontSize = 12,
                    TextColor = Color.FromHex("#60727A")
                });

                rows.Add(new DetailRowState
                {
                    Item = item,
                    CantidadEntry = cantidadEntry
                });

                stackDetails.Children.Add(frame);
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (locked)
            {
                SetStatus("Este numero esta aprobado y no se puede editar.", true);
                return;
            }

            if (isBusy)
                return;

            try
            {
                isBusy = true;
                var updates = new List<BorradorDevUpdateRequestItem>();

                foreach (var row in rows)
                {
                    decimal cantidad;
                    var rawValue = (row.CantidadEntry.Text ?? string.Empty).Trim().Replace(",", ".");

                    if (!decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out cantidad) || cantidad <= 0)
                    {
                        SetStatus("Hay cantidades invalidas. Solo se permiten numeros mayores a cero.", true);
                        return;
                    }

                    updates.Add(new BorradorDevUpdateRequestItem
                    {
                        CodigoBarra = row.Item.CodigoBarra,
                        Cantidad = cantidad,
                        NumeroCaja = row.Item.NumeroCaja ?? 1,
                        NumeroCajaOriginal = row.Item.NumeroCaja ?? 1
                    });
                }

                SetStatus("Guardando cambios...", false);
                await apiService.UpdateDetalleAsync(numero, updates);
                SetStatus("Guardado con exito.", false);
                isBusy = false;
                await LoadAsync(true);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, true);
            }
            finally
            {
                isBusy = false;
            }
        }

        private void SetStatus(string message, bool isError)
        {
            lblStatus.Text = message;
            lblStatus.TextColor = isError ? Color.FromHex("#9C2F27") : Color.FromHex("#166534");
            frameStatus.IsVisible = !string.IsNullOrWhiteSpace(message);
            frameStatus.BackgroundColor = isError ? Color.FromHex("#FDECEA") : Color.FromHex("#E8F5EE");
            frameStatus.BorderColor = isError ? Color.FromHex("#F4C7C3") : Color.FromHex("#B7E1C3");
        }

        private View CreateBadge(string title, string value)
        {
            return new Frame
            {
                Padding = new Thickness(8, 4),
                Margin = new Thickness(0, 0, 8, 6),
                CornerRadius = 14,
                HasShadow = false,
                BorderColor = Color.FromHex("#D6E0E5"),
                BackgroundColor = Color.FromHex("#EEF5F7"),
                Content = new Label
                {
                    Text = title + ": " + value,
                    FontSize = 12,
                    TextColor = Color.FromHex("#1E4855")
                }
            };
        }

        private class DetailRowState
        {
            public BorradorDevDetailItem Item { get; set; }
            public Entry CantidadEntry { get; set; }
        }
    }
}
