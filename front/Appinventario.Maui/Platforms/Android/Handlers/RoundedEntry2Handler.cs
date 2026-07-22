using Android.Graphics.Drawables;
using Appinventario.Controls;
using Microsoft.Maui.Handlers;

namespace Appinventario.Platforms.Android.Handlers;

public class RoundedEntry2Handler : EntryHandler
{
    protected override void ConnectHandler(AndroidX.AppCompat.Widget.AppCompatEditText platformView)
    {
        base.ConnectHandler(platformView);

        var gradientDrawable = new GradientDrawable();
        gradientDrawable.SetStroke(2, global::Android.Graphics.Color.SteelBlue);
        gradientDrawable.SetColor(global::Android.Graphics.Color.LightGray);

        platformView.Background = gradientDrawable;
        platformView.SetPadding(50, platformView.PaddingTop, platformView.PaddingRight, platformView.PaddingBottom);
    }
}
