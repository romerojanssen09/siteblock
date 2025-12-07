# Site Blocking VPN - .NET MAUI

A cross-platform VPN-based site blocking application built with .NET MAUI and C#. This is a C# port of the Kotlin Android VPN blocking app.

## Features

- **DNS-only VPN blocking** - Intercepts DNS queries without routing all traffic
- **System-wide blocking** - Works across all apps on the device
- **Real-time rule updates** - Automatically restarts VPN when rules change
- **Domain and IP blocking** - Block by domain name or IP address
- **Live statistics** - See blocked and allowed request counts
- **Activity logs** - View detailed VPN activity logs

## Architecture

### Services

- **BlockingRulesManager** - Manages blocked domains and IPs with observable collections
- **LogManager** - Centralized logging with UI updates
- **BlockingVpnService** (Android) - VPN service that intercepts DNS packets

### How It Works

1. **VPN Setup**: Creates a VPN interface that only routes DNS servers (8.8.8.8, 8.8.4.4, etc.)
2. **DNS Interception**: Captures all DNS queries going through the VPN
3. **Domain Checking**: Checks if the queried domain is in the block list
4. **Blocking**: Returns 0.0.0.0 for blocked domains, causing connection failures
5. **Forwarding**: Forwards allowed DNS queries to real DNS servers
6. **Direct Routing**: TCP/UDP traffic goes directly to the internet (not through VPN)

### Auto-Restart on Rule Changes

When you add or remove a blocking rule while the VPN is active:
- The VPN automatically restarts (~100ms)
- DNS cache is cleared
- New rules apply immediately
- User gets a notification

## Project Structure

```
siteblock/
├── Services/
│   ├── BlockingRulesManager.cs    # Rule management
│   └── LogManager.cs               # Logging service
├── Platforms/
│   └── Android/
│       ├── Services/
│       │   └── BlockingVpnService.cs  # VPN implementation
│       ├── MainActivity.cs
│       └── AndroidManifest.xml
├── MainPage.xaml                   # Main UI
├── MainPage.xaml.cs                # Main UI logic
├── LogsPage.xaml                   # Logs UI
└── LogsPage.xaml.cs                # Logs UI logic
```

## Permissions Required

- `BIND_VPN_SERVICE` - Required for VPN functionality
- `FOREGROUND_SERVICE` - Required for persistent VPN service
- `POST_NOTIFICATIONS` - For block notifications
- `INTERNET` - Network access
- `ACCESS_NETWORK_STATE` - Network state monitoring

## Usage

1. **Start VPN**: Tap "Start VPN" button (grants VPN permission on first use)
2. **Add Rules**: Enter domain (e.g., `facebook.com`) or IP and tap "Add Rule"
3. **View Logs**: Tap "View Logs" to see DNS queries and blocking activity
4. **Stop VPN**: Tap "Stop VPN" to disable blocking

## Differences from Kotlin Version

- Uses C# async/await instead of Kotlin coroutines
- ObservableCollection instead of StateFlow
- MAUI cross-platform UI instead of Jetpack Compose
- ActivityResultLauncher for VPN permission
- Same DNS-only blocking logic and packet handling

## Building

1. Open `siteblock.sln` in Visual Studio 2022
2. Select Android target
3. Build and deploy to device or emulator

## Testing

1. Start the VPN
2. Add `facebook.com` to block list
3. Try accessing Facebook in a browser
4. Should see "Site Blocked" notification
5. Check logs to see DNS queries

## Notes

- **Android Only**: VPN functionality only works on Android
- **DNS Cache**: VPN auto-restarts when rules change to clear DNS cache
- **No Root Required**: Uses Android VPN API (no root needed)
- **Battery Efficient**: Only processes DNS packets, not all traffic
