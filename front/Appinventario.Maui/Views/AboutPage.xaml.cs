using Appinventario.Interfaces;
using Appinventario.Services;
using Appinventario.ViewModels;
using System;
using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Appinventario.Views
{
    public partial class AboutPage : ContentPage
    {
        AboutViewModel _viewModel;

        public AboutPage()
        {
            InitializeComponent();
            //BindingContext = _viewModel = new AboutViewModel();
            //if (_viewModel.SelectCommand.CanExecute(null))
            //    _viewModel.SelectCommand.Execute(null);
            try
            {
                var phoneReader = DependencyService.Get<IReadPhoneState>();
                Util.Imei = phoneReader?.GetPhoneIMEI() ?? string.Empty;
            }
            catch
            {
                Util.Imei = string.Empty;
            }

            if (lblImei != null)
            {
                lblImei.Text = string.IsNullOrWhiteSpace(Util.Imei)
                    ? "IMEI: no disponible"
                    : $"IMEI: {Util.Imei}";
            }
        }
    }
}
