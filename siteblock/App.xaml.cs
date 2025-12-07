using siteblock.Storage;
using siteblock.Services;

namespace siteblock
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
            // Initialize notification permissions
            Task.Run(async () =>
            {
                await Helpers.NotificationHelper.RequestNotificationPermissionAsync();
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}