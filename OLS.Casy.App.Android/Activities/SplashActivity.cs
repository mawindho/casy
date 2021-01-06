using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

namespace OLS.Casy.App.Droid.Activities
{
    [Activity(
        Label = "CASY App",
        Icon = "@drawable/ic_launcher",
        Theme = "@style/Theme.Splash",
        NoHistory = true,
        MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class SplashActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            InvokeMainActivity();
        }

        private void InvokeMainActivity()
        {
            StartActivity(new Intent(this, typeof(MainActivity)));
        }
    }
}