using System.Threading.Tasks;

namespace Appinventario.Services
{
    /// <summary>
    /// Avisos breves que no detienen el escaneo: éxito y validaciones de formulario.
    /// Los errores que el operador debe leer y las decisiones siguen usando DisplayAlert.
    /// </summary>
    public static class FeedbackService
    {
        public static Task ShowAsync(string message)
        {
#if ANDROID
            var activity = Platform.CurrentActivity;
            var root = activity?.FindViewById(global::Android.Resource.Id.Content);
            if (root != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    Google.Android.Material.Snackbar.Snackbar
                        .Make(root, message, Google.Android.Material.Snackbar.Snackbar.LengthLong)
                        .Show());
                return Task.CompletedTask;
            }
#endif
            var page = Application.Current?.MainPage;
            return page != null ? page.DisplayAlert(string.Empty, message, "Aceptar") : Task.CompletedTask;
        }
    }
}
