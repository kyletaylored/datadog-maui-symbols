# Datadog.MAUI.Symbols

Native .NET MSBuild plugin for uploading symbol files to Datadog from MAUI applications.

> **Note**: This is a community package (`kyletaylored.Datadog.MAUI.Symbols`) that may be superseded by an official `Datadog.MAUI.Symbols`.

## Features

- **Platform Support**: Android (ProGuard/R8), iOS (dSYM), Flutter symbols
- **Build Integration**: Automatic upload via MSBuild targets
- **Git Metadata**: Automatic commit SHA and tracked files collection
- **Build ID Support**: Optional build ID for Android (matches native gradle task)
- **Dry Run Mode**: Test configuration without uploading
- **Verbose Debugging**: Detailed HTTP request/response logging
- **Flexible Configuration**: Environment variables, command line, or project file

## Quick Start

### 1. Install the Package

```bash
dotnet add package kyletaylored.Datadog.MAUI.Symbols
```

### 2. Enable Symbol Generation

Add to your `.csproj`:

```xml
<!-- Android: Enable R8 code shrinking -->
<PropertyGroup Condition="'$(Configuration)' == 'Release' AND $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">
  <AndroidLinkMode>Full</AndroidLinkMode>
  <AndroidLinkTool>r8</AndroidLinkTool>
</PropertyGroup>

<!-- iOS: Enable dSYM generation -->
<PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
  <MtouchDebugSymbols>true</MtouchDebugSymbols>
  <GenerateDsymBundle>true</GenerateDsymBundle>
</PropertyGroup>
```

### 3. Configure Required Settings

```xml
<PropertyGroup>
  <!-- API Key - use environment variable to keep out of source control -->
  <DatadogApiKey Condition="'$(DatadogApiKey)' == ''">$(DATADOG_API_KEY)</DatadogApiKey>

  <!-- Required: Service name -->
  <DatadogServiceName>my-mobile-app</DatadogServiceName>

  <!-- Required: App version -->
  <DatadogVersion>1.0.0</DatadogVersion>

  <!-- Optional: Datadog site (defaults to datadoghq.com) -->
  <DatadogSite>datadoghq.com</DatadogSite>
</PropertyGroup>
```

### 4. Publish Your App

```bash
# Set your API key
export DATADOG_API_KEY=your_api_key_here
export DATADOG_SITE=datadoghq.com  # or us3.datadoghq.com, datadoghq.eu, etc.

# Android
dotnet publish -c Release -f net9.0-android

# iOS
dotnet publish -c Release -f net9.0-ios
```

That's it! The plugin will automatically validate your API key and upload symbols after each Release build.

## Requirements

**IMPORTANT**: This plugin uploads symbol files but cannot generate them. You must enable symbol generation:

### Android Requirements

Enable R8 code shrinking to generate `mapping.txt`:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release' AND $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">
  <AndroidLinkMode>Full</AndroidLinkMode>
  <AndroidLinkTool>r8</AndroidLinkTool>
</PropertyGroup>
```

### iOS Requirements

Enable dSYM generation:

```xml
<PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
  <MtouchDebugSymbols>true</MtouchDebugSymbols>
  <GenerateDsymBundle>true</GenerateDsymBundle>
</PropertyGroup>
```

**Verify symbol files are generated:**

```bash
# Android
find . -name "mapping.txt" -type f

# iOS
find . -name "*.dSYM" -type d
```

## RUM SDK Integration

The plugin automatically generates a `DatadogBuildInfo` class at build time with metadata needed to connect crash reports to uploaded symbols. No manual configuration required.

Simply access the auto-generated values in your app:

```csharp
// Initialize RUM with build metadata
DatadogSdk.Instance.SetBuildId(Datadog.MAUI.Symbols.DatadogBuildInfo.BuildId);
DatadogSdk.Instance.SetVariant(Datadog.MAUI.Symbols.DatadogBuildInfo.Variant);
```

The `DatadogBuildInfo` class provides:
- `ServiceName` - From `DatadogServiceName` property
- `Version` - From `DatadogVersion` property
- `Variant` - From `DatadogVariant` property (e.g., Release, Debug)
- `BuildId` - Auto-generated unique identifier

This links crash events to the uploaded symbols for accurate deobfuscation.

## Documentation

- **[RUM SDK Integration](docs/RUM_INTEGRATION.md)** - Connecting symbols to crash reports in Datadog RUM
- **[Configuration Guide](docs/CONFIGURATION.md)** - Complete configuration reference, API key setup, site configuration
- **[Usage Guide](docs/USAGE.md)** - Detailed usage examples, dry run mode, verbose debugging, CI/CD integration
- **[Troubleshooting](docs/TROUBLESHOOTING.md)** - Common issues and solutions
- **[Architecture](docs/ARCHITECTURE.md)** - Technical architecture and API details
- **[Symbol Files Guide](docs/SYMBOL_FILES.md)** - Enabling, verifying, and manually uploading symbol files
- **[Build vs Publish](docs/BUILD_VS_PUBLISH.md)** - Understanding when to use `dotnet build` vs `dotnet publish`
- **[Development Guide](DEVELOPMENT.md)** - Building, testing, and contributing
- **[Sample App](samples/MauiSampleApp/README.md)** - Example MAUI application

## Sample Application

A working example is available in [`samples/MauiSampleApp/`](samples/MauiSampleApp/):

```bash
# Android
make sample-run-android

# iOS
make sample-run-ios
```

## Migration to Official Package

If an official `Datadog.MAUI.Symbols` package is released:

1. Update your `.csproj`:

   ```xml
   <!-- Before -->
   <PackageReference Include="kyletaylored.Datadog.MAUI.Symbols" Version="0.1.0" />

   <!-- After -->
   <PackageReference Include="Datadog.MAUI.Symbols" Version="0.1.0" />
   ```

2. That's it! No code changes, property changes, or configuration updates needed.

All namespaces, target files, and MSBuild properties remain identical.

## Common Issues

### Upload Skipped

Set your API key:

```bash
export DATADOG_API_KEY=your_api_key_here
```

### Symbol File Not Found

Ensure R8/dSYM generation is enabled (see [Requirements](#requirements)).

### Invalid API Key

Verify your API key and site configuration match your Datadog organization.

For more issues, see the [Troubleshooting Guide](docs/TROUBLESHOOTING.md).

## Contributing

Contributions welcome! See [DEVELOPMENT.md](DEVELOPMENT.md) for details.

## License

Apache-2.0

---

**Links:**

- [NuGet Package](https://www.nuget.org/packages/kyletaylored.Datadog.MAUI.Symbols/)
- [GitHub Repository](https://github.com/kyletaylored/datadog-maui-symbols)
- [Datadog Documentation](https://docs.datadoghq.com/real_user_monitoring/error_tracking/mobile/android/)
