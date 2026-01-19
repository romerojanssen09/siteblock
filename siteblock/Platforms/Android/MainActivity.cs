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
            
            // Ensure the app can run in background
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                // Request battery optimization exemption for better background performance
                var powerManager = GetSystemService(PowerService) as PowerManager;
                var packageName = PackageName;
                
                if (powerManager != null && packageName != null && !powerManager.IsIgnoringBatteryOptimizations(packageName))
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] App is subject to battery optimization");
                    // Note: You can prompt user to disable battery optimization if needed
                }
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == VPN_REQUEST_CODE)
            {
                if (resultCode == Result.Ok)
                {
                    // Show notification that permission was granted
                    siteblock.Platforms.Android.Helper.AndroidNotificationHelper.ShowNormalNotification(
                        this,
                        "VPN Permission Granted",
                        "Starting VPN service...");
                    
                    // VPN permission granted, start the service
                    var intent = new Intent(this, typeof(BlockingVpnService));
                    intent.SetAction(BlockingVpnService.ACTION_START);
                    
                    // Use StartForegroundService for Android 8.0+
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    {
                        StartForegroundService(intent);
                    }
                    else
                    {
                        StartService(intent);
                    }
                    
                    System.Diagnostics.Debug.WriteLine("[MainActivity] VPN service start requested");
                }
                else
                {
                    // Show notification that permission was denied
                    siteblock.Platforms.Android.Helper.AndroidNotificationHelper.ShowNormalNotification(
                        this,
                        "VPN Permission Denied",
                        "Cannot start VPN without permission");
                    
                    System.Diagnostics.Debug.WriteLine("[MainActivity] VPN permission denied");
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
