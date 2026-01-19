using Android.App;
using Android.Content;
using Android.OS;

namespace siteblock.Platforms.Android.Helper
{
    /// <summary>
    /// Android-specific notification helper for VPN service
    /// Handles two types of notifications:
    /// 1. Silent VPN service notification (persistent, no sound/vibration)
    /// 2. Heads-up blocked site notifications (pop-up with vibration)
    /// </summary>
    public static class AndroidNotificationHelper
    {
        private const string TAG = "AndroidNotificationHelper";
        
        // Notification channels
        private const string VPN_CHANNEL_ID = "vpn_service_silent";
        private const string BLOCKED_CHANNEL_ID = "blocked_sites_headsup";
        private const string NORMAL_CHANNEL_ID = "normal_notifications";
        
        // Notification IDs
        private const int VPN_NOTIFICATION_ID = 1;
        private const int BLOCKED_NOTIFICATION_BASE_ID = 1000;

        /// <summary>
        /// Initialize all notification channels
        /// Must be called before showing any notifications
        /// </summary>
        public static void InitializeChannels(Context context)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                return; // Channels only needed for Android 8.0+

            var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
            if (notificationManager == null) return;

            // 1. Silent VPN Service Channel - Low importance, no sound, no vibration
            var vpnChannel = new NotificationChannel(
                VPN_CHANNEL_ID,
                "VPN Service",
                NotificationImportance.Low)
            {
                Description = "Shows VPN is running in background",
                LockscreenVisibility = NotificationVisibility.Public
            };
            vpnChannel.SetSound(null, null); // Silent
            vpnChannel.EnableVibration(false); // No vibration
            vpnChannel.EnableLights(false); // No LED
            vpnChannel.SetShowBadge(false); // No badge
            notificationManager.CreateNotificationChannel(vpnChannel);

            // 2. Blocked Sites Channel - High importance for heads-up display
            var blockedChannel = new NotificationChannel(
                BLOCKED_CHANNEL_ID,
                "Blocked Sites",
                NotificationImportance.High)
            {
                Description = "Alerts when sites are blocked"
            };
            blockedChannel.EnableVibration(true); // Enable vibration
            blockedChannel.EnableLights(true); // Enable LED
            blockedChannel.SetShowBadge(true); // Show badge
            notificationManager.CreateNotificationChannel(blockedChannel);

            // 3. Normal Notifications Channel - Default importance
            var normalChannel = new NotificationChannel(
                NORMAL_CHANNEL_ID,
                "General Notifications",
                NotificationImportance.Default)
            {
                Description = "General app notifications"
            };
            notificationManager.CreateNotificationChannel(normalChannel);

            Log("Notification channels initialized");
        }

        /// <summary>
        /// Create silent VPN service notification (persistent, no sound/vibration)
        /// </summary>
        public static Notification CreateVpnNotification(Context context)
        {
            var intent = new Intent(context, typeof(MainActivity));
            var pendingIntent = PendingIntent.GetActivity(
                context, 
                0, 
                intent, 
                PendingIntentFlags.Immutable);

            var builder = new Notification.Builder(context, VPN_CHANNEL_ID)
                .SetContentTitle("VPN Protection Active")
                .SetContentText("Site blocking is running")
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                .SetContentIntent(pendingIntent)
                .SetOngoing(true) // Persistent notification
                .SetAutoCancel(false) // Cannot be dismissed
                .SetShowWhen(false); // Don't show time

            // Set category for proper classification
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                builder.SetCategory(Notification.CategoryService);
            }
            else
            {
                // For older Android, set priority to low
                builder.SetPriority((int)NotificationPriority.Low);
            }

            return builder.Build();
        }

        /// <summary>
        /// Show heads-up notification for blocked site (pop-up with vibration)
        /// </summary>
        public static void ShowBlockedSiteNotification(Context context, string domain)
        {
            try
            {
                var intent = new Intent(context, typeof(MainActivity));
                var pendingIntent = PendingIntent.GetActivity(
                    context,
                    0,
                    intent,
                    PendingIntentFlags.Immutable);

                var builder = new Notification.Builder(context, BLOCKED_CHANNEL_ID)
                    .SetContentTitle("🚫 Site Blocked")
                    .SetContentText($"Blocked: {domain}")
                    .SetSmallIcon(global::Android.Resource.Drawable.IcDialogAlert)
                    .SetContentIntent(pendingIntent)
                    .SetAutoCancel(true) // Dismiss when tapped
                    .SetShowWhen(true)
                    .SetWhen(Java.Lang.JavaSystem.CurrentTimeMillis());

                // For Android 8.0+, use alarm category for heads-up
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    builder.SetCategory(Notification.CategoryAlarm);
                    builder.SetTimeoutAfter(5000); // Auto-dismiss after 5 seconds
                }
                else
                {
                    // For older versions, set high priority
                    builder.SetPriority((int)NotificationPriority.High);
                }

                var notification = builder.Build();
                var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;

                // Use unique ID for each domain to avoid duplicates
                var notificationId = BLOCKED_NOTIFICATION_BASE_ID + Math.Abs(domain.GetHashCode() % 1000);
                notificationManager?.Notify(notificationId, notification);

                Log($"Heads-up notification shown for: {domain}");
            }
            catch (Exception ex)
            {
                Log($"Error showing blocked site notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Show normal notification (default importance)
        /// </summary>
        public static void ShowNormalNotification(Context context, string title, string message)
        {
            try
            {
                var intent = new Intent(context, typeof(MainActivity));
                var pendingIntent = PendingIntent.GetActivity(
                    context,
                    0,
                    intent,
                    PendingIntentFlags.Immutable);

                var builder = new Notification.Builder(context, NORMAL_CHANNEL_ID)
                    .SetContentTitle(title)
                    .SetContentText(message)
                    .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                    .SetContentIntent(pendingIntent)
                    .SetAutoCancel(true)
                    .SetShowWhen(true);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    builder.SetTimeoutAfter(3000);
                }

                var notification = builder.Build();
                var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;

                var notificationId = (int)(DateTime.Now.Ticks % int.MaxValue);
                notificationManager?.Notify(notificationId, notification);

                Log($"Normal notification shown: {title}");
            }
            catch (Exception ex)
            {
                Log($"Error showing normal notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Get VPN notification ID
        /// </summary>
        public static int GetVpnNotificationId() => VPN_NOTIFICATION_ID;

        /// <summary>
        /// Cancel all blocked site notifications
        /// </summary>
        public static void CancelAllBlockedNotifications(Context context)
        {
            try
            {
                var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
                if (notificationManager == null) return;

                // Cancel all blocked site notifications
                for (int i = 0; i < 1000; i++)
                {
                    notificationManager.Cancel(BLOCKED_NOTIFICATION_BASE_ID + i);
                }

                Log("All blocked site notifications cancelled");
            }
            catch (Exception ex)
            {
                Log($"Error cancelling notifications: {ex.Message}");
            }
        }

        private static void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[{TAG}] {message}");
        }
    }
}
