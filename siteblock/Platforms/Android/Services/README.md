# Android VPN Services

## BlockingVpnService
Main VPN service that intercepts DNS queries and blocks access to specified domains.

### Features
- DNS-based blocking (intercepts DNS queries)
- Persistent foreground notification
- Real-time rule updates
- Statistics tracking
- Appears in active apps list

### Usage
```csharp
// Start VPN
var intent = new Intent(context, typeof(BlockingVpnService));
intent.SetAction(BlockingVpnService.ACTION_START);
context.StartForegroundService(intent);

// Stop VPN
var intent = new Intent(context, typeof(BlockingVpnService));
intent.SetAction(BlockingVpnService.ACTION_STOP);
context.StartService(intent);
```

## VpnServiceManager
Helper class for managing VPN service lifecycle.

### Methods

#### IsVpnPermissionGranted
Check if VPN permission is granted.
```csharp
bool hasPermission = VpnServiceManager.IsVpnPermissionGranted(context);
```

#### IsVpnServiceRunning
Check if VPN service is currently running.
```csharp
bool isRunning = VpnServiceManager.IsVpnServiceRunning(context);
```

#### StartVpnService
Start VPN service with proper foreground service handling.
```csharp
bool success = VpnServiceManager.StartVpnService(context);
```

#### StopVpnService
Stop VPN service.
```csharp
VpnServiceManager.StopVpnService(context);
```

#### GetVpnPermissionIntent
Get intent to request VPN permission from user.
```csharp
Intent? permissionIntent = VpnServiceManager.GetVpnPermissionIntent(context);
if (permissionIntent != null)
{
    activity.StartActivityForResult(permissionIntent, VPN_REQUEST_CODE);
}
```

## Example: Complete VPN Flow

```csharp
// Check permission
if (!VpnServiceManager.IsVpnPermissionGranted(context))
{
    // Request permission
    var permissionIntent = VpnServiceManager.GetVpnPermissionIntent(context);
    if (permissionIntent != null)
    {
        activity.StartActivityForResult(permissionIntent, 100);
    }
}
else
{
    // Start VPN
    bool success = VpnServiceManager.StartVpnService(context);
    if (success)
    {
        // VPN started successfully
        // Persistent notification will appear
    }
}

// Check if running
bool isRunning = VpnServiceManager.IsVpnServiceRunning(context);

// Stop VPN
VpnServiceManager.StopVpnService(context);
```

## Notification Details

### Persistent Notification
- **Cannot be dismissed** by swiping
- **Always visible** when VPN is active
- **Shows in active apps** list
- **Stop button** for quick access

### Notification Actions
- Tap notification: Opens app
- Tap "Stop VPN": Stops the service

## Service Behavior

### Foreground Service
- Runs as foreground service on Android 8.0+
- Uses `systemExempted` foreground service type
- Automatically restarts if killed (Sticky service)

### Background Operation
- Continues running when app is closed
- Survives app being swiped away from recent apps
- May be affected by battery optimization on some devices

### Service Lifecycle
1. Service starts → Creates notification → Starts foreground
2. Service running → Processes packets → Shows in active apps
3. Service stops → Removes notification → Cleans up resources

## Troubleshooting

### Service Not Appearing in Active Apps
- Ensure notification channel importance is set to High
- Verify service is started with `StartForegroundService()`
- Check that `StartForeground()` is called within 5 seconds

### Notification Can Be Dismissed
- Verify `SetOngoing(true)` is set
- Check `SetAutoCancel(false)` is set
- Ensure notification channel is created properly

### Service Stops in Background
- Check battery optimization settings
- Verify `StartCommandResult.Sticky` is returned
- Ensure proper wake lock handling

### Permission Issues
- Verify `BIND_VPN_SERVICE` permission in manifest
- Check VPN permission is granted before starting
- Handle permission denial gracefully
