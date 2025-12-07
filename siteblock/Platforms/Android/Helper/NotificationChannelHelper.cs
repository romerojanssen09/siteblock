using Android.App;
using Android.Content;

namespace siteblock.Platforms.Android.Helper
{
    public static class NotificationChannelHelper
    {
        private const string CHANNEL_ID_DEFAULT = "siteblock_notifications";
        private const string CHANNEL_ID_SUCCESS = "siteblock_success";
        private const string CHANNEL_ID_ERROR = "siteblock_error";
        private const string CHANNEL_ID_BLOCKED = "siteblock_blocked";

        public static void CreateNotificationChannels(Context context)
        {
            var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
            if (notificationManager == null) return;

            // Default channel
            var defaultChannel = new NotificationChannel(
                CHANNEL_ID_DEFAULT,
                "General Notifications",
                NotificationImportance.Default)
            {
                Description = "General app notifications"
            };
            notificationManager.CreateNotificationChannel(defaultChannel);

            // Success channel
            var successChannel = new NotificationChannel(
                CHANNEL_ID_SUCCESS,
                "Success Notifications",
                NotificationImportance.Low)
            {
                Description = "Success and confirmation notifications"
            };
            notificationManager.CreateNotificationChannel(successChannel);

            // Error channel
            var errorChannel = new NotificationChannel(
                CHANNEL_ID_ERROR,
                "Error Notifications",
                NotificationImportance.High)
            {
                Description = "Error and warning notifications"
            };
            notificationManager.CreateNotificationChannel(errorChannel);

            // Blocked sites channel
            var blockedChannel = new NotificationChannel(
                CHANNEL_ID_BLOCKED,
                "Blocked Sites",
                NotificationImportance.Default)
            {
                Description = "Notifications when sites are blocked"
            };
            notificationManager.CreateNotificationChannel(blockedChannel);
        }
    }
}
