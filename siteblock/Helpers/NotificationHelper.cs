using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using Plugin.LocalNotification.iOSOption;

namespace siteblock.Helpers;

public static class NotificationHelper
{
    private static int _notificationId = 1000;
    private const string CHANNEL_ID_DEFAULT = "siteblock_notifications";
    private const string CHANNEL_ID_SUCCESS = "siteblock_success";
    private const string CHANNEL_ID_ERROR = "siteblock_error";
    private const string CHANNEL_ID_BLOCKED = "siteblock_blocked";

    // Notification duplicate prevention (1 second throttle)
    private static readonly Dictionary<string, DateTime> _lastNotificationTimes = new();
    private static readonly object _notificationLock = new();



    /// <summary>
    /// Get platform information for diagnostics
    /// </summary>
    /// <returns>Platform information string</returns>
    public static string GetPlatformInfo()
    {
#if ANDROID
        try
        {
            var sdkInt = global::Android.OS.Build.VERSION.SdkInt;
            var release = global::Android.OS.Build.VERSION.Release;
            return $"Android SDK: {sdkInt}\nAndroid Version: {release}";
        }
        catch (Exception ex)
        {
            return $"Android (Error getting version: {ex.Message})";
        }
#elif IOS
        return "iOS Platform";
#elif WINDOWS
        return "Windows Platform";
#elif MACCATALYST
        return "Mac Catalyst Platform";
#else
        return "Unknown Platform";
#endif
    }

    /// <summary>
    /// Request notification permission for local notifications
    /// </summary>
    /// <returns>True if permission granted</returns>
    public static async Task<bool> RequestNotificationPermissionAsync()
    {
        try
        {
            return await LocalNotificationCenter.Current.RequestNotificationPermission();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Permission request error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Initialize notification channels (Android)
    /// </summary>
    private static void InitializeNotificationChannels()
    {
#if ANDROID
        try
        {
            var context = global::Android.App.Application.Context;
            if (context != null)
            {
                siteblock.Platforms.Android.Helper.NotificationChannelHelper.CreateNotificationChannels(context);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Channel initialization error: {ex.Message}");
        }
#endif
    }



    /// <summary>
    /// Check if notification should be shown (1 second duplicate prevention)
    /// </summary>
    private static bool ShouldShowNotification(string title, string message)
    {
        lock (_notificationLock)
        {
            var notificationKey = $"{title}|{message}";
            var now = DateTime.Now;

            // Check if we've shown this notification in the last 1 second
            if (_lastNotificationTimes.TryGetValue(notificationKey, out var lastTime))
            {
                var timeSinceLastNotification = now - lastTime;
                if (timeSinceLastNotification.TotalSeconds < 1)
                {
                    System.Diagnostics.Debug.WriteLine($"[Notification] Duplicate prevented: '{notificationKey}' (shown {timeSinceLastNotification.TotalMilliseconds:F0}ms ago)");
                    return false;
                }
            }

            // Update last notification time
            _lastNotificationTimes[notificationKey] = now;

            // Clean up old entries (older than 5 seconds)
            var keysToRemove = _lastNotificationTimes
                .Where(kvp => now - kvp.Value > TimeSpan.FromSeconds(5))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _lastNotificationTimes.Remove(key);
            }

            return true;
        }
    }

    /// <summary>
    /// Shows an immediate local notification with deduplication
    /// </summary>
    /// <param name="title">Notification title</param>
    /// <param name="message">Notification message</param>
    /// <param name="channelId">Channel ID for Android</param>
    /// <returns>True if notification was shown successfully</returns>
    public static async Task<bool> ShowLocalNotificationAsync(string title, string message, string channelId = CHANNEL_ID_DEFAULT)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[Notification] Attempting to show: '{title}' - '{message}'");
            
            // Check for duplicates within 1 second
            if (!ShouldShowNotification(title, message))
            {
                return false; // Duplicate notification prevented
            }
            
            // Initialize channels first
            InitializeNotificationChannels();

            // Request permission first
            var granted = await RequestNotificationPermissionAsync();
            if (!granted)
            {
                System.Diagnostics.Debug.WriteLine("❌ Notification permission not granted");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"[Notification] Permission granted, creating notification...");

            var request = new NotificationRequest
            {
                NotificationId = GetNextNotificationId(),
                Title = title,
                Description = message,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = DateTime.Now // Show immediately
                }
            };

            // Add Android-specific options
            request.Android = new AndroidOptions
            {
                ChannelId = channelId
            };

            // Add iOS-specific options  
            request.iOS = new iOSOptions
            {
                HideForegroundAlert = false
            };

            await LocalNotificationCenter.Current.Show(request);
            System.Diagnostics.Debug.WriteLine($"✅ [Notification] Successfully shown: '{title}' - '{message}'");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Local notification error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Gets the next available notification ID
    /// </summary>
    /// <returns>Next notification ID</returns>
    private static int GetNextNotificationId()
    {
        return ++_notificationId;
    }

    /// <summary>
    /// Cancels all pending local notifications
    /// </summary>
    public static void CancelAllLocalNotifications()
    {
        try
        {
            LocalNotificationCenter.Current.CancelAll();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cancel all notifications error: {ex.Message}");
        }
    }



    // Predefined notification types for immediate local notifications
    public static class NotificationTypes
    {
        public static async Task<bool> ShowSuccessAsync(string message)
        {
            return await ShowLocalNotificationAsync("✅ Success", message, CHANNEL_ID_SUCCESS);
        }

        public static async Task<bool> ShowErrorAsync(string message)
        {
            return await ShowLocalNotificationAsync("❌ Error", message, CHANNEL_ID_ERROR);
        }

        public static async Task<bool> ShowWarningAsync(string message)
        {
            return await ShowLocalNotificationAsync("⚠️ Warning", message, CHANNEL_ID_ERROR);
        }

        public static async Task<bool> ShowInfoAsync(string message)
        {
            return await ShowLocalNotificationAsync("ℹ️ Info", message, CHANNEL_ID_DEFAULT);
        }

        public static async Task<bool> ShowReminderAsync(string message)
        {
            return await ShowLocalNotificationAsync("🔔 Reminder", message, CHANNEL_ID_DEFAULT);
        }

        public static async Task<bool> ShowBlockedAsync(string message)
        {
            return await ShowLocalNotificationAsync("🚫 Blocked", message, CHANNEL_ID_BLOCKED);
        }

        public static async Task<bool> ShowBlockInfoAsync(string message)
        {
            return await ShowLocalNotificationAsync("🛡️ Block Info", message, CHANNEL_ID_BLOCKED);
        }

        /// <summary>
        /// Shows gambling-specific blocking notifications with detailed context
        /// </summary>
        public static class GamblingBlocking
        {
            public static async Task<bool> ShowSiteDetectedAsync(string domain)
            {
                return await ShowLocalNotificationAsync(
                    "🎰 Gambling Site Detected",
                    $"Detected attempt to access {domain}",
                    CHANNEL_ID_BLOCKED);
            }

            public static async Task<bool> ShowSiteBlockedAsync(string domain)
            {
                return await ShowLocalNotificationAsync(
                    "🚫 Site Blocked",
                    $"Blocked access to: {domain}",
                    CHANNEL_ID_BLOCKED);
            }

            public static async Task<bool> ShowBrowserClosedAsync(string domain)
            {
                return await ShowLocalNotificationAsync(
                    "🛡️ Protection Active",
                    $"Browser closed to protect you from {domain}",
                    CHANNEL_ID_BLOCKED);
            }

            public static async Task<bool> ShowContentDetectedAsync(string keyword)
            {
                return await ShowLocalNotificationAsync(
                    "⚠️ Gambling Content",
                    $"Detected gambling content: {keyword}",
                    CHANNEL_ID_BLOCKED);
            }

            public static async Task<bool> ShowProtectionActiveAsync()
            {
                return await ShowLocalNotificationAsync(
                    "🛡️ Protection Enabled",
                    "Gambling site protection is now active and monitoring",
                    CHANNEL_ID_SUCCESS);
            }

            public static async Task<bool> ShowMultipleAttemptsAsync(int count, string domain)
            {
                return await ShowLocalNotificationAsync(
                    "🚨 Multiple Attempts",
                    $"Blocked {count} attempts to access {domain}",
                    CHANNEL_ID_BLOCKED);
            }

            public static async Task<bool> ShowAppBlockedAsync(string appName)
            {
                return await ShowLocalNotificationAsync(
                    "🚫 App Blocked",
                    $"Blocked gambling app: {appName}",
                    CHANNEL_ID_BLOCKED);
            }
        }
    }
}
