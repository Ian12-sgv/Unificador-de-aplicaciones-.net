using Appinventario.Models;
using Appinventario.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Appinventario.ViewModels
{
    public class TomaFisicaViewModel : BaseViewModel
    {
        #region Constructor
        public TomaFisicaViewModel()
        {
            SelectCommand = new Command(OnSelect);
            Title = "Toma Física";
        }
        #endregion

        #region Atributtes
            private int? numeroPagina;
            private string observacion;
            private string referencia;
            private int cantidad;
            private string codigoBarra;
            private string prenda;
            private string ip;
            private int id;
            private bool porLote;
            private bool idimei;

        public Command SelectCommand { get; set; }


        #endregion

        #region Properties
        public int? NumeroPagina
        {
            get => numeroPagina;
            set
            {
                SetProperty(ref numeroPagina, value);
            }
        }
       
        public string Observacion
        {
            get => observacion;
            set
            {
                SetProperty(ref observacion, value);
            }
        }
        public string Referencia
        {
            get => referencia;
            set
            {
                SetProperty(ref referencia, value);
            }
        }
        public int Cantidad
        {
            get => cantidad;
            set
            {
                SetProperty(ref cantidad, value);
            }
        }
        public string CodigoBarra
        {
            get => codigoBarra;
            set
            {
                SetProperty(ref codigoBarra, value);
            }
        }
        public string Prenda
        {
            get => prenda;
            set
            {
                SetProperty(ref prenda, value);
            }
        }
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
        public Boolean PorLote
        {
            get => porLote;
            set => SetProperty(ref porLote, value);
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
            foreach (var l in await App.Database.GetParameters())
            {              
                IP = l.IP;
                Estatusimei = l.IDIMEI;
                Util.EstatusImei = l.IDIMEI;
            } 
        }
        

        #endregion
    }
}