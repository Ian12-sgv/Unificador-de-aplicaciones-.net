using Appinventario.Models;
using Appinventario.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ModuleAdminPage : ContentPage
    {
        private readonly ObservableCollection<ModuleState> modules = new ObservableCollection<ModuleState>();

        public ModuleAdminPage()
        {
            InitializeComponent();
            listModules.ItemsSource = modules;
            LoadModules();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (AuthService.CurrentUser == null || !AuthService.CurrentUser.CanManageModules)
                Application.Current.MainPage = new LoginPage();
        }

        private void LoadModules()
        {
            modules.Clear();

            foreach (var module in AuthService.AllModules)
            {
                if (module.IsAlwaysVisible)
                    continue;

                modules.Add(new ModuleState
                {
                    Key = module.Key,
                    Title = module.Title,
                    IsEnabled = AuthService.IsModuleEnabled(module.Key)
                });
            }
        }

        private void OnModuleToggled(object sender, ToggledEventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is ModuleState module)
                AuthService.SetModuleEnabled(module.Key, e.Value);
        }

        private async void OnApplyClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Información", "Cambios aplicados.", "ok");
            Application.Current.MainPage = new AppShell();
        }

        private class ModuleState : INotifyPropertyChanged
        {
            private bool isEnabled;

            public string Key { get; set; }
            public string Title { get; set; }

            public bool IsEnabled
            {
                get => isEnabled;
                set
                {
                    if (isEnabled == value)
                        return;

                    isEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
