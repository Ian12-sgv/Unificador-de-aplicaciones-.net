using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Telephony;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Appinventario.Droid;
using Appinventario.Interfaces;
using Android.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[assembly: Xamarin.Forms.Dependency(typeof(ReadPhoneStateService))]
namespace Appinventario.Droid
{
     class ReadPhoneStateService : IReadPhoneState
    {
        Android.Telephony.TelephonyManager mTelephonyMgr;

        public string GetPhoneIMEI()
        {
            var valor = "";
            if (Build.VERSION.SdkInt <= BuildVersionCodes.Q) //ANDROID10
            {
                mTelephonyMgr = (Android.Telephony.TelephonyManager)Application.Context.GetSystemService(Context.TelephonyService);
                valor = mTelephonyMgr.Imei;
            }
            else
                valor = Settings.Secure.GetString(Android.App.Application.Context.ContentResolver, Settings.Secure.AndroidId);

            //if (valor == "")
            //    valor = Settings.Secure.GetString(Android.App.Application.Context.ContentResolver, Settings.Secure.AndroidId);
           
            return valor;
        }
    
    }
}