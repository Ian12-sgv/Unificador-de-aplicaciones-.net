using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Appinventario.Controls;
using Appinventario.Droid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(SoftkeyboardDisabledEntry), typeof(SoftkeyboardDisabledEntryRenderer))]

namespace Appinventario.Droid
{
    public class SoftkeyboardDisabledEntryRenderer : EntryRenderer
    {

        protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                ((SoftkeyboardDisabledEntry)e.NewElement).PropertyChanging += OnPropertyChanging;
            }

            if (e.OldElement != null)
            {
                ((SoftkeyboardDisabledEntry)e.OldElement).PropertyChanging -= OnPropertyChanging;
            }

            if (e.OldElement == null)
            {
                //Control.SetBackgroundResource(Resource.Layout.rounded_shape);
                var gradientDrawable = new GradientDrawable();
                //  gradientDrawable.SetCornerRadius(60f);
                 gradientDrawable.SetStroke(2, Android.Graphics.Color.Orange);
                gradientDrawable.SetColor(Android.Graphics.Color.LightGray);
                Control.SetBackground(gradientDrawable);
                Control.SetPadding(50, Control.PaddingTop, Control.PaddingRight, Control.PaddingBottom);
            }
            // Disable the Keyboard on Focus
            this.Control.ShowSoftInputOnFocus = false;
        }

        private void OnPropertyChanging(object sender, PropertyChangingEventArgs propertyChangingEventArgs)
        {
            try
            {
                if (propertyChangingEventArgs.PropertyName == VisualElement.IsFocusedProperty.PropertyName)
                {
                    // incase if the focus was moved from another Entry
                    // Forcefully dismiss the Keyboard 
                    InputMethodManager imm = (InputMethodManager)this.Context.GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow(this.Control.WindowToken, 0);
                }
            }
            catch(Exception fail)
            {
                return;
            }
            // Check if the view is about to get Focus
          
        }


    }
}