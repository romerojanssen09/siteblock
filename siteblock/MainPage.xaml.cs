using siteblock.Services;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

#if ANDROID
using Android.Content;
using Android.Net;
using siteblock.Platforms.Android.Services;
#endif

namespace siteblock
{
    public partial class MainPage : ContentPage
    {
        private bool _isVpnActive = false;
        private readonly ObservableCollection<string> _allRules = new();

        public MainPage()
        {
            InitializeComponent();
            LoadBlockedRules();
            UpdateStats();

            // Subscribe to stats updates
            var rulesManager = BlockingRulesManager.Instance;
            rulesManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(BlockingRulesManager.BlockedCount) ||
                    e.PropertyName == nameof(BlockingRulesManager.AllowedCount))
                {
                    MainThread.BeginInvokeOnMainThread(UpdateStats);
                }
            };

            // Subscribe to collection changes
            rulesManager.BlockedDomains.CollectionChanged += (s, e) => LoadBlockedRules();
            rulesManager.BlockedIps.CollectionChanged += (s, e) => LoadBlockedRules();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Check VPN status when page appears
            CheckAndUpdateVpnStatus();
        }

        private void CheckAndUpdateVpnStatus()
        {
#if ANDROID
            try
            {
                // Check if VPN is active by checking if VpnService.Prepare returns null
                var prepareIntent = VpnService.Prepare(Platform.CurrentActivity);
                var isVpnPermissionGranted = prepareIntent == null;
                
                // Check if our service is actually running
                var activityManager = Platform.CurrentActivity?.GetSystemService(Context.ActivityService) as Android.App.ActivityManager;
                var isServiceRunning = false;
                
                if (activityManager != null)
                {
                    var runningServices = activityManager.GetRunningServices(int.MaxValue);
                    isServiceRunning = runningServices?.Any(service => 
                        service.Service.ClassName?.Contains("BlockingVpnService") == true) ?? false;
                }
                
                System.Diagnostics.Debug.WriteLine($"[MainPage] VPN Status Check - Permission granted: {isVpnPermissionGranted}, Service running: {isServiceRunning}");
                
                // Update UI based on actual status
                if (isServiceRunning)
                {
                    _isVpnActive = true;
                    StatusLabel.Text = "Active";
                    VpnButton.Text = "Stop VPN";
                    StatsLabel.IsVisible = true;
                }
                else
                {
                    _isVpnActive = false;
                    StatusLabel.Text = "Inactive";
                    VpnButton.Text = "Start VPN";
                    StatsLabel.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Error checking VPN status: {ex.Message}");
            }
#endif
        }

        private void LoadBlockedRules()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _allRules.Clear();
                var rulesManager = BlockingRulesManager.Instance;

                foreach (var domain in rulesManager.BlockedDomains)
                {
                    _allRules.Add(domain);
                }

                foreach (var ip in rulesManager.BlockedIps)
                {
                    _allRules.Add(ip);
                }

                BlockedList.ItemsSource = _allRules;
                RuleCountLabel.Text = $"{_allRules.Count} rules";
            });
        }

        private void UpdateStats()
        {
            var rulesManager = BlockingRulesManager.Instance;
            StatsLabel.Text = $"Blocked: {rulesManager.BlockedCount} | Allowed: {rulesManager.AllowedCount}";
            StatsLabel.IsVisible = _isVpnActive;
        }

        private async void OnVpnButtonClicked(object sender, EventArgs e)
        {
            if (_isVpnActive)
            {
                StopVpnService();
                _isVpnActive = false;
                StatusLabel.Text = "Inactive";
                VpnButton.Text = "Start VPN";
                StatsLabel.IsVisible = false;
                BlockingRulesManager.Instance.ResetStats();
            }
            else
            {
#if ANDROID
                // Show notification first
                siteblock.Platforms.Android.Helper.AndroidNotificationHelper.ShowNormalNotification(
                    Platform.CurrentActivity!,
                    "Starting VPN",
                    "Requesting VPN permission...");
                
                // Small delay to ensure notification is visible
                await Task.Delay(500);
                
                var prepareIntent = VpnService.Prepare(Platform.CurrentActivity);
                if (prepareIntent != null)
                {
                    // Request VPN permission - result handled in MainActivity.OnActivityResult
                    Platform.CurrentActivity?.StartActivityForResult(prepareIntent, 100);
                    
                    // Wait a bit and check if VPN started
                    await Task.Delay(2000);
                    UpdateVpnStatus();
                }
                else
                {
                    // Permission already granted, start directly
                    StartVpnService();
                    
                    // Show success notification
                    siteblock.Platforms.Android.Helper.AndroidNotificationHelper.ShowNormalNotification(
                        Platform.CurrentActivity!,
                        "VPN Started",
                        "Protection is now active");
                }
#else
                await DisplayAlert("Not Supported", "VPN is only supported on Android", "OK");
#endif
            }
        }

        private void StartVpnService()
        {
#if ANDROID
            var intent = new Intent(Platform.CurrentActivity, typeof(BlockingVpnService));
            intent.SetAction(BlockingVpnService.ACTION_START);
            
            // Use StartForegroundService for Android 8.0+ to ensure proper background operation
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                Platform.CurrentActivity?.StartForegroundService(intent);
            }
            else
            {
                Platform.CurrentActivity?.StartService(intent);
            }

            _isVpnActive = true;
            StatusLabel.Text = "Active";
            VpnButton.Text = "Stop VPN";
            StatsLabel.IsVisible = true;
            
            System.Diagnostics.Debug.WriteLine("[MainPage] VPN service started with foreground service");
#endif
        }

        private void UpdateVpnStatus()
        {
            // Check VPN status after permission request
            CheckAndUpdateVpnStatus();
        }

        private void StopVpnService()
        {
#if ANDROID
            var intent = new Intent(Platform.CurrentActivity, typeof(BlockingVpnService));
            intent.SetAction(BlockingVpnService.ACTION_STOP);
            Platform.CurrentActivity?.StartService(intent);
#endif
        }

        private async void OnAddRuleClicked(object sender, EventArgs e)
        {
            var rule = DomainEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(rule))
            {
                await DisplayAlert("Error", "Please enter a domain or IP address", "OK");
                return;
            }

            var rulesManager = BlockingRulesManager.Instance;

            // Check if it's an IP address
            if (Regex.IsMatch(rule, @"^\d+\.\d+\.\d+\.\d+$"))
            {
                rulesManager.AddBlockedIp(rule);
            }
            else
            {
                await rulesManager.AddBlockedDomainAsync(rule);
            }

            DomainEntry.Text = string.Empty;
            
            // Show cache info
            var count = await rulesManager.GetCachedSitesCountAsync();
            await DisplayAlert("Success", $"Added blocking rule: {rule}\nTotal cached sites: {count}", "OK");
        }

        private async void OnDeleteRuleClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string rule)
            {
                var rulesManager = BlockingRulesManager.Instance;

                if (Regex.IsMatch(rule, @"^\d+\.\d+\.\d+\.\d+$"))
                {
                    rulesManager.RemoveBlockedIp(rule);
                }
                else
                {
                    await rulesManager.RemoveBlockedDomainAsync(rule);
                }
            }
        }


    }
}
