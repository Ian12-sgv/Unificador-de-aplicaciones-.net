using Appinventario.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;
using Appinventario.Data;
using Appinventario.Services;

namespace Appinventario.ViewModels
{
    public class ParameterViewModel : BaseViewModel
    {
        #region Constructor
        public ParameterViewModel()
        {
            SaveCommand = new Command(OnSave, ValidateSave);
            CancelCommand = new Command(OnCancel);
            SelectCommand = new Command(OnSelect);
            this.PropertyChanged += (_, __) => SaveCommand.ChangeCanExecute();
        }
        #endregion

        #region Atributtes
        private string message;
        private string sucursal;
        private string ip;
        private int id;
        private bool idimei;
        private bool testGuardar;
        #endregion

        #region Properties
        public int Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }
        public string IP
        {
            get => ip;
            set => SetProperty(ref ip, value);
        }

        public bool Idimei
        {
            get => idimei;
            set => SetProperty(ref idimei, value);
        }

        public string Sucursal
        {
            get => sucursal;
            set => SetProperty(ref sucursal, value);
        }
        public string Message
        {
            get => message;
            set => SetProperty(ref message, value);
        }
        public bool TestGuardar
        {
            get => testGuardar;
            set => SetProperty(ref testGuardar, value);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }
        public Command SelectCommand { get; set; }
        #endregion

        #region Methods
       
        private bool ValidateSave()
        {
            return !String.IsNullOrWhiteSpace(ip);
        }

        private async void OnCancel()
        {        
            await Shell.Current.GoToAsync("//AboutPage");
        }

        private async void OnSave()
        {
            var parameter = new Parameter
            {
                Id= Id,
                IP = IP,
                IDIMEI = Idimei
            };
            Util.EstatusImei = parameter.IDIMEI;
            if ( Util.TestGuardar == false)
            {
                Message = "Url debe aprobarse en el test.";
                await Shell.Current.GoToAsync("..");
                 return;
            }

            await App.Database.SaveParameterAsync(ref parameter);
            if (parameter.Id> 0)
                Message = "Se guardó satisfactoriamente.";
            else
                Message = "No se pudo guarda.";
            Util.TestGuardar = false;
            OnSelect();
            await Shell.Current.GoToAsync("..");
        }

        public async void OnSelect()
        {
           foreach (var l in  await App.Database.GetParameters())
            {
                Id = l.Id;
                IP = l.IP;
                Idimei = l.IDIMEI;
                Util.EstatusImei = l.IDIMEI;
            }
            await Shell.Current.GoToAsync("..");
        }
        #endregion

    }
}
