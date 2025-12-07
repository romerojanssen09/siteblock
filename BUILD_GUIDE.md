# Build Guide - Site Blocking VPN (C# MAUI)

## Fixed Issues

### 1. FileOutputStream/FileInputStream
- **Issue**: Missing Java.IO namespace
- **Fix**: Added `using Java.IO;` to BlockingVpnService.cs
- **Usage**: Java streams for reading/writing VPN packets

### 2. Async File Operations
- **Issue**: C# async methods don't directly work with Java streams
- **Fix**: Wrapped Java stream operations in `Task.Run()`
- Example:
  ```csharp
  await Task.Run(() => outputStream.Write(response), cancellationToken);
  ```

### 3. VPN Permission Handling
- **Issue**: ActivityResultLauncher not directly compatible with MAUI
- **Fix**: Simplified to use `StartActivityForResult()` with delay
- **Note**: In production, implement proper result handling in MainActivity

## Build Steps

1. **Open Solution**
   ```
   Open siteblock.sln in Visual Studio 2022
   ```

2. **Select Android Target**
   - Set target to `net9.0-android` or your Android version
   - Select Android device or emulator

3. **Build**
   ```
   Build > Build Solution (Ctrl+Shift+B)
   ```

4. **Deploy**
   ```
   Debug > Start Debugging (F5)
   ```

## Required NuGet Packages

The following should be automatically included:
- `Microsoft.Maui.Controls`
- `Microsoft.Maui.Controls.Compatibility`
- `Xamarin.AndroidX.Activity`
- `Xamarin.AndroidX.AppCompat`

## Project Structure

```
siteblock/
├── Services/                       # Shared services
│   ├── BlockingRulesManager.cs    # Rule management
│   └── LogManager.cs               # Logging
├── Platforms/Android/              # Android-specific
│   ├── Services/
│   │   └── BlockingVpnService.cs  # VPN implementation
│   ├── MainActivity.cs
│   └── AndroidManifest.xml        # Permissions
├── MainPage.xaml                   # Main UI
├── MainPage.xaml.cs                # Main logic
├── LogsPage.xaml                   # Logs UI
└── LogsPage.xaml.cs                # Logs logic
```

## Key Differences from Kotlin Version

| Feature | Kotlin | C# MAUI |
|---------|--------|---------|
| Streams | FileInputStream/FileOutputStream | Java.IO.FileInputStream/FileOutputStream |
| Async | Coroutines | async/await with Task.Run() |
| Collections | StateFlow | ObservableCollection |
| UI | Jetpack Compose | XAML |
| Permissions | ActivityResultLauncher | StartActivityForResult |

## Testing

1. Build and deploy to Android device/emulator
2. Grant VPN permission when prompted
3. Add domain to block (e.g., `facebook.com`)
4. VPN will auto-restart
5. Try accessing blocked site - should fail
6. Check logs to see DNS queries

## Common Issues

### Issue: VPN won't start
- **Solution**: Check AndroidManifest.xml has all permissions
- Ensure `BIND_VPN_SERVICE` permission is present

### Issue: Compilation errors with Java types
- **Solution**: Ensure `using Java.IO;` is present
- Check Android SDK is properly installed

### Issue: VPN permission not granted
- **Solution**: Manually grant VPN permission in Android settings
- Or implement proper ActivityResult handling

## Production Improvements

1. **Proper VPN Permission Handling**
   - Implement `OnActivityResult` in MainActivity
   - Pass result back to MainPage

2. **Error Handling**
   - Add try-catch around VPN operations
   - Show user-friendly error messages

3. **Persistence**
   - Save blocked rules to preferences
   - Restore on app restart

4. **Performance**
   - Optimize packet processing
   - Add packet buffering

5. **UI Enhancements**
   - Add search/filter for rules
   - Export/import rule lists
   - Statistics dashboard
