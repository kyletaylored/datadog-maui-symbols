# Usage Guide

Detailed guide for using the Datadog.MAUI.Symbols plugin in various scenarios.

## Automatic Upload (Default)

The plugin automatically uploads symbols after publishing Release builds for Android and iOS:

```bash
# Android - generates mapping.txt and uploads to Datadog
dotnet publish -c Release -f net9.0-android

# iOS - generates dSYM bundle and uploads to Datadog
dotnet publish -c Release -f net9.0-ios
```

**Note**: Use `dotnet publish` (not `dotnet build`) for Release builds to ensure symbol files are generated. Debug builds use `dotnet build` and skip symbol upload by default.

**Example Output:**

```
[Datadog.Symbols] Symbol upload target triggered for android
API key validated successfully.
[Datadog.Symbols] Starting symbol upload.
[Datadog.Symbols] Uploading Android ProGuard/R8 Mapping File at location /path/to/mapping.txt
[Datadog.Symbols]   build_id: 7ce2df430df5f1bb
[Datadog.Symbols] Uploading Android ProGuard/R8 Mapping File mapping.txt (16.59 MB)
Symbol upload finished
```

The plugin will:

1. **Validate your API key** against the Datadog API endpoint
2. **Locate symbol files** in your build output directory
3. **Collect git metadata** (commit SHA, tracked files) if available
4. **Upload symbols** to the Datadog API with gzip compression
5. **Report results** with clear success/error messages

## Manual Upload

Invoke the upload target manually:

```bash
dotnet build -t:DatadogUploadSymbols \
  -p:DatadogApiKey=YOUR_API_KEY \
  -p:DatadogServiceName=my-app \
  -p:DatadogVersion=1.0.0 \
  -p:TargetPlatformIdentifier=android
```

## Dry Run Mode

Test your configuration without uploading files. Dry run mode performs all checks (file location, metadata building, git collection) but skips the actual HTTP upload:

```bash
# Test during publish
dotnet publish -c Release -f net9.0-android \
  -p:DatadogSymbolUploadDryRun=true
```

Or configure in your `.csproj`:

```xml
<PropertyGroup>
  <DatadogSymbolUploadDryRun>true</DatadogSymbolUploadDryRun>
</PropertyGroup>
```

**Dry Run Output Example:**

```
Starting symbol upload.
Uploading Android ProGuard/R8 Mapping File at location /path/to/mapping.txt
  version: 1.0.0 service: my-app variant: Release
Collected git metadata: commit abc123d, 42 tracked files

🔍 DRY RUN MODE - No files will be uploaded

File: mapping.txt
Size: 0.05 MB
Type: Android ProGuard/R8 Mapping File
Endpoint: https://sourcemap-intake.datadoghq.com/api/v2/srcmap
Site: datadoghq.com
Service: my-app
Version: 1.0.0
Variant: Release
Git commit: abc123d
Git tracked files: 42

✅ DRY RUN: All checks passed. Upload would succeed. (0.123s)
```

## Verbose Mode

Enable verbose logging to see detailed HTTP request/response information for debugging:

```bash
# Via command line
dotnet publish -c Release -f net9.0-android \
  -p:DatadogVerbose=true

# Or in .csproj
<PropertyGroup>
  <DatadogVerbose>true</DatadogVerbose>
</PropertyGroup>
```

**Verbose Output Shows:**

- Complete multipart form data structure (all fields being sent)
- HTTP method, URL, and all request headers
- Content headers (Content-Type, Content-Encoding)
- Response status code, headers, and body

**Example Verbose Output:**

```
[Datadog.Symbols] === Multipart Form Data ===

[Datadog.Symbols] Field: 'type' = 'jvm_mapping_file'
[Datadog.Symbols] Field: 'service' = 'maui-sample-app'
[Datadog.Symbols] Field: 'version' = '1.0.0'
[Datadog.Symbols] Field: 'platform' = 'android'
[Datadog.Symbols] Field: 'variant' = 'Release'
[Datadog.Symbols] Field: 'build_id' = '7ce2df430df5f1bb'

[Datadog.Symbols] Field: 'jvm_mapping_file' (file)
[Datadog.Symbols]   Filename: mapping.txt
[Datadog.Symbols]   Size: 17,393,029 bytes

[Datadog.Symbols] === HTTP Request Debug ===
[Datadog.Symbols] Method: POST
[Datadog.Symbols] URL: https://sourcemap-intake.us3.datadoghq.com/api/v2/srcmap

[Datadog.Symbols] Request Headers:
[Datadog.Symbols]   DD-API-KEY: ***masked***
[Datadog.Symbols]   DD-EVP-ORIGIN: datadog-maui-symbols
[Datadog.Symbols]   DD-EVP-ORIGIN-VERSION: 0.1.0.0

[Datadog.Symbols] Content Headers:
[Datadog.Symbols]   Content-Type: multipart/form-data; boundary="..."
[Datadog.Symbols]   Content-Encoding: gzip

[Datadog.Symbols] === HTTP Response ===
[Datadog.Symbols] Status: 202 Accepted

[Datadog.Symbols] Response Headers:
[Datadog.Symbols]   Date: Thu, 05 Feb 2026 09:04:54 GMT
```

**Use verbose mode when:**

- Troubleshooting upload failures
- Verifying which fields are being sent to the API
- Debugging API key or site configuration issues
- Confirming symbol file is being read correctly

## Platform-Specific Notes

### Android

- **Symbol Type**: `jvm_mapping_file`
- **Default Location**: `obj/Release/net9.0-android/lp/*/mapping.txt`
- **Build ID**: Auto-generated if not specified
- **Requirements**: R8/ProGuard must be enabled (see [Requirements](../README.md#android-requirements))

**Android-specific properties:**

```xml
<PropertyGroup>
  <DatadogBuildId></DatadogBuildId>  <!-- Auto-generated from APK hash -->
</PropertyGroup>
```

### iOS

- **Symbol Type**: `ios_dsym`
- **Default Location**: Auto-detected `.dSYM` bundles in `bin/Release/`
- **Architecture**: Auto-detected from `RuntimeIdentifier` (arm64/x64)
- **Requirements**: dSYM generation must be enabled (see [Requirements](../README.md#ios-requirements))

## CI/CD Integration

### GitHub Actions

```yaml
- name: Publish Android with Symbol Upload
  env:
    DATADOG_API_KEY: ${{ secrets.DATADOG_API_KEY }}
    DATADOG_SITE: us3.datadoghq.com
  run: |
    dotnet publish -c Release -f net9.0-android \
      -p:DatadogServiceName=my-app \
      -p:DatadogVersion=${{ github.ref_name }}
```

### Azure Pipelines

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Publish Android with Symbols'
  inputs:
    command: 'publish'
    arguments: '-c Release -f net9.0-android'
  env:
    DATADOG_API_KEY: $(DatadogApiKey)
    DATADOG_SITE: us3.datadoghq.com
```

### GitLab CI

```yaml
publish-android:
  script:
    - export DATADOG_API_KEY=$DATADOG_API_KEY
    - dotnet publish -c Release -f net9.0-android
```

## Disabling Upload

### For Specific Builds

```bash
# Skip upload for this build only
dotnet publish -c Release -f net9.0-android -p:DatadogSymbolUploadSkip=true
```

### For Debug Builds (Default)

Debug builds automatically skip symbol upload. To enable for Debug:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DatadogSymbolUploadSkip>false</DatadogSymbolUploadSkip>
</PropertyGroup>
```

### Disable Completely

```xml
<PropertyGroup>
  <DatadogSymbolUploadEnabled>false</DatadogSymbolUploadEnabled>
</PropertyGroup>
```

## Migration from Official Package

When the official `Datadog.MAUI.Symbols` package becomes available, migration is simple:

1. **Update PackageReference** in your `.csproj`:
   ```xml
   <!-- Before -->
   <PackageReference Include="kyletaylored.Datadog.MAUI.Symbols" Version="0.1.0" />

   <!-- After -->
   <PackageReference Include="Datadog.MAUI.Symbols" Version="0.1.0" />
   ```

2. **That's it!** No code changes, property changes, or configuration updates needed.

All namespaces, target files, and MSBuild properties remain identical between the personal and official packages.
