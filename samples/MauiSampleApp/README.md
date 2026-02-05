# Datadog MAUI Sample App

A minimal .NET MAUI application demonstrating the `Datadog.MAUI.Symbols` plugin for automatic symbol file upload to Datadog.

## What This Demonstrates

- Integration of the Datadog symbols upload plugin in a MAUI project
- Automatic symbol upload for Android (ProGuard/R8 mapping files)
- Automatic symbol upload for iOS (dSYM bundles)
- Configuration via MSBuild properties
- Environment variable support for API keys
- **Runtime build info display** - The app shows the build configuration (service name, version, variant, build ID) that was generated during compilation, proving that MSBuild properties flow correctly into the runtime app

## Requirements

- .NET 9 SDK or later
- MAUI workload installed: `dotnet workload install maui`
- Android SDK (for Android builds)
- Xcode (for iOS builds, macOS only)
- Datadog API key (optional - uploads will be skipped if not provided)

## Configuration

The plugin is configured in `MauiSampleApp.csproj` with these properties:

```xml
<!-- Service name and version -->
<DatadogServiceName>maui-sample-app</DatadogServiceName>
<DatadogVersion>$(ApplicationDisplayVersion)</DatadogVersion>
<DatadogVariant>$(Configuration)</DatadogVariant>
```

### Setting the API Key

The app is configured to read the API key from an environment variable:

```bash
# Set environment variable
export DATADOG_API_KEY=your_api_key_here

# Or pass directly during build
dotnet build -c Release -p:DatadogApiKey=your_api_key
```

**Note**: If no API key is provided, symbol upload will be skipped automatically.

## Building

### Android

```bash
# Debug build (upload skipped by default)
dotnet build -f net9.0-android -c Debug

# Release build (generates mapping file and uploads if API key present)
dotnet build -f net9.0-android -c Release
```

The Release build enables ProGuard/R8 which generates the mapping file at:
```
obj/Release/net9.0-android/lp/map.cache
```

### iOS

```bash
# Debug build
dotnet build -f net9.0-ios -c Debug

# Release build (generates dSYM and uploads if API key present)
dotnet build -f net9.0-ios -c Release
```

dSYM bundles are typically generated in the build output directory.

## Running the App

To see the build configuration displayed at runtime:

```bash
# Using Makefile (from project root)
make sample-run-android
make sample-run-ios

# Or directly with dotnet
dotnet build -f net9.0-android -c Debug -t:Run
dotnet build -f net9.0-ios -c Debug -t:Run
```

When the app launches, scroll down to see the "Build Configuration" section displaying:
- **Service**: maui-sample-app
- **Version**: 1.0.0
- **Variant**: Debug/Release
- **Configuration**: Debug/Release
- **Build ID**: Generated 16-character hash (e.g., `bf6b5bc3d6c94cb4`)

This demonstrates that the build-time values from MSBuild are successfully passed into the runtime application.

## Testing the Plugin

### Without API Key (Safe Testing)

```bash
# Build will succeed, upload will be skipped
dotnet build -c Release -f net9.0-android
```

Look for this message in the build output:
```
⚠️  Symbol upload skipped.
To enable symbol upload, ensure DatadogApiKey is set via environment variable or project property.
```

### With API Key (Real Upload)

```bash
# Set your API key
export DATADOG_API_KEY=your_actual_api_key

# Build and upload
dotnet build -c Release -f net9.0-android
```

Look for these messages in the build output:
```
Starting symbol upload.
Uploading Android ProGuard/R8 Mapping File at location /path/to/mapping.txt
  version: 1.0.0 service: maui-sample-app variant: Release
Please ensure you use the same values during SDK initialization to guarantee the success of the symbolication process.
Collected git metadata: commit abc123d, 42 tracked files
Uploading Android ProGuard/R8 Mapping File mapping.txt (0.05 MB)
Symbol upload finished
After upload is successful, symbol files will be processed and ready to use within the next 5 minutes.

✅ Uploaded symbol 1 file (0.05 MB) in 0.156 seconds.
```

### Dry Run Mode (Test Without Uploading)

Test your configuration without making actual uploads:

```bash
# Set API key (still required for validation)
export DATADOG_API_KEY=your_api_key

# Build with dry-run mode enabled
dotnet build -c Release -f net9.0-android \
  -p:DatadogSymbolUploadDryRun=true
```

Look for dry-run output showing what would be uploaded:
```
Starting symbol upload.
Uploading Android ProGuard/R8 Mapping File at location /path/to/mapping.txt
  version: 1.0.0 service: maui-sample-app variant: Release
Collected git metadata: commit abc123d, 42 tracked files

🔍 DRY RUN MODE - No files will be uploaded

File: mapping.txt
Size: 0.05 MB
Type: Android ProGuard/R8 Mapping File
Endpoint: https://sourcemap-intake.datadoghq.com/api/v2/srcmap
Site: datadoghq.com
Service: maui-sample-app
Version: 1.0.0
Variant: Release
Git commit: abc123d
Git tracked files: 42

✅ DRY RUN: All checks passed. Upload would succeed. (0.123s)
```

## Manual Upload Target

You can also invoke the upload target manually:

```bash
dotnet build -t:DatadogUploadSymbols \
  -p:DatadogApiKey=your_api_key \
  -p:DatadogServiceName=maui-sample-app \
  -p:DatadogVersion=1.0.0 \
  -p:DatadogPlatform=android \
  -p:DatadogSymbolType=jvm_mapping_file \
  -p:DatadogSymbolFilePath=/path/to/mapping.txt
```

## Customizing Configuration

Edit `MauiSampleApp.csproj` to customize:

```xml
<PropertyGroup>
  <!-- Change service name -->
  <DatadogServiceName>my-custom-service</DatadogServiceName>

  <!-- Use different Datadog site -->
  <DatadogSite>us5.datadoghq.com</DatadogSite>

  <!-- Disable git metadata collection -->
  <DatadogIncludeGitMetadata>false</DatadogIncludeGitMetadata>

  <!-- Enable dry-run mode (test without uploading) -->
  <DatadogSymbolUploadDryRun>true</DatadogSymbolUploadDryRun>

  <!-- Always skip upload -->
  <DatadogSymbolUploadSkip>true</DatadogSymbolUploadSkip>
</PropertyGroup>
```

## Project Structure

```
MauiSampleApp/
├── MauiSampleApp.csproj    # Project file with Datadog configuration
├── App.xaml                # App definition
├── App.xaml.cs             # App code-behind
├── AppShell.xaml           # Shell navigation
├── MainPage.xaml           # Main page UI
├── MainPage.xaml.cs        # Main page logic
└── Resources/              # App resources (icons, fonts, etc.)
```

## Verifying Upload

After a successful upload, you can verify in the Datadog UI:

1. Go to your Datadog dashboard
2. Navigate to Error Tracking or APM
3. Check for symbolicated stack traces using your service name

## Troubleshooting

### Upload is skipped

- Check that you're building in Release mode
- Verify the `DATADOG_API_KEY` environment variable is set
- Look for mapping files in the build output

### Mapping file not found

- Ensure R8 is enabled for Release builds
- Check that `AndroidLinkTool` is set to `r8` in your project file
- Verify the build completed successfully

### dSYM not found

- Ensure you're building for a real device or simulator
- Check the build output directory for `.dSYM` bundles
- iOS Debug builds may not generate dSYMs by default

## See Also

- [Plugin Documentation](../../README.md)
- [Datadog Error Tracking](https://docs.datadoghq.com/real_user_monitoring/error_tracking/)
