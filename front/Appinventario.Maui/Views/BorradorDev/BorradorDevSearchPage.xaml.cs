using Appinventario.Models.BorradorDev;
using Appinventario.Services;
using Appinventario.Services.BorradorDev;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Controls.Xaml;

namespace Appinventario.Views.BorradorDev
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BorradorDevSearchPage : ContentPage
    {
        private readonly BorradorDevApiService apiService;
        private bool isBusy;
        private bool isClosing;
        private CancellationTokenSource searchDelayCts;
        private int searchVersion;
        private const int PageSize = 10;
        private readonly List<BorradorDevListItem> cachedItems = new List<BorradorDevListItem>();
        private int visibleCount;

        public event Action<long> NumeroSelected;

        public BorradorDevSearchPage(BorradorDevApiService apiService)
        {
            InitializeComponent();
            this.apiService = apiService;

            ToolbarItems.Add(new ToolbarItem("Cerrar", null, async () =>
            {
                isClosing = true;
                CancelPendingSearch();
                await Navigation.PopModalAsync();
            }));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                isClosing = false;
                await LoadAsync(txtSearch.Text);
            }
            catch (Exception ex)
            {
                await CrashReportService.LogExceptionAsync(ex, "BorradorDevSearchPage.OnAppearing");
                SetStatus("Error al iniciar la búsqueda. Intenta de nuevo.", true);
            }
        }

        protected override void OnDisappearing()
        {
            isClosing = true;
            CancelPendingSearch();
            base.OnDisappearing();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            CancelPendingSearch();

            if (string.IsNullOrWhiteSpace(e.NewTextValue) || e.NewTextValue.Trim().Length >= 2)
            {
                searchDelayCts = new CancellationTokenSource();
                var version = Interlocked.Increment(ref searchVersion);
                _ = DebouncedLoadAsync(e.NewTextValue, searchDelayCts.Token, version);
            }
        }

        private async Task DebouncedLoadAsync(string query, CancellationToken cancellationToken, int version)
        {
            try
            {
                await Task.Delay(250, cancellationToken);
                if (!cancellationToken.IsCancellationRequested && !isClosing && version == searchVersion)
                {
                    await LoadAsync(query);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignorar cancelaciones debounced.
            }
            catch (Exception ex)
            {
                await CrashReportService.LogExceptionAsync(ex, "BorradorDevSearchPage.DebouncedLoadAsync");
                SetStatus("Error de búsqueda. Intenta de nuevo.", true);
            }
        }

        private async void OnSearchCompleted(object sender, EventArgs e)
        {
            try
            {
                CancelPendingSearch();
                await LoadAsync(txtSearch.Text);
            }
            catch (Exception ex)
            {
                await CrashReportService.LogExceptionAsync(ex, "BorradorDevSearchPage.OnSearchCompleted");
                SetStatus("Error al ejecutar la búsqueda. Intenta de nuevo.", true);
            }
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            try
            {
                CancelPendingSearch();
                await LoadAsync(txtSearch.Text);
            }
            catch (Exception ex)
            {
                await CrashReportService.LogExceptionAsync(ex, "BorradorDevSearchPage.OnSearchClicked");
                SetStatus("Error al ejecutar la búsqueda. Intenta de nuevo.", true);
            }
        }

        private async void OnResultTapped(object sender, TappedEventArgs e)
        {
            try
            {
                var item = e.Parameter as BorradorDevListItem;

                if (item == null)
                    return;

                NumeroSelected?.Invoke(item.Numero);
                isClosing = true;
                CancelPendingSearch();
                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await CrashReportService.LogExceptionAsync(ex, "BorradorDevSearchPage.OnResultTapped");
                SetStatus("Error al seleccionar el número. Intenta de nuevo.", true);
            }
        }

        private void OnMoreClicked(object sender, EventArgs e)
        {
            visibleCount += PageSize;
            RenderResults();
        }

        private async System.Threading.Tasks.Task LoadAsync(string query)
        {
            if (isBusy || isClosing)
                return;

            try
            {
                isBusy = true;
                SetStatus("Buscando...", false);

                var items = await apiService.ListAsync(query, 50, null);
                cachedItems.Clear();
                cachedItems.AddRange(items ?? new List<BorradorDevListItem>());
                visibleCount = PageSize;
                RenderResults();

                if (cachedItems.Count == 0)
                    SetStatus("Sin resultados.", false);
                else
                    SetStatus("Selecciona un numero para seguir.", false);
            }
            catch (Exception ex)
            {
                cachedItems.Clear();
                RenderResults();
                await CrashReportService.LogExceptionAsync(ex, "BorradorDevSearchPage.LoadAsync");
                SetStatus("No se pudo completar la búsqueda. Revisa la conexión.", true);
            }
            finally
            {
                isBusy = false;
            }
        }

        private void RenderResults()
        {
            stackResults.Children.Clear();

            var count = Math.Min(visibleCount, cachedItems.Count);
            for (var index = 0; index < count; index++)
            {
                var item = cachedItems[index];
                var card = CreateResultCard(item);
                stackResults.Children.Add(card);
            }

            btnMore.IsVisible = count < cachedItems.Count;
        }

        private View CreateResultCard(BorradorDevListItem item)
        {
            var card = new Border
            {
                Padding = new Thickness(12, 10),
                BackgroundColor = Colors.White,
                Stroke = Color.FromHex("#D6E0E5"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 8
                }
            };

            var layout = new Grid
            {
                RowSpacing = 4,
                ColumnSpacing = 8
            };

            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var numero = new Label
            {
                Text = item.NumeroDisplay,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                TextColor = Color.FromHex("#24343A"),
                VerticalTextAlignment = TextAlignment.Center
            };

            var status = new Label
            {
                Text = item.StatusDisplay,
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                TextColor = item.Status.HasValue && item.Status.Value == 1
                    ? Color.FromHex("#8A5A00")
                    : Color.FromHex("#166534"),
                HorizontalTextAlignment = TextAlignment.End,
                LineBreakMode = LineBreakMode.TailTruncation
            };

            var meta = new Label
            {
                Text = item.FechaDisplay + " | " + item.UsuarioDisplay,
                FontSize = 12,
                TextColor = Color.FromHex("#60727A"),
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1
            };

            var observacion = new Label
            {
                Text = item.ObservacionDisplay,
                FontSize = 13,
                TextColor = Color.FromHex("#344951"),
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1
            };

            Grid.SetRow(numero, 0);
            Grid.SetColumn(numero, 0);
            Grid.SetRow(status, 0);
            Grid.SetColumn(status, 1);
            Grid.SetRow(meta, 1);
            Grid.SetColumn(meta, 0);
            Grid.SetColumnSpan(meta, 2);
            Grid.SetRow(observacion, 2);
            Grid.SetColumn(observacion, 0);
            Grid.SetColumnSpan(observacion, 2);

            layout.Children.Add(numero);
            layout.Children.Add(status);
            layout.Children.Add(meta);
            layout.Children.Add(observacion);
            card.Content = layout;

            var tap = new TapGestureRecognizer { CommandParameter = item };
            tap.Tapped += OnResultTapped;
            card.GestureRecognizers.Add(tap);
            return card;
        }

        private void SetStatus(string message, bool isError)
        {
            if (isClosing)
                return;

            lblStatus.Text = message;
            lblStatus.TextColor = isError ? Color.FromHex("#9C2F27") : Color.FromHex("#1E4855");
            frameStatus.IsVisible = !string.IsNullOrWhiteSpace(message);
            frameStatus.BackgroundColor = isError ? Color.FromHex("#FDECEA") : Color.FromHex("#EEF5F7");
            frameStatus.Stroke = new SolidColorBrush(isError ? Color.FromHex("#F4C7C3") : Color.FromHex("#D6E0E5"));
        }

        private void CancelPendingSearch()
        {
            Interlocked.Increment(ref searchVersion);
            var cts = searchDelayCts;
            searchDelayCts = null;
            if (cts == null)
                return;

            try
            {
                cts.Cancel();
            }
            catch
            {
                // Ignore cancellation races during rotation/close.
            }
            finally
            {
                cts.Dispose();
            }
        }
    }
}
