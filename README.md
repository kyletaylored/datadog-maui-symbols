# Datadog.MAUI.SymbolsUpload

Native .NET MSBuild plugin for uploading symbol files to Datadog from MAUI applications. This plugin eliminates the Node.js dependency by directly calling the Datadog API.

## Features

- **No Node.js Required**: Pure .NET implementation with direct API communication
- **Platform Support**: Android (ProGuard/R8), iOS (dSYM), Flutter symbols
- **Build Integration**: Automatic upload via MSBuild targets
- **API Key Validation**: Validates credentials before upload for fast-fail behavior
- **Git Metadata**: Automatically collects commit SHA, remote URL, and tracked files
- **Build ID Support**: Optional build ID parameter for Android, matching native gradle task behavior
- **Dry Run Mode**: Test configuration without uploading files
- **Verbose Debugging**: Detailed HTTP request/response logging for troubleshooting
- **Flexible Configuration**: Configure via MSBuild properties or environment variables

## Quick Start

1. **Install the package**:
   ```bash
   dotnet add package Datadog.MAUI.SymbolsUpload
   ```

2. **Enable symbol generation** in your `.csproj`:
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

3. **Configure required settings**:
   ```xml
   <PropertyGroup>
     <!-- API Key - use environment variable to keep it out of source control -->
     <DatadogApiKey Condition="'$(DatadogApiKey)' == ''">$(DATADOG_API_KEY)</DatadogApiKey>

     <!-- Required: Service name -->
     <DatadogServiceName>my-mobile-app</DatadogServiceName>

     <!-- Required: App version -->
     <DatadogVersion>1.0.0</DatadogVersion>

     <!-- Optional: Datadog site (defaults to datadoghq.com) -->
     <DatadogSite>datadoghq.com</DatadogSite>
   </PropertyGroup>
   ```

4. **Set your API key** and publish:
   ```bash
   export DATADOG_API_KEY=your_api_key_here
   export DATADOG_SITE=datadoghq.com  # or us3.datadoghq.com, datadoghq.eu, etc.

   # Android
   dotnet publish -c Release -f net9.0-android

   # iOS
   dotnet publish -c Release -f net9.0-ios
   ```

That's it! The plugin will automatically validate your API key and upload symbols after each Release build.

## Installation

Add the NuGet package to your MAUI project:

```bash
dotnet add package Datadog.MAUI.SymbolsUpload
```

Or add to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Datadog.MAUI.SymbolsUpload" Version="0.1.0" />
</ItemGroup>
```

## Requirements

**IMPORTANT**: This plugin uploads symbol files to Datadog, but it cannot generate them. You must enable symbol file generation in your project configuration:

### Android Requirements

Enable R8 code shrinking to generate ProGuard/R8 mapping files:

```xml
<!-- Required for Android symbol upload -->
<PropertyGroup Condition="'$(Configuration)' == 'Release' AND $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">
  <AndroidLinkMode>Full</AndroidLinkMode>
  <AndroidLinkTool>r8</AndroidLinkTool>
</PropertyGroup>
```

This generates a `mapping.txt` file that the plugin uploads to Datadog for crash deobfuscation.

### iOS Requirements

Enable dSYM generation for debug symbols:

```xml
<!-- Required for iOS symbol upload -->
<PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
  <MtouchDebugSymbols>true</MtouchDebugSymbols>
  <GenerateDsymBundle>true</GenerateDsymBundle>
</PropertyGroup>
```

This generates `.dSYM` bundles that the plugin uploads to Datadog for crash symbolication.

**Note**: Without these settings, the plugin will report "Could not locate symbol file" and skip the upload.

### Verifying Symbol File Generation

To check if symbol files are being generated:

```bash
# Android - look for mapping.txt
find . -name "mapping.txt" -type f

# iOS - look for .dSYM bundles
find . -name "*.dSYM" -type d

# Or search in specific build output directories:
# Android: obj/Release/net9.0-android/lp/*/mapping.txt
# iOS: bin/Release/net9.0-ios/*/*.app.dSYM
```

## Configuration

You can configure the plugin in multiple ways. Choose the method that best fits your workflow:

### Configuration Methods

#### 1. Environment Variables (Recommended for API Keys)

Best for keeping secrets out of source control:

```bash
# Set once in your shell or CI/CD environment
export DATADOG_API_KEY=your_api_key_here
export DATADOG_SITE=us3.datadoghq.com

# Then publish normally
dotnet publish -c Release -f net9.0-android
```

#### 2. Command Line Properties

Pass properties directly via `-p:` flag:

```bash
dotnet publish -c Release -f net9.0-android \
  -p:DatadogApiKey=$DATADOG_API_KEY \
  -p:DatadogSite=us3.datadoghq.com \
  -p:DatadogServiceName=my-app
```

#### 3. Project File (.csproj)

Add to your `.csproj` file:

```xml
<PropertyGroup>
  <!-- Read from environment variable (recommended) -->
  <DatadogApiKey Condition="'$(DatadogApiKey)' == ''">$(DATADOG_API_KEY)</DatadogApiKey>

  <!-- Service configuration -->
  <DatadogServiceName>my-mobile-app</DatadogServiceName>
  <DatadogVersion>$(ApplicationVersion)</DatadogVersion>
  <DatadogSite>us3.datadoghq.com</DatadogSite>
</PropertyGroup>
```

#### 4. Directory.Build.props (Multi-Project)

For solutions with multiple projects, create `Directory.Build.props` in your solution root:

```xml
<Project>
  <PropertyGroup>
    <!-- Shared across all projects -->
    <DatadogApiKey Condition="'$(DatadogApiKey)' == ''">$(DATADOG_API_KEY)</DatadogApiKey>
    <DatadogSite>us3.datadoghq.com</DatadogSite>
    <DatadogServiceName>my-mobile-app</DatadogServiceName>
  </PropertyGroup>
</Project>
```

### Required Settings

At minimum, you need:

```xml
<PropertyGroup>
  <!-- API Key (use environment variable method shown above) -->
  <DatadogApiKey>YOUR_API_KEY</DatadogApiKey>

  <!-- Service name (always required) -->
  <DatadogServiceName>my-mobile-app</DatadogServiceName>

  <!-- App version (always required) -->
  <DatadogVersion>1.0.0</DatadogVersion>
</PropertyGroup>
```

**Security Note:** Never commit API keys directly to source control. Use environment variables or CI/CD secrets instead.

### Optional Settings

```xml
<PropertyGroup>
  <!-- Build ID - alternative to service/version/variant -->
  <DatadogBuildId></DatadogBuildId>

  <!-- Build variant/flavor - defaults to $(Configuration) -->
  <DatadogVariant>release</DatadogVariant>

  <!-- Datadog site - defaults to datadoghq.com -->
  <DatadogSite>us3.datadoghq.com</DatadogSite>

  <!-- Disable git operations - prevents git invocation and repository metadata - defaults to false -->
  <DatadogDisableGit>false</DatadogDisableGit>

  <!-- Custom repository URL - overrides git-detected remote URL -->
  <DatadogRepositoryUrl>https://github.com/my-company/my-project</DatadogRepositoryUrl>

  <!-- [Deprecated] Use DatadogDisableGit instead - defaults to true -->
  <DatadogIncludeGitMetadata>true</DatadogIncludeGitMetadata>

  <!-- Skip upload - defaults to false (true for Debug builds) -->
  <DatadogSymbolUploadSkip>false</DatadogSymbolUploadSkip>

  <!-- Dry run mode - performs all checks but skips actual upload - defaults to false -->
  <DatadogSymbolUploadDryRun>false</DatadogSymbolUploadDryRun>

  <!-- Skip API key validation - useful for air-gapped environments - defaults to false -->
  <DatadogSkipApiKeyValidation>false</DatadogSkipApiKeyValidation>

  <!-- Enable verbose logging - shows full HTTP request/response details - defaults to false -->
  <DatadogVerbose>false</DatadogVerbose>

  <!-- Disable upload entirely - defaults to true -->
  <DatadogSymbolUploadEnabled>true</DatadogSymbolUploadEnabled>

  <!-- Manual symbol file path (auto-detected if not specified) -->
  <DatadogSymbolFilePath></DatadogSymbolFilePath>
</PropertyGroup>
```

### Datadog Site Configuration

The `DatadogSite` property specifies which Datadog region to upload symbols to. This must match the region where your Datadog organization is hosted.

**Valid site values:**

| Region | Site Value | Description |
|--------|-----------|-------------|
| US1 | `datadoghq.com` | Default US region |
| US3 | `us3.datadoghq.com` | US3 region |
| US5 | `us5.datadoghq.com` | US5 region |
| EU1 | `datadoghq.eu` | European region |
| AP1 | `ap1.datadoghq.com` | Asia-Pacific region |
| US1-FED | `ddog-gov.com` | US government region |

**Example:**
```xml
<PropertyGroup>
  <DatadogSite>us3.datadoghq.com</DatadogSite>
</PropertyGroup>
```

Or via environment variable:
```bash
export DATADOG_SITE=us3.datadoghq.com
dotnet publish -c Release -f net9.0-android -p:DatadogSite=us3.datadoghq.com
```

**Important**: Ensure the site matches your Datadog organization's region, or symbol uploads will fail.

### Git Integration Control

By default, the plugin automatically collects git metadata (commit SHA, remote URL, tracked files) to enhance crash symbolication and link errors to specific commits. You can control this behavior:

**Disable Git Entirely** (matches CLI `--disable-git`):
```xml
<PropertyGroup>
  <!-- Prevents git invocation and repository metadata collection -->
  <DatadogDisableGit>true</DatadogDisableGit>
</PropertyGroup>
```

**Override Repository URL** (matches CLI `--repository-url`):
```xml
<PropertyGroup>
  <!-- Use custom repository URL instead of git-detected remote -->
  <DatadogRepositoryUrl>https://github.com/my-company/my-project</DatadogRepositoryUrl>
</PropertyGroup>
```

**Common Use Cases:**
- **CI/CD without git**: Set `DatadogDisableGit=true` when git is unavailable or not needed
- **Private CI with public mirror**: Use `DatadogRepositoryUrl` to link to public GitHub URL instead of internal git remote
- **Shallow clones**: Use `DatadogRepositoryUrl` when git metadata is incomplete

### API Key Validation

The plugin automatically validates your Datadog API key before attempting uploads. This provides fast-fail behavior if your key is invalid or misconfigured.

**How it works:**
- Before upload, the plugin sends a validation request to `https://api.<site>/api/v1/validate`
- Invalid keys fail immediately with a clear error message
- Validation is skipped during dry-run mode

**Skip validation** (for air-gapped environments or testing):
```xml
<PropertyGroup>
  <!-- Skip API key validation before upload -->
  <DatadogSkipApiKeyValidation>true</DatadogSkipApiKeyValidation>
</PropertyGroup>
```

**Error handling:**
- If validation fails, you'll see: `Invalid API key for Datadog site <site>`
- Network errors during validation are ignored (upload proceeds)
- Validation timeout (10s) is ignored (upload proceeds)

## Usage

### Automatic Upload (Default)

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

### Manual Upload

Invoke the upload target manually:

```bash
dotnet build -t:DatadogUploadSymbols \
  -p:DatadogApiKey=YOUR_API_KEY \
  -p:DatadogServiceName=my-app \
  -p:DatadogVersion=1.0.0 \
  -p:DatadogPlatform=android \
  -p:DatadogSymbolType=jvm_mapping_file \
  -p:DatadogSymbolFilePath=/path/to/mapping.txt
```

### Dry Run Mode

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

### Verbose Mode

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
[Datadog.Symbols]   DD-EVP-ORIGIN: datadog-maui-symbols-upload
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

### Using Build ID (Android)

For Android, you can optionally include a `BuildId` in addition to `ServiceName`/`Version`:

```xml
<PropertyGroup>
  <DatadogApiKey>YOUR_API_KEY</DatadogApiKey>
  <DatadogServiceName>my-mobile-app</DatadogServiceName>
  <DatadogVersion>1.0.0</DatadogVersion>
  <!-- Build ID will be auto-generated from APK hash or configuration -->
  <DatadogBuildId></DatadogBuildId>
</PropertyGroup>
```

The plugin will automatically generate a build ID based on:

1. APK file hash (if available)
2. Configuration hash (version + variant + version code + timestamp)

**Note**: `BuildId` is supplemental and does not replace `ServiceName`/`Version`.

## Platform-Specific Notes

### Android

- **Symbol Type**: `jvm_mapping_file`
- **Default Location**: `build/outputs/mapping/{variant}/mapping.txt`
- **Build ID**: Auto-generated if not specified
- **Trigger**: Runs after `BuildAndroid` target

### iOS

- **Symbol Type**: `ios_dsym`
- **Default Location**: Auto-detected `.dSYM` bundles
- **Architecture**: Auto-detected from `RuntimeIdentifier` (arm64/x64)
- **Trigger**: Runs after `Build` target

## Sample Application

A working example MAUI app is available in [`samples/MauiSampleApp/`](samples/MauiSampleApp/). This demonstrates:

- Plugin integration for Android and iOS
- Configuration via MSBuild properties
- Environment variable support for API keys
- ProGuard/R8 and dSYM generation
- Runtime display of build configuration (service name, version, variant, build ID)

Quick start:

```bash
# Build and run on Android
make sample-run-android

# Build and run on iOS simulator
make sample-run-ios
```

See the [sample README](samples/MauiSampleApp/README.md) for details.

## Example Configurations

### Basic Android App

```xml
<PropertyGroup>
  <DatadogApiKey>dd_api_key_123</DatadogApiKey>
  <DatadogServiceName>my-android-app</DatadogServiceName>
  <DatadogVersion>1.2.3</DatadogVersion>
</PropertyGroup>
```

### Multi-Platform MAUI App

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net8.0-android'">
  <DatadogApiKey>dd_api_key_123</DatadogApiKey>
  <DatadogServiceName>my-maui-app-android</DatadogServiceName>
  <DatadogVersion>$(ApplicationVersion)</DatadogVersion>
</PropertyGroup>

<PropertyGroup Condition="'$(TargetFramework)' == 'net8.0-ios'">
  <DatadogApiKey>dd_api_key_123</DatadogApiKey>
  <DatadogServiceName>my-maui-app-ios</DatadogServiceName>
  <DatadogVersion>$(ApplicationVersion)</DatadogVersion>
</PropertyGroup>
```

## Troubleshooting

### Upload is Skipped

```
⚠️  Symbol upload skipped.
To enable symbol upload, ensure DatadogApiKey is set via environment variable or project property.
```

**Solution**: Set the `DATADOG_API_KEY` environment variable or `DatadogApiKey` MSBuild property.

### Symbol File Not Found

```
Could not locate symbol file for platform 'android'
```

**Solutions**:

- **For Android**: Ensure R8/ProGuard is enabled in Release builds. See [Android Requirements](#android-requirements) for configuration.
- **For iOS**: Ensure dSYM generation is enabled. See [iOS Requirements](#ios-requirements) for configuration.
- Verify symbol files are being generated using the commands in [Verifying Symbol File Generation](#verifying-symbol-file-generation)
- Manually specify the path: `<DatadogSymbolFilePath>/path/to/symbols</DatadogSymbolFilePath>`

### Git Metadata Warning

```
⚠ An error occurred while collecting git metadata.
⚠ Make sure the command is running within your git repository...
```

**Solutions**:

- Run the build from within a git repository, OR
- Disable git metadata: `<DatadogDisableGit>true</DatadogDisableGit>`

### Invalid API Key

```
Invalid API key for Datadog site us3.datadoghq.com
Please verify your DATADOG_API_KEY environment variable or DatadogApiKey property.
```

**Solutions**:
- Verify your API key is correct and has the necessary permissions
- Ensure the `DatadogSite` matches your Datadog organization's region
- Check that your API key is not expired or revoked
- For air-gapped environments, set `<DatadogSkipApiKeyValidation>true</DatadogSkipApiKeyValidation>`

### Missing Service or Version

If you see `ServiceName is required` or `Version is required`, you must provide these values:

```xml
<DatadogServiceName>my-app</DatadogServiceName>
<DatadogVersion>1.0.0</DatadogVersion>
```

These fields are always required for symbol uploads to work correctly.

### Upload Failures

If uploads fail with HTTP errors, use **verbose mode** to see the full request/response:

```bash
dotnet publish -c Release -f net9.0-android -p:DatadogVerbose=true
```

This will show:
- The exact URL being called
- All form fields being sent
- Complete HTTP response with error details

Common issues:
- **400 Bad Request**: Missing required fields (check verbose output for what's being sent)
- **403 Forbidden**: Invalid API key or wrong site configuration
- **404 Not Found**: Incorrect site value in `DatadogSite`

## Migration from CLI Wrapper

If you're migrating from the `Datadog.MAUI.Symbols` CLI wrapper plugin:

1. **Remove old package**: `dotnet remove package Datadog.MAUI.Symbols`
2. **Install new package**: `dotnet add package Datadog.MAUI.SymbolsUpload`
3. **Update properties**: Rename properties to match new naming
4. **Remove Node.js dependency**: No longer needed!

## Documentation

- **[Development Guide](DEVELOPMENT.md)** - Building, testing, and contributing
- **[Architecture](docs/ARCHITECTURE.md)** - Technical architecture and API details
- **[Symbol Files Guide](docs/SYMBOL_FILES.md)** - Enabling, verifying, and manually uploading symbol files
- **[Build vs Publish](docs/BUILD_VS_PUBLISH.md)** - Understanding when to use `dotnet build` vs `dotnet publish`
- **[Sample App](samples/MauiSampleApp/README.md)** - Example MAUI application

## License

Apache-2.0
