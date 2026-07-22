using Appinventario.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Appinventario.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ModificarTransferenciaPage : ContentPage
    {
        public ModificarTransferenciaPage()
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

        private async void ModificarTransfer_Clicked(object sender, EventArgs e)
        {
            bool respuesta = await DisplayAlert("Modificar Transferencia", "Desea Modificar la Transferencia?", "Modificar", "Cancelar");
        }
    }
}