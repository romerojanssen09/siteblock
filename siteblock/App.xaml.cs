using siteblock.Storage;
using siteblock.Services;

namespace siteblock
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
            // Initialize Android notification channels and check permissions
#if ANDROID
            Task.Run(async () =>
            {
                try
                {
                    var context = Android.App.Application.Context;
                    
                    // Initialize notification channels
                    siteblock.Platforms.Android.Helper.AndroidNotificationHelper.InitializeChannels(context);
                    System.Diagnostics.Debug.WriteLine("[App] Android notification channels initialized");
                    
                    // Check and request notification permission
                    await CheckAndRequestNotificationPermission();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[App] Error initializing notifications: {ex.Message}");
                }
            });
#endif
        }

#if ANDROID
        private async Task CheckAndRequestNotificationPermission()
        {
            try
            {
                // For Android 13+ (API 33+), we need to check notification permission
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
                {
                    var context = Android.App.Application.Context;
                    var notificationManager = context.GetSystemService(Android.Content.Context.NotificationService) as Android.App.NotificationManager;
                    
                    if (notificationManager != null)
                    {
                        bool areNotificationsEnabled = notificationManager.AreNotificationsEnabled();
                        System.Diagnostics.Debug.WriteLine($"[App] Notifications enabled: {areNotificationsEnabled}");
                        
                        if (!areNotificationsEnabled)
                        {
                            System.Diagnostics.Debug.WriteLine("[App] Notifications disabled, will request permission when needed");
                            
                            // Show a test notification to trigger permission request
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await Task.Delay(1000); // Wait for app to fully load
                                
                                // Request permission using Plugin.LocalNotification
                                var granted = await Plugin.LocalNotification.LocalNotificationCenter.Current.RequestNotificationPermission();
                                System.Diagnostics.Debug.WriteLine($"[App] Notification permission granted: {granted}");
                                
                                if (granted)
                                {
                                    // Show welcome notification
                                    siteblock.Platforms.Android.Helper.AndroidNotificationHelper.ShowNormalNotification(
                                        context,
                                        "Welcome to SiteBlock",
                                        "Notifications are enabled");
                                }
                            });
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[App] Notifications already enabled");
                        }
                    }
                }
                else
                {
                    // For Android 12 and below, notifications are enabled by default
                    System.Diagnostics.Debug.WriteLine("[App] Android < 13, notifications enabled by default");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error checking notification permission: {ex.Message}");
            }
        }
#endif

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}