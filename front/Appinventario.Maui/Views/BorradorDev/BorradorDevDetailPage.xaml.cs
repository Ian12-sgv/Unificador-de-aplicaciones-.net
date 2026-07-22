using Appinventario.Models.BorradorDev;
using Appinventario.Services.BorradorDev;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Appinventario.Views.BorradorDev
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BorradorDevDetailPage : ContentPage
    {
        private readonly BorradorDevApiService apiService;
        private readonly long numero;
        private readonly ObservableCollection<DetailRowState> rows = new ObservableCollection<DetailRowState>();
        private bool locked;
        private bool isBusy;

        public BorradorDevDetailPage(BorradorDevApiService apiService, long numero)
        {
            InitializeComponent();
            this.apiService = apiService;
            this.numero = numero;
            Title = "Detalle #" + numero;
            listDetails.ItemsSource = rows;

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
                    rows.Clear();
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

            foreach (var item in detalles.OrderBy(x => x.Item ?? int.MaxValue))
            {
                rows.Add(new DetailRowState
                {
                    Item = item,
                    CantidadText = item.Cantidad.HasValue ? item.Cantidad.Value.ToString("0.##", CultureInfo.InvariantCulture) : "0",
                    CanEdit = !locked
                });
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
                    var rawValue = (row.CantidadText ?? string.Empty).Trim().Replace(",", ".");

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

        private class DetailRowState
        {
            public BorradorDevDetailItem Item { get; set; }
            public string CantidadText { get; set; }
            public bool CanEdit { get; set; }

            public string CodigoDisplay
            {
                get { return "Codigo: " + (Item != null && !string.IsNullOrWhiteSpace(Item.CodigoBarra) ? Item.CodigoBarra : "-"); }
            }

            public string CajaDisplay
            {
                get { return "Caja: " + (Item != null && Item.NumeroCaja.HasValue ? Item.NumeroCaja.Value.ToString() : "-"); }
            }

            public string ReferenciaDisplay
            {
                get { return "Referencia: " + (Item != null && !string.IsNullOrWhiteSpace(Item.Referencia) ? Item.Referencia : "-"); }
            }

            public string NombreDisplay
            {
                get { return "Nombre: " + (Item != null && !string.IsNullOrWhiteSpace(Item.Nombre) ? Item.Nombre : "-"); }
            }
        }
    }
}

