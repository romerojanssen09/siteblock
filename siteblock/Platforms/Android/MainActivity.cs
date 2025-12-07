using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using siteblock.Platforms.Android.Services;

namespace siteblock
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int VPN_REQUEST_CODE = 100;
        public static MainActivity? Instance { get; private set; }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Instance = this;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == VPN_REQUEST_CODE)
            {
                if (resultCode == Result.Ok)
                {
                    // VPN permission granted, start the service
                    var intent = new Intent(this, typeof(BlockingVpnService));
                    intent.SetAction(BlockingVpnService.ACTION_START);
                    StartService(intent);
                }
            }
        }

        protected override void OnDestroy()
        {
            Instance = null;
            base.OnDestroy();
        }
    }
}
