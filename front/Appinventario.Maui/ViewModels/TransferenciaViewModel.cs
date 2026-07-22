using Appinventario.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Controls;

namespace Appinventario.ViewModels
{
    internal class TransferenciaViewModel : BaseViewModel
    {
        #region Constructor
        public TransferenciaViewModel()
        {
            SelectCommand = new Command(OnSelect);
        }
        #endregion

        #region Atributtes
        private int id;
        private string ip;
        private bool idimei;
        public Command SelectCommand { get; set; }
        #endregion

        #region Properties

        public int Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }
        #endregion
        public string IP
        {
            get => ip;
            set => SetProperty(ref ip, value);
        }
        public bool Estatusimei
        {
            get => idimei;
            set => SetProperty(ref idimei, value);
        }

        #region Methods
        public async void OnSelect()
        {
            foreach (var l in await App.Database.GetParameters())
            {
                Id = l.Id;
                IP = l.IP;
                Estatusimei = l.IDIMEI;
                Util.EstatusImei = l.IDIMEI;
            }

            await Shell.Current.GoToAsync("..");
        }
        #endregion


    }
}

