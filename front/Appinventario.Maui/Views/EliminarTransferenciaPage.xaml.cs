using Appinventario.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Appinventario.Models;


namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EliminarTransferenciaPage : ContentPage
    {
        public EliminarTransferenciaPage()
        {
            InitializeComponent();
            List<Transferencia> listaTranfer = new List<Transferencia>
            {
                new Transferencia { Numero = 124001, Observacion = "Para Liliana" },
                new Transferencia { Numero = 124002, Observacion = "Corrección" },
                new Transferencia { Numero = 124003, Observacion = "Nuevos articulos" }
             };
            listaTranferencia.ItemsSource = listaTranfer;
        }

        private async void EliminarTransfer_Clicked(object sender, EventArgs e)
        {
            bool respuesta = await DisplayAlert("Eliminar Transferencia","Desea eliminar la Transferencia?", "Eliminar","Cancelar");
        }


    }
}
