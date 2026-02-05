# RUM SDK Integration Guide

Connecting symbol uploads to Datadog RUM for crash symbolication and error tracking.

## Overview

To properly symbolicate crashes and errors in Datadog RUM, you need to:

1. **Upload symbols** - This plugin handles uploading mapping files (Android) and dSYM bundles (iOS)
2. **Tag RUM events** - Pass `build_id` and `variant` to the RUM SDK to link crash events to uploaded symbols

Without this linkage, Datadog cannot match crash reports to the correct symbol files, resulting in obfuscated stack traces.

## Why Build ID and Variant Matter

When a crash occurs:
- The RUM SDK captures the crash with `build_id` and `variant` tags
- Datadog's error tracking service looks up symbols using these identifiers
- The matching symbol file is used to deobfuscate the stack trace

**If the tags don't match**, symbolication fails and you see raw obfuscated code.

## Auto-Generated Build Metadata

**The plugin automatically generates build metadata at compile time - no manual configuration required.**

When you build your MAUI application, the plugin automatically creates a `DatadogBuildInfo` class with all the metadata needed to link crash reports to uploaded symbols.

### What Gets Generated

The plugin creates `Datadog.MAUI.Symbols.DatadogBuildInfo` with:
- `ServiceName` - From your `DatadogServiceName` property
- `Version` - From your `DatadogVersion` property
- `Variant` - From your `DatadogVariant` property (e.g., Release, Debug)
- `BuildId` - Auto-generated unique identifier (matches uploaded symbols)

**The `BuildId` uses the same algorithm as the symbol upload**, ensuring perfect consistency between RUM events and uploaded symbol files.

### Initialize RUM SDK with Build Metadata

In your app initialization code, access the auto-generated class:

```csharp
using Datadog.Trace;
using Datadog.MAUI.Symbols; // Auto-generated namespace

public class App : Application
{
    public App()
    {
        InitializeComponent();

        // Initialize Datadog RUM SDK
        DatadogSdk.Instance.Initialize(
            clientToken: "YOUR_CLIENT_TOKEN",
            environment: "production",
            applicationId: "YOUR_APPLICATION_ID"
        );

        // Link RUM events to uploaded symbols (uses auto-generated values)
        DatadogSdk.Instance.SetBuildId(DatadogBuildInfo.BuildId);
        DatadogSdk.Instance.SetVariant(DatadogBuildInfo.Variant);

        // Optional: Add version for filtering
        DatadogSdk.Instance.SetVersion(DatadogBuildInfo.Version);

        MainPage = new AppShell();
    }
}
```

### Step 3: Verify in Datadog

After a crash is reported:

1. Go to **Error Tracking** in Datadog
2. Open a crash report
3. Check the **Tags** section for:
   - `build_id`: Should match the uploaded symbol file
   - `variant`: Should match (e.g., "Release", "Debug")
   - `version`: Should match your app version
4. Stack trace should show **deobfuscated** code with file names and line numbers

## Platform-Specific Considerations

### Android

For Android, the `build_id` is critical:

```xml
<PropertyGroup>
  <DatadogBuildId></DatadogBuildId> <!-- Auto-generated if empty -->
  <DatadogVariant>$(Configuration)</DatadogVariant> <!-- Usually "Release" -->
</PropertyGroup>
```

The generated `build_id`:
- Is based on APK hash or configuration
- Must be **exactly the same** in RUM events and uploaded symbols
- Is automatically consistent when using `GenerateBuildIdTask`

### iOS

For iOS, `variant` is typically sufficient, but `build_id` can help differentiate builds:

```xml
<PropertyGroup>
  <DatadogVariant>$(Configuration)</DatadogVariant>
  <DatadogVersion>$(ApplicationDisplayVersion)</DatadogVersion>
</PropertyGroup>
```

## Example: Multi-Platform Configuration

Simply configure the Datadog properties - the plugin handles BuildInfo generation automatically:

```xml
<PropertyGroup>
  <!-- Service configuration -->
  <DatadogServiceName>my-maui-app</DatadogServiceName>
  <DatadogVersion>$(ApplicationDisplayVersion)</DatadogVersion>
  <DatadogVariant>$(Configuration)</DatadogVariant>

  <!-- Site configuration -->
  <DatadogSite>us3.datadoghq.com</DatadogSite>
</PropertyGroup>

<!-- Platform-specific: Android gets build ID -->
<PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">
  <DatadogBuildId></DatadogBuildId> <!-- Auto-generated if empty -->
</PropertyGroup>
```

That's it! The `Datadog.MAUI.Symbols.DatadogBuildInfo` class is automatically generated during build with these values.

## Runtime Access Pattern

```csharp
using Datadog.MAUI.Symbols; // Auto-generated namespace

// Example: Display in debug UI
public class DebugPage : ContentPage
{
    public DebugPage()
    {
        Content = new StackLayout
        {
            Children =
            {
                new Label { Text = $"Service: {DatadogBuildInfo.ServiceName}" },
                new Label { Text = $"Version: {DatadogBuildInfo.Version}" },
                new Label { Text = $"Variant: {DatadogBuildInfo.Variant}" },
                new Label { Text = $"Build ID: {DatadogBuildInfo.BuildId}" }
            }
        };
    }
}

// Example: Pass to RUM SDK
public static class DatadogInitializer
{
    public static void Initialize()
    {
        DatadogSdk.Instance.Initialize(
            clientToken: Environment.GetEnvironmentVariable("DD_CLIENT_TOKEN"),
            environment: "production"
        );

        // Critical: Link to uploaded symbols
        DatadogSdk.Instance.SetBuildId(DatadogBuildInfo.BuildId);
        DatadogSdk.Instance.SetVariant(DatadogBuildInfo.Variant);
        DatadogSdk.Instance.SetVersion(DatadogBuildInfo.Version);
    }
}
```

## Troubleshooting

### Stack Traces Not Symbolicated

**Symptom**: Crash reports show obfuscated class/method names

**Causes**:
1. `build_id` in RUM doesn't match uploaded symbols
2. Symbols weren't uploaded for this build
3. `variant` mismatch (e.g., RUM shows "Debug" but symbols uploaded for "Release")

**Solutions**:
- Verify `DatadogBuildInfo` was generated: Check `DatadogBuildInfo.BuildId` at runtime
- Check Datadog Error Tracking: Ensure symbol file exists for the `build_id`
- Use verbose mode: `-p:DatadogVerbose=true` to see uploaded metadata
- Compare values: RUM event tags vs. uploaded symbol metadata
- Inspect generated file: Look at `obj/[config]/[framework]/Datadog.MAUI.Symbols/DatadogBuildInfo.g.cs`

### Build ID Changes Unexpectedly

**Symptom**: Every build generates a different `build_id`

**Cause**: Using APK hash, which changes with every build

**Solution**: Use explicit versioning:
```xml
<PropertyGroup>
  <DatadogBuildId>$(DatadogVersion)-$(DatadogVariant)</DatadogBuildId>
</PropertyGroup>
```

### RUM SDK Methods Not Available

**Symptom**: `SetBuildId()` or `SetVariant()` methods don't exist

**Cause**: Older RUM SDK version

**Solution**: Update to latest Datadog RUM SDK:
```bash
dotnet add package Datadog.Trace
```

Check SDK documentation for correct API: https://docs.datadoghq.com/real_user_monitoring/mobile_and_tv_monitoring/

## Complete Example

See the [Sample App](../samples/MauiSampleApp/) for a working implementation:
- [MauiSampleApp.csproj](../samples/MauiSampleApp/MauiSampleApp.csproj) - Configuration properties
- [MainPage.xaml.cs](../samples/MauiSampleApp/MainPage.xaml.cs) - Runtime access to `DatadogBuildInfo`
- Auto-generated `DatadogBuildInfo.g.cs` is created during build

## Additional Resources

- [Datadog Error Tracking Documentation](https://docs.datadoghq.com/real_user_monitoring/error_tracking/)
- [Mobile Crash Reporting](https://docs.datadoghq.com/real_user_monitoring/error_tracking/mobile/)
- [Android Symbol Upload](https://docs.datadoghq.com/real_user_monitoring/error_tracking/mobile/android/)
- [iOS Symbol Upload](https://docs.datadoghq.com/real_user_monitoring/error_tracking/mobile/ios/)
