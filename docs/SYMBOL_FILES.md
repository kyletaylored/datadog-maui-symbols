# Symbol File Generation and Management

This guide explains how to enable, verify, and manually upload symbol files.

## Quick Reference

### Finding Symbol Files

```bash
# Use the Makefile command
make sample-find-symbols

# Or search manually:
# Android mapping files
find . -name "mapping.txt" -type f

# iOS dSYM bundles
find . -name "*.dSYM" -type d
```

### Common Locations

**Android R8 Mapping Files:**
- Release builds: `obj/Release/net9.0-android/lp/*/mapping.txt`
- Also copied to: `bin/Release/net9.0-android/mapping.txt`

**iOS dSYM Bundles:**
- Device builds: `bin/Release/net9.0-ios/ios-arm64/YourApp.app.dSYM/`
- Simulator builds: `bin/Release/net9.0-ios/iossimulator-arm64/YourApp.app.dSYM/` (if enabled)

## Enabling Symbol File Generation

### Android: R8/ProGuard Mapping Files

Add to your `.csproj`:

```xml
<!-- Enable R8 code shrinking (generates mapping.txt) -->
<PropertyGroup Condition="'$(Configuration)' == 'Release' AND $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">
  <AndroidLinkMode>Full</AndroidLinkMode>
  <AndroidLinkTool>r8</AndroidLinkTool>
  <AndroidCreatePackagePerAbi>false</AndroidCreatePackagePerAbi>
</PropertyGroup>
```

**Verify it's enabled:**

```bash
# Publish Release and check for mapping file
dotnet publish -c Release -f net9.0-android
find . -name "mapping.txt" -type f
```

**Important**: Use `dotnet publish` (not `dotnet build`) to generate mapping files in Release builds.

### iOS: dSYM Debug Symbols

Add to your `.csproj`:

```xml
<!-- Enable dSYM generation -->
<PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
  <MtouchDebugSymbols>true</MtouchDebugSymbols>
  <GenerateDsymBundle>true</GenerateDsymBundle>
</PropertyGroup>
```

**Verify it's enabled:**

```bash
# Publish Release and check for dSYM
dotnet publish -c Release -f net9.0-ios
find . -name "*.dSYM" -type d
```

**Important**:
- Use `dotnet publish` (not `dotnet build`) to generate dSYM files
- iOS simulator builds may not generate dSYM files even with this setting enabled
- For guaranteed dSYM generation, publish for a physical device

## Manual Upload Without Rebuilding

If you already have symbol files from a previous build, you can upload them without rebuilding:

### Android Mapping File

```bash
dotnet build samples/MauiSampleApp/MauiSampleApp.csproj \
  -t:DatadogUploadSymbols \
  -p:DatadogPlatform=android \
  -p:DatadogSymbolType=jvm_mapping_file \
  -p:DatadogSymbolFilePath="/path/to/mapping.txt" \
  -p:DatadogServiceName="my-app" \
  -p:DatadogVersion="1.0.0" \
  -p:DatadogVariant="Release" \
  -p:DatadogApiKey="$DATADOG_API_KEY"
```

### iOS dSYM Bundle

```bash
dotnet build samples/MauiSampleApp/MauiSampleApp.csproj \
  -t:DatadogUploadSymbols \
  -p:DatadogPlatform=ios \
  -p:DatadogSymbolType=ios_dsym \
  -p:DatadogSymbolFilePath="/path/to/YourApp.app.dSYM" \
  -p:DatadogServiceName="my-app" \
  -p:DatadogVersion="1.0.0" \
  -p:DatadogVariant="Release" \
  -p:DatadogApiKey="$DATADOG_API_KEY"
```

### Dry Run First

Always test with dry-run mode first:

```bash
# Add to any of the above commands:
-p:DatadogSymbolUploadDryRun=true \
-p:DatadogApiKey="dummy-key-for-dryrun"
```

## Troubleshooting

### No Symbol Files Generated

**Important**: Use `dotnet publish` (not `dotnet build`) for Release builds to generate symbol files.

**Android:**
1. Check that `AndroidLinkTool=r8` is set for Release builds
2. Verify you're using `dotnet publish -c Release` (not `dotnet build`)
3. R8 only runs for Release publish operations by default

**iOS:**
1. Check that `MtouchDebugSymbols=true` and `GenerateDsymBundle=true` are set
2. Verify you're using `dotnet publish -c Release` (not `dotnet build`)
3. Check the postprocessing.items file for dSYM status:
   ```bash
   find . -name "postprocessing.items" -exec grep -H "dSYMSourcePathExists" {} \;
   ```
4. Try publishing for a physical device instead of simulator:
   ```bash
   dotnet publish -c Release -f net9.0-ios -p:RuntimeIdentifier=ios-arm64
   ```
5. Some Xcode versions require additional settings

### Plugin Says "Symbol File Not Found"

1. Verify files exist:
   ```bash
   make sample-find-symbols
   ```

2. Check the exact path the plugin is looking for:
   ```bash
   # Publish with verbose logging
   dotnet publish -c Release -v:detailed | grep -i "symbol\|mapping\|dsym"
   ```

3. Manually specify the path:
   ```xml
   <DatadogSymbolFilePath>/absolute/path/to/symbol/file</DatadogSymbolFilePath>
   ```

### Symbol File Exists But Upload Skipped

Check if upload is disabled:

```bash
# Look for this in build output:
# "⚠️  Symbol upload skipped."
```

Solutions:
- Set `DATADOG_API_KEY` environment variable
- Or set `DatadogApiKey` in your `.csproj`
- Check that `DatadogSymbolUploadEnabled=true` (default)

## File Size Information

**Typical Android mapping.txt sizes:**
- Small app (10-50 classes): 50-500 KB
- Medium app (100-500 classes): 1-5 MB
- Large app (1000+ classes): 10-50 MB
- Very large app with many dependencies: 50-200 MB

**Typical iOS dSYM sizes:**
- Small app: 5-20 MB
- Medium app: 20-100 MB
- Large app: 100-500 MB

The plugin will display the file size when uploading.

## Build Configuration Best Practices

### For Development

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <!-- Skip symbol upload in Debug builds -->
  <DatadogSymbolUploadSkip>true</DatadogSymbolUploadSkip>
</PropertyGroup>
```

### For Release/Production

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <!-- Always generate and upload symbols in Release builds -->
  <DatadogApiKey>$(DATADOG_API_KEY)</DatadogApiKey>
  <DatadogServiceName>my-app</DatadogServiceName>
  <DatadogVersion>$(ApplicationDisplayVersion)</DatadogVersion>
</PropertyGroup>
```

### For CI/CD

```xml
<PropertyGroup>
  <!-- Get API key from environment variable -->
  <DatadogApiKey Condition="'$(DatadogApiKey)' == ''">$(DATADOG_API_KEY)</DatadogApiKey>

  <!-- Skip if no API key (local builds) -->
  <DatadogSymbolUploadSkip Condition="'$(DatadogApiKey)' == ''">true</DatadogSymbolUploadSkip>
</PropertyGroup>
```

## See Also

- [README.md](../README.md) - Main documentation
- [ARCHITECTURE.md](ARCHITECTURE.md) - Technical implementation details
- [DEVELOPMENT.md](../DEVELOPMENT.md) - Development and testing guide
