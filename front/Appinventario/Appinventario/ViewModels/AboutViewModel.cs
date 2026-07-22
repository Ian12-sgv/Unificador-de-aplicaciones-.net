using Appinventario.Services;
using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Appinventario.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {

        #region Constructor
        public AboutViewModel()
        {
            SelectCommand = new Command(OnSelect);
            OnSelect();
            VersionTracking.Track();
            var currentBuild = VersionTracking.CurrentVersion;

            Title = "Acerca de";
            AppC = "CONETSOFT C.A. & Palacio de Blumer ® stock+";
            VersionEti = currentBuild;//@"Versión 1.0.0.400";
            DerechosCo = "COPYRIGHT © " + DateTime.Now.Year.ToString();

        }
        #endregion

        #region Atributtes
        public Command SelectCommand { get; set; }
        public ICommand OpenWebCommand { get; }

        #endregion

        #region Methods
        public async void OnSelect()
        {
            
        }
        #endregion

    }
}