using Android.Content;
using Android.Views.InputMethods;
using Appinventario.Controls;
using Microsoft.Maui.Handlers;

namespace Appinventario.Platforms.Android.Handlers;

public class SoftkeyboardDisabledEntryHandler : EntryHandler
{
    protected override void ConnectHandler(AndroidX.AppCompat.Widget.AppCompatEditText platformView)
    {
        base.ConnectHandler(platformView);

        platformView.Background = EntryBackground.Create(platformView.Context, "Primary", 2, 8);
        platformView.SetPadding(50, platformView.PaddingTop, platformView.PaddingRight, platformView.PaddingBottom);
        platformView.ShowSoftInputOnFocus = false;
        platformView.FocusChange += OnFocusChange;
    }

    protected override void DisconnectHandler(AndroidX.AppCompat.Widget.AppCompatEditText platformView)
    {
        platformView.FocusChange -= OnFocusChange;
        base.DisconnectHandler(platformView);
    }

    private void OnFocusChange(object sender, global::Android.Views.View.FocusChangeEventArgs e)
    {
        if (sender is global::Android.Views.View view)
        {
            var imm = view.Context.GetSystemService(Context.InputMethodService) as InputMethodManager;
            imm?.HideSoftInputFromWindow(view.WindowToken, 0);
        }
    }
}
