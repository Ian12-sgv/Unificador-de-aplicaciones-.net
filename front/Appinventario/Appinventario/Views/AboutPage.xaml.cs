using Appinventario.Interfaces;
using Appinventario.Services;
using Appinventario.ViewModels;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

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
            Util.Imei  = DependencyService.Get<IReadPhoneState>().GetPhoneIMEI();
           lblImei.Text = $"IMEI: {Util.Imei}";
        }
    }
}