using Appinventario.Models;
using Appinventario.Services;
using Appinventario.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Appinventario.Interfaces;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;


namespace Appinventario.ViewModel
{
    internal class MovTransferenciasViewModel : BaseViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public MovTransferencias MovTransferencias { get; set; }
        public ICommand GuardarCommand { get; set; }
        public MovTransferenciasViewModel()
        {
            MovTransferencias = new MovTransferencias
            {
                Fecha = DateTime.Now
            };

            GuardarCommand = new Command(GuardarMovTransferencias);
        }
        private async void GuardarMovTransferencias()
        {
            // Aquí va la lógica para guardar la transferencia en la base de datos
            // Por ejemplo, hacer una llamada a un servicio API para guardar en la base de datos.
            // var resultado = await servicioApi.GuardarTransferencia(Transferencia);

            // Simulación de guardado exitoso
            await Application.Current.MainPage.DisplayAlert("Éxito", "Transferencia guardada correctamente", "OK");
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

