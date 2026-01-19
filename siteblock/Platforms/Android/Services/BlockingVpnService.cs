using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Java.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace siteblock.Platforms.Android.Services
{
    [Service(Permission = "android.permission.BIND_VPN_SERVICE", Exported = true, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeSystemExempted)]
    [IntentFilter(new[] { "android.net.VpnService" })]
    public class BlockingVpnService : VpnService
    {
        private const string TAG = "BlockingVPN";
        private const string CHANNEL_ID = "BlockingVpnChannel";
        private const string BLOCK_CHANNEL_ID = "BlockingNotifications";
        private const int NOTIFICATION_ID = 1;
        private const int BLOCK_NOTIFICATION_ID = 2;

        public const string ACTION_START = "com.siteblock.START_VPN";
        public const string ACTION_STOP = "com.siteblock.STOP_VPN";

        private ParcelFileDescriptor? _vpnInterface;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _packetProcessingTask;
        private ConnectivityManager? _connectivityManager;
        private Task? _notificationWatcherTask;
        private CancellationTokenSource? _notificationWatcherCts;

        public override void OnCreate()
        {
            base.OnCreate();
            Log("OnCreate called");
            _connectivityManager = (ConnectivityManager?)GetSystemService(ConnectivityService);
            
            // Initialize notification channels
            siteblock.Platforms.Android.Helper.AndroidNotificationHelper.InitializeChannels(this);
            
            ObserveBlockingRules();
        }

        private void ObserveBlockingRules()
        {
            Log("Setting up rules observer");
            var rulesManager = siteblock.Services.BlockingRulesManager.Instance;
            rulesManager.BlockedDomains.CollectionChanged += (s, e) =>
            {
                if (_vpnInterface != null)
                {
                    Log("🔄 Blocking rules updated");
                    Log("🔄 Restarting VPN to clear DNS cache...");
                    RestartVpn();
                }
            };
        }

        private void RestartVpn()
        {
            Task.Run(async () =>
            {
                Log("Stopping VPN for restart...");
                _cancellationTokenSource?.Cancel();
                await Task.Delay(100);
                _vpnInterface?.Close();
                _vpnInterface = null;

                await Task.Delay(100);

                Log("Restarting VPN...");
                StartVpn();
                ShowRulesUpdatedNotification();
            });
        }

        private void ShowRulesUpdatedNotification()
        {
            siteblock.Platforms.Android.Helper.AndroidNotificationHelper.ShowNormalNotification(
                this,
                "Blocking Rules Updated",
                "VPN restarted - new rules active");
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            Log($"OnStartCommand called with action: {intent?.Action}");
            
            if (intent?.Action == ACTION_START)
                StartVpn();
            else if (intent?.Action == ACTION_STOP)
                StopVpn();

            return StartCommandResult.Sticky;
        }

        private void StartVpn()
        {
            if (_vpnInterface != null)
            {
                Log("VPN already running, skipping start");
                return;
            }

            Log("Starting VPN service...");

            try
            {
                var builder = new Builder(this);
                builder.SetSession("Blocking VPN")
                    .AddAddress("10.0.0.2", 24)
                    .AddDnsServer("8.8.8.8")
                    .AddDnsServer("8.8.4.4")
                    .AddRoute("8.8.8.8", 32)
                    .AddRoute("8.8.4.4", 32)
                    .AddRoute("1.1.1.1", 32)
                    .AddRoute("1.0.0.1", 32)
                    .SetBlocking(false)
                    .SetMtu(1500);

                Log("VPN Builder configured");

                // Try to set underlying network for proper routing
                try
                {
                    var activeNetwork = _connectivityManager?.ActiveNetwork;
                    if (activeNetwork != null)
                    {
                        builder.SetUnderlyingNetworks(new[] { activeNetwork });
                        Log("✓ Using underlying network for routing");
                    }
                    else
                    {
                        Log("⚠ No active network found");
                    }
                }
                catch (Exception ex)
                {
                    Log($"⚠ Warning: Could not set underlying network - {ex.Message}");
                }

                Log("Establishing VPN interface...");
                _vpnInterface = builder.Establish();

                if (_vpnInterface == null)
                {
                    Log("❌ ERROR: Failed to establish VPN interface");
                    return;
                }

                Log("✓ VPN interface established - DNS-only blocking mode");

                // Start foreground service with silent notification
                var notification = siteblock.Platforms.Android.Helper.AndroidNotificationHelper.CreateVpnNotification(this);
                var notificationId = siteblock.Platforms.Android.Helper.AndroidNotificationHelper.GetVpnNotificationId();
                
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                {
                    StartForeground(notificationId, notification, global::Android.Content.PM.ForegroundService.TypeSystemExempted);
                }
                else
                {
                    StartForeground(notificationId, notification);
                }
                Log("✓ Started foreground service with silent notification");

                // Update notification to ensure it's visible
                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager?.Notify(notificationId, notification);

                _cancellationTokenSource = new CancellationTokenSource();
                _packetProcessingTask = Task.Run(() => ProcessPackets(_cancellationTokenSource.Token));
                Log("✓ Packet processing task started");

                // Start notification watcher to keep notification visible
                _notificationWatcherCts = new CancellationTokenSource();
                _notificationWatcherTask = Task.Run(() => WatchNotification(_notificationWatcherCts.Token));
                Log("✓ Notification watcher started");
            }
            catch (Exception ex)
            {
                Log($"❌ ERROR: VPN setup failed - {ex.Message}");
                Log($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Watches and re-shows the notification if it gets dismissed
        /// </summary>
        private async Task WatchNotification(CancellationToken cancellationToken)
        {
            Log("📢 Notification watcher started");
            
            while (!cancellationToken.IsCancellationRequested && _vpnInterface != null)
            {
                try
                {
                    // Re-show notification every 2 seconds to ensure it stays visible
                    await Task.Delay(2000, cancellationToken);
                    
                    if (_vpnInterface != null)
                    {
                        var notification = siteblock.Platforms.Android.Helper.AndroidNotificationHelper.CreateVpnNotification(this);
                        var notificationId = siteblock.Platforms.Android.Helper.AndroidNotificationHelper.GetVpnNotificationId();
                        var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                        notificationManager?.Notify(notificationId, notification);
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"⚠️ Notification watcher error: {ex.Message}");
                }
            }
            
            Log("📢 Notification watcher stopped");
        }

        private void StopVpn()
        {
            Log("Stopping VPN...");
            _cancellationTokenSource?.Cancel();
            _notificationWatcherCts?.Cancel();
            _vpnInterface?.Close();
            _vpnInterface = null;
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
            Log("VPN stopped");
        }

        private async Task ProcessPackets(CancellationToken cancellationToken)
        {
            if (_vpnInterface == null)
            {
                Log("❌ Cannot process packets - VPN interface is null");
                return;
            }

            Log("📦 Starting packet processing...");

            FileInputStream? inputStream = null;
            FileOutputStream? outputStream = null;

            try
            {
                inputStream = new FileInputStream(_vpnInterface.FileDescriptor);
                outputStream = new FileOutputStream(_vpnInterface.FileDescriptor);
                var packet = new byte[32767];

                Log("✓ Packet processing started - waiting for packets...");

                int packetCount = 0;
                while (!cancellationToken.IsCancellationRequested && _vpnInterface != null)
                {
                    var length = await Task.Run(() => inputStream.Read(packet), cancellationToken);
                    if (length > 0)
                    {
                        packetCount++;
                        if (packetCount % 100 == 0)
                        {
                            Log($"📦 Processed {packetCount} packets");
                        }
                        await HandlePacket(packet, length, outputStream, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Log($"❌ ERROR: Packet processing failed - {ex.Message}");
                    Log($"Stack trace: {ex.StackTrace}");
                }
            }
            finally
            {
                inputStream?.Close();
                outputStream?.Close();
                Log("📦 Packet processing stopped");
            }
        }

        private async Task HandlePacket(byte[] packet, int length, FileOutputStream vpnOutput, CancellationToken cancellationToken)
        {
            try
            {
                if (length < 20) return;

                var version = (packet[0] >> 4) & 0xF;
                if (version != 4) return;

                var protocol = packet[9] & 0xFF;
                var ipHeaderLength = (packet[0] & 0x0F) * 4;

                // Check destination IP
                var destIp = $"{packet[16] & 0xFF}.{packet[17] & 0xFF}.{packet[18] & 0xFF}.{packet[19] & 0xFF}";

                // Drop packets to 0.0.0.0 (blocked domains)
                if (destIp == "0.0.0.0") return;

                if (siteblock.Services.BlockingRulesManager.Instance.IsBlocked(destIp))
                {
                    Log($"🚫 BLOCKED IP: {destIp}");
                    return;
                }

                // Handle DNS queries (UDP port 53)
                if (protocol == 17 && length >= ipHeaderLength + 8)
                {
                    var destPort = ((packet[ipHeaderLength + 2] & 0xFF) << 8) | (packet[ipHeaderLength + 3] & 0xFF);

                    if (destPort == 53)
                    {
                        var domain = ParseDnsDomain(packet, ipHeaderLength + 8, length);

                        if (domain != null)
                        {
                            if (siteblock.Services.BlockingRulesManager.Instance.IsDomainBlocked(domain))
                            {
                                Log($"🚫 BLOCKED: {domain} → 0.0.0.0");
                                
                                // Show heads-up notification for blocked site
                                siteblock.Platforms.Android.Helper.AndroidNotificationHelper.ShowBlockedSiteNotification(this, domain);
                                
                                var response = CreateBlockedDnsResponse(packet, length, ipHeaderLength);
                                await Task.Run(() => vpnOutput.Write(response), cancellationToken);
                                return;
                            }
                            else
                            {
                                Log($"✓ DNS Query: {domain}");
                                await ForwardDnsQuery(packet, length, ipHeaderLength, vpnOutput, domain, cancellationToken);
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error handling packet: {ex.Message}");
            }
        }

        private async Task ForwardDnsQuery(byte[] packet, int length, int ipHeaderLength, FileOutputStream vpnOutput, string domain, CancellationToken cancellationToken)
        {
            using var socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, ProtocolType.Udp);

            if (!Protect(socket.Handle.ToInt32()))
            {
                Log($"❌ ERROR: Failed to protect socket for {domain}");
                return;
            }

            try
            {
                var udpHeaderLength = 8;
                var dnsOffset = ipHeaderLength + udpHeaderLength;
                var dnsLength = length - dnsOffset;
                var dnsQuery = new byte[dnsLength];
                Array.Copy(packet, dnsOffset, dnsQuery, 0, dnsLength);

                var dnsServers = new[] { "8.8.8.8", "8.8.4.4", "1.1.1.1" };
                var success = false;

                foreach (var dnsServer in dnsServers)
                {
                    try
                    {
                        var dnsEndpoint = new IPEndPoint(IPAddress.Parse(dnsServer), 53);
                        socket.SendTimeout = 3000;
                        socket.ReceiveTimeout = 3000;

                        await socket.SendToAsync(new ArraySegment<byte>(dnsQuery), SocketFlags.None, dnsEndpoint);

                        var responseBuffer = new byte[512];
                        var received = await socket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), SocketFlags.None);

                        Log($"✓ DNS OK: {domain}");

                        var response = CreateDnsResponsePacket(packet, ipHeaderLength, responseBuffer, received);
                        await Task.Run(() => vpnOutput.Write(response), cancellationToken);
                        success = true;
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (!success)
                {
                    Log($"⚠ ERROR: DNS timeout for {domain}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ ERROR: DNS exception for {domain} - {ex.Message}");
            }
        }

        private byte[] CreateDnsResponsePacket(byte[] originalPacket, int ipHeaderLength, byte[] dnsResponse, int dnsResponseLength)
        {
            var udpHeaderLength = 8;
            var responseSize = ipHeaderLength + udpHeaderLength + dnsResponseLength;
            var response = new byte[responseSize];

            Array.Copy(originalPacket, 0, response, 0, ipHeaderLength);

            // Swap IP addresses
            for (int i = 0; i < 4; i++)
            {
                var temp = response[12 + i];
                response[12 + i] = response[16 + i];
                response[16 + i] = temp;
            }

            Array.Copy(originalPacket, ipHeaderLength, response, ipHeaderLength, udpHeaderLength);

            // Swap UDP ports
            for (int i = 0; i < 2; i++)
            {
                var temp = response[ipHeaderLength + i];
                response[ipHeaderLength + i] = response[ipHeaderLength + 2 + i];
                response[ipHeaderLength + 2 + i] = temp;
            }

            Array.Copy(dnsResponse, 0, response, ipHeaderLength + udpHeaderLength, dnsResponseLength);

            // Update IP total length
            response[2] = (byte)(responseSize >> 8);
            response[3] = (byte)responseSize;

            // Update UDP length
            var udpLength = udpHeaderLength + dnsResponseLength;
            response[ipHeaderLength + 4] = (byte)(udpLength >> 8);
            response[ipHeaderLength + 5] = (byte)udpLength;

            // Clear and recalculate IP checksum
            response[10] = 0;
            response[11] = 0;
            var ipChecksum = CalculateChecksum(response, 0, ipHeaderLength);
            response[10] = (byte)(ipChecksum >> 8);
            response[11] = (byte)ipChecksum;

            // Clear UDP checksum
            response[ipHeaderLength + 6] = 0;
            response[ipHeaderLength + 7] = 0;

            return response;
        }

        private string? ParseDnsDomain(byte[] packet, int dnsOffset, int length)
        {
            try
            {
                if (length < dnsOffset + 12) return null;

                var pos = dnsOffset + 12;
                var domain = new StringBuilder();

                while (pos < length)
                {
                    var labelLength = packet[pos] & 0xFF;
                    if (labelLength == 0) break;
                    if (labelLength >= 192) break;
                    if (pos + labelLength >= length) break;

                    pos++;
                    if (domain.Length > 0) domain.Append('.');

                    for (int i = 0; i < labelLength; i++)
                    {
                        if (pos >= length) break;
                        domain.Append((char)packet[pos++]);
                    }
                }

                return domain.Length > 0 ? domain.ToString() : null;
            }
            catch
            {
                return null;
            }
        }

        private byte[] CreateBlockedDnsResponse(byte[] query, int queryLength, int ipHeaderLength)
        {
            var udpHeaderLength = 8;
            var answerLength = 16;
            var responseSize = queryLength + answerLength;
            var response = new byte[responseSize];

            Array.Copy(query, 0, response, 0, queryLength);

            // Swap IP addresses
            for (int i = 0; i < 4; i++)
            {
                var temp = response[12 + i];
                response[12 + i] = response[16 + i];
                response[16 + i] = temp;
            }

            // Swap UDP ports
            for (int i = 0; i < 2; i++)
            {
                var temp = response[ipHeaderLength + i];
                response[ipHeaderLength + i] = response[ipHeaderLength + 2 + i];
                response[ipHeaderLength + 2 + i] = temp;
            }

            var dnsOffset = ipHeaderLength + udpHeaderLength;

            // Set DNS response flags
            response[dnsOffset + 2] = 0x81;
            response[dnsOffset + 3] = 0x80;

            // Set answer count to 1
            response[dnsOffset + 6] = 0x00;
            response[dnsOffset + 7] = 0x01;

            // Add answer
            var pos = queryLength;
            response[pos++] = 0xC0;
            response[pos++] = 0x0C;
            response[pos++] = 0x00;
            response[pos++] = 0x01;
            response[pos++] = 0x00;
            response[pos++] = 0x01;
            response[pos++] = 0x00;
            response[pos++] = 0x00;
            response[pos++] = 0x00;
            response[pos++] = 0x00; // no caching
            response[pos++] = 0x00;
            response[pos++] = 0x04;
            response[pos++] = 0x00;
            response[pos++] = 0x00;
            response[pos++] = 0x00;
            response[pos++] = 0x00;

            // Update IP total length
            response[2] = (byte)(responseSize >> 8);
            response[3] = (byte)responseSize;

            // Update UDP length
            var udpLength = responseSize - ipHeaderLength;
            response[ipHeaderLength + 4] = (byte)(udpLength >> 8);
            response[ipHeaderLength + 5] = (byte)udpLength;

            // Clear and recalculate IP checksum
            response[10] = 0;
            response[11] = 0;
            var ipChecksum = CalculateChecksum(response, 0, ipHeaderLength);
            response[10] = (byte)(ipChecksum >> 8);
            response[11] = (byte)ipChecksum;

            // Clear UDP checksum
            response[ipHeaderLength + 6] = 0;
            response[ipHeaderLength + 7] = 0;

            return response;
        }

        private int CalculateChecksum(byte[] data, int offset, int length)
        {
            long sum = 0;
            int i = offset;
            while (i < offset + length - 1)
            {
                sum += ((data[i] & 0xFF) << 8) | (data[i + 1] & 0xFF);
                i += 2;
            }
            if (i < offset + length)
            {
                sum += (data[i] & 0xFF) << 8;
            }
            while ((sum >> 16) != 0)
            {
                sum = (sum & 0xFFFF) + (sum >> 16);
            }
            return (int)(~sum & 0xFFFF);
        }

        private void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[{TAG}] {message}");
        }

        public override void OnDestroy()
        {
            Log("OnDestroy called");
            StopVpn();
            base.OnDestroy();
        }
    }
}
