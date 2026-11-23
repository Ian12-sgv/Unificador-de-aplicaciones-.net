using Appinventario.Controls;
using Microsoft.Maui.Handlers;

namespace Appinventario.Platforms.Android.Handlers;

public class RoundedEntry2Handler : EntryHandler
{
    protected override void ConnectHandler(AndroidX.AppCompat.Widget.AppCompatEditText platformView)
    {
        base.ConnectHandler(platformView);

        platformView.Background = EntryBackground.Create(platformView.Context, "OutlineStrong", 1, 8);
        platformView.SetPadding(50, platformView.PaddingTop, platformView.PaddingRight, platformView.PaddingBottom);
    }
}
