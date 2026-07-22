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
    public partial class TransferenciaPage : ContentPage
    {
        public TransferenciaPage()
        {
            InitializeComponent();
        }

        private async void Button_Nueva(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new NuevaTransferenciaPage());
        }
        private async void Button_Consultar(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ConsultarTransferenciaPage());
        }
        private async void Button_Modificar(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ModificarTransferenciaPage());
        }
        private async void Button_Eliminar(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new EliminarTransferenciaPage());
        }
    }
}