using Android.Content;
using Android.Graphics.Drawables;
using Appinventario.Services;
using Microsoft.Maui.Platform;

namespace Appinventario.Platforms.Android.Handlers;

/// <summary>
/// Fondo común de los campos de texto con borde: superficie blanca y trazo de la paleta.
/// </summary>
internal static class EntryBackground
{
    public static GradientDrawable Create(Context context, string strokeColorKey, double strokeDp, double cornerDp)
    {
        var drawable = new GradientDrawable();
        drawable.SetCornerRadius((float)context.ToPixels(cornerDp));
        drawable.SetStroke((int)context.ToPixels(strokeDp), AppColors.Get(strokeColorKey).ToPlatform());
        drawable.SetColor(AppColors.Get("Surface").ToPlatform());
        return drawable;
    }
}
