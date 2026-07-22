using Appinventario.ViewModels;
using Appinventario.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;

namespace Appinventario
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            InitializeComponent();
          //  Routing.RegisterRoute(nameof(NewItemPage), typeof(NewItemPage));
            this.BindingContext = this;
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }
        //  public ICommand HelpCommand => new Command(() => this.DisplayAlert("CONETSOFT","Desea salir?", "SI","NO"));
         public ICommand HelpCommand => new Command(() => Salir());


        private async Task Salir()
        {
            var action = await DisplayAlert("CONETSOFT", "Desea salir?", "SI", "NO");
            if (action)
            {
                 System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
                 System.Diagnostics.Process.GetCurrentProcess().Kill();
            }

        }
























    }
}
