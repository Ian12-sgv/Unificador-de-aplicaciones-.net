using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Telephony;
using AndroidX.Core.Content;
using Appinventario.Interfaces;

[assembly: Microsoft.Maui.Controls.Dependency(typeof(Appinventario.Platforms.Android.ReadPhoneStateService))]

namespace Appinventario.Platforms.Android;

public class ReadPhoneStateService : IReadPhoneState
{
    public string GetPhoneIMEI()
    {
        try
        {
            var context = global::Android.App.Application.Context;
            if (context == null)
            {
                return string.Empty;
            }

            if (Build.VERSION.SdkInt <= BuildVersionCodes.Q &&
                ContextCompat.CheckSelfPermission(context, global::Android.Manifest.Permission.ReadPhoneState) == global::Android.Content.PM.Permission.Granted)
            {
                var telephonyManager = (TelephonyManager?)context.GetSystemService(Context.TelephonyService);
                if (!string.IsNullOrWhiteSpace(telephonyManager?.Imei))
                {
                    return telephonyManager.Imei;
                }
            }

            return Settings.Secure.GetString(context.ContentResolver, Settings.Secure.AndroidId) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
