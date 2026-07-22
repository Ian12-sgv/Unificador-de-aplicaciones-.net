using Appinventario.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Appinventario.ViewModels
{
    public class ArticuloViewModel : BaseViewModel
    {
        #region Constructor
        public ArticuloViewModel()
        {
            SelectCommand = new Command(OnSelect);
        }
        #endregion

        #region Atributtes
        private string ip;
        private int id;
        private bool idimei;
        public Command SelectCommand { get; set; }


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
        public bool Estatusimei
        {
            get => idimei;
            set => SetProperty(ref idimei, value);
        }

        #endregion

        #region Methods
        public async void OnSelect()
        {
            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            foreach (var l in await App.Database.GetParameters())
            {
                Id = l.Id;
                IP = l.IP;
                Estatusimei = l.IDIMEI;
                Util.EstatusImei = l.IDIMEI;
            }
        }
        #endregion


    }
}

