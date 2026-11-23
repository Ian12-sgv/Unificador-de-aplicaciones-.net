using Appinventario.Services;
using Appinventario.Views;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Appinventario
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            BindingContext = this;
            BuildMenu();
        }

        public ICommand HelpCommand => new Command(async () => await Salir());

        public ICommand LogoutCommand => HelpCommand;

        private void BuildMenu()
        {
            Items.Clear();

            foreach (var module in AuthService.GetVisibleModules())
            {
                var item = new FlyoutItem
                {
                    Title = module.Title,
                    Icon = module.Icon
                };

                item.Items.Add(new ShellContent
                {
                    Route = module.Route,
                    ContentTemplate = new DataTemplate(module.PageType)
                });

                Items.Add(item);
            }
        }

        private async Task Salir()
        {
            var action = await DisplayAlert("Cerrar sesión", "¿Desea cerrar la sesión?", "Cerrar sesión", "Cancelar");
            if (!action)
                return;

            AuthService.Logout();
            TomaFisicaAuthService.Clear();
            Application.Current.MainPage = new LoginPage();
        }
    }
}
