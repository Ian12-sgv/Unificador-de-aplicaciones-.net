using Appinventario.Models.BorradorDev;
using Appinventario.Services.BorradorDev;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Appinventario.Views.BorradorDev
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BorradorDevSearchPage : ContentPage
    {
        private readonly BorradorDevApiService apiService;
        private bool isBusy;

        public event Action<long> NumeroSelected;

        public BorradorDevSearchPage(BorradorDevApiService apiService)
        {
            InitializeComponent();
            this.apiService = apiService;

            ToolbarItems.Add(new ToolbarItem("Cerrar", null, async () =>
            {
                await Navigation.PopModalAsync();
            }));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAsync(searchBar.Text);
        }

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.NewTextValue) || e.NewTextValue.Trim().Length >= 2)
                await LoadAsync(e.NewTextValue);
        }

        private async void OnSearchButtonPressed(object sender, EventArgs e)
        {
            await LoadAsync(searchBar.Text);
        }

        private async void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            var item = e.Item as BorradorDevListItem;
            if (item == null)
                return;

            if (NumeroSelected != null)
                NumeroSelected(item.Numero);

            listResults.SelectedItem = null;
            await Navigation.PopModalAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync(string query)
        {
            if (isBusy)
                return;

            try
            {
                isBusy = true;
                SetStatus("Buscando...", false);

                var items = await apiService.ListAsync(query, 50, null);
                listResults.ItemsSource = items ?? new List<BorradorDevListItem>();

                if (items == null || items.Count == 0)
                    SetStatus("Sin resultados.", false);
                else
                    SetStatus("Selecciona un numero para seguir.", false);
            }
            catch (Exception ex)
            {
                listResults.ItemsSource = new List<BorradorDevListItem>();
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
            lblStatus.TextColor = isError ? Color.FromHex("#9C2F27") : Color.FromHex("#1E4855");
            frameStatus.IsVisible = !string.IsNullOrWhiteSpace(message);
            frameStatus.BackgroundColor = isError ? Color.FromHex("#FDECEA") : Color.FromHex("#EEF5F7");
            frameStatus.BorderColor = isError ? Color.FromHex("#F4C7C3") : Color.FromHex("#D6E0E5");
        }
    }
}
