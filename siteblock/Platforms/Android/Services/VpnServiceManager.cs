using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;

namespace siteblock.Platforms.Android.Services
{
    /// <summary>
    /// Helper class to manage VPN service lifecycle and ensure it stays running
    /// </summary>
    public static class VpnServiceManager
    {
        private const string TAG = "VpnServiceManager";

        /// <summary>
        /// Check if VPN permission is granted
        /// </summary>
        public static bool IsVpnPermissionGranted(Context context)
        {
            try
            {
                var prepareIntent = VpnService.Prepare(context);
                return prepareIntent == null;
            }
            catch (Exception ex)
            {
                Log($"Error checking VPN permission: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if VPN service is currently running
        /// </summary>
        public static bool IsVpnServiceRunning(Context context)
        {
            try
            {
                var activityManager = context.GetSystemService(Context.ActivityService) as ActivityManager;
                if (activityManager == null) return false;

                var runningServices = activityManager.GetRunningServices(int.MaxValue);
                return runningServices?.Any(service =>
                    service.Service.ClassName?.Contains("BlockingVpnService") == true) ?? false;
            }
            catch (Exception ex)
            {
                Log($"Error checking VPN service status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Start VPN service with proper foreground service handling
        /// </summary>
        public static bool StartVpnService(Context context)
        {
            try
            {
                // Check permission first
                if (!IsVpnPermissionGranted(context))
                {
                    Log("VPN permission not granted");
                    return false;
                }

                var intent = new Intent(context, typeof(BlockingVpnService));
                intent.SetAction(BlockingVpnService.ACTION_START);

                // Use StartForegroundService for Android 8.0+
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    context.StartForegroundService(intent);
                    Log("VPN service started as foreground service");
                }
                else
                {
                    context.StartService(intent);
                    Log("VPN service started");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log($"Error starting VPN service: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop VPN service
        /// </summary>
        public static void StopVpnService(Context context)
        {
            try
            {
                var intent = new Intent(context, typeof(BlockingVpnService));
                intent.SetAction(BlockingVpnService.ACTION_STOP);
                context.StartService(intent);
                Log("VPN service stop requested");
            }
            catch (Exception ex)
            {
                Log($"Error stopping VPN service: {ex.Message}");
            }
        }

        /// <summary>
        /// Request VPN permission from user
        /// </summary>
        public static Intent? GetVpnPermissionIntent(Context context)
        {
            try
            {
                return VpnService.Prepare(context);
            }
            catch (Exception ex)
            {
                Log($"Error getting VPN permission intent: {ex.Message}");
                return null;
            }
        }

        private static void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[{TAG}] {message}");
        }
    }
}
