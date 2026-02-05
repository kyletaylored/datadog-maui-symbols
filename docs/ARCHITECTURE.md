# Architecture

This document describes the technical architecture of the Datadog.MAUI.Symbols plugin.

## Overview

```
┌─────────────────────────────────────┐
│  MSBuild Tasks (C#)                 │
│  ├── UploadSymbolsTask              │
│  └── GenerateBuildIdTask            │
├─────────────────────────────────────┤
│  Core Components                    │
│  ├── DatadogApiClient (gzip)        │
│  ├── SymbolFileLocator              │
│  ├── MetadataBuilder                │
│  └── GitMetadataCollector           │
├─────────────────────────────────────┤
│  HTTP → Datadog API                 │
│  POST sourcemap-intake.<site>       │
│       /api/v2/srcmap                │
└─────────────────────────────────────┘
```

## Components

### MSBuild Tasks

**UploadSymbolsTask** - Main task for uploading symbol files
- Validates configuration (API key, service name/version or build ID)
- Locates symbol files automatically or uses provided path
- Collects git metadata if enabled
- Builds upload request with metadata
- Calls DatadogApiClient to perform upload
- Handles dry-run mode for testing
- Provides user-friendly progress and error messages

**GenerateBuildIdTask** - Generates build ID for Android
- Calculates SHA256 hash of APK if available
- Falls back to configuration hash (version + variant + version code + timestamp)
- Returns 16-character hex string as build ID

### Core Components

**DatadogApiClient** - HTTP client for Datadog API
- Sends gzip-compressed multipart requests
- Builds multipart form data with event metadata, symbol files, and git repository data
- Handles authentication via DD-API-KEY header
- Returns structured UploadResult with success/failure details

**SymbolFileLocator** - Locates symbol files in project structure
- **Android**: Finds ProGuard/R8 mapping files in `build/outputs/mapping/{variant}/mapping.txt`
- **iOS**: Finds `.dSYM` bundles in build output directories
- **Flutter**: Finds symbol files in `build/app/outputs/flutter-apk/*.symbols`
- Supports variant-specific searches

**MetadataBuilder** - Builds and validates metadata
- Validates required fields (service/version or build_id)
- Converts SymbolMetadata to API event payload
- Handles optional fields (variant, arch, git data)
- Ensures type and cli_version are always included

**GitMetadataCollector** - Collects git repository information
- Executes git commands to collect commit SHA, remote URL, and tracked files
- Handles errors gracefully (returns null if not in git repo)
- Formats data for Datadog's repository payload format

### Models

**SymbolMetadata** - Describes symbol file being uploaded
- Platform (android, ios, flutter)
- Type (jvm_mapping_file, ios_dsym, flutter_symbol_file)
- Service name, version, variant (or build_id as alternative)
- Architecture (for iOS)
- CLI version for tracking

**GitMetadata** - Git repository information
- Commit hash (SHA)
- Remote URL
- List of tracked files
- Converts to Datadog repository payload format

**UploadRequest** - Complete upload request data
- Symbol metadata
- File path
- Git metadata (optional)
- API key
- Datadog site

**UploadResult** - Result of upload operation
- Success flag
- Error message (if failed)
- HTTP status code (if applicable)

## API Implementation

The plugin communicates with Datadog's sourcemap intake API using the following specification:

### Endpoint

```
POST https://sourcemap-intake.{site}/api/v2/srcmap
```

Where `{site}` is typically `datadoghq.com` but can be customized for different Datadog regions (e.g., `us5.datadoghq.com`, `datadoghq.eu`).

### Request Format

**Method**: POST
**Content-Type**: multipart/form-data
**Content-Encoding**: gzip

**Headers**:
- `DD-API-KEY`: Datadog API key for authentication
- `DD-EVP-ORIGIN`: Client identifier (`datadog-maui-symbols`)
- `DD-EVP-ORIGIN-VERSION`: Plugin version number

**Multipart Parts**:

1. **event** (application/json) - Symbol metadata
   ```json
   {
     "cli_version": "datadog-maui-symbols/0.1.0",
     "type": "jvm_mapping_file",
     "service": "my-app",
     "version": "1.0.0",
     "variant": "release",
     "platform": "android"
   }
   ```

   Or with build_id:
   ```json
   {
     "cli_version": "datadog-maui-symbols/0.1.0",
     "type": "jvm_mapping_file",
     "build_id": "abc123def456",
     "platform": "android"
   }
   ```

2. **jvm_mapping_file** / **dsym** / **flutter_symbol_file** - Symbol file content
   - Field name depends on symbol type
   - Contains raw file bytes
   - Includes filename in Content-Disposition header

3. **repository** (application/json, optional) - Git metadata
   ```json
   {
     "data": [
       {
         "files": ["path/to/file1.cs", "path/to/file2.cs"],
         "hash": "abc123def456789",
         "repository_url": "https://github.com/user/repo.git"
       }
     ]
   }
   ```

### Compression

The entire multipart request body is compressed with gzip before sending:

1. Build multipart form-data content
2. Read content as byte array
3. Compress with GZipStream (CompressionLevel.Optimal)
4. Set `Content-Encoding: gzip` header
5. Send compressed bytes

This significantly reduces upload time and bandwidth usage, especially for large symbol files.

### Response

**Success**: 202 Accepted
**Body**: `{}`

The response body is typically an empty JSON object. The 202 status indicates the upload was accepted and will be processed asynchronously.

Symbol files are processed server-side and become available for symbolication within approximately 5 minutes.

**Error**: 4xx or 5xx status code
**Body**: JSON with error details

## Execution Flow

### Automatic Upload (Android)

1. User builds MAUI project: `dotnet build -c Release -f net9.0-android`
2. MSBuild executes `BuildAndroid` target
3. After build completes, `DatadogUploadAndroidSymbols` target runs
4. GenerateBuildIdTask generates build ID (if not provided)
5. UploadSymbolsTask executes:
   - Validates API key and configuration
   - Locates ProGuard/R8 mapping file
   - Collects git metadata
   - Builds upload request
   - Calls DatadogApiClient
6. DatadogApiClient:
   - Builds multipart content
   - Compresses with gzip
   - Sends POST request
   - Returns result
7. Task logs success/failure message

### Automatic Upload (iOS)

Similar flow but:
- Triggers after `Build` target (not `BuildAndroid`)
- Locates `.dSYM` bundles instead of mapping files
- No build ID generation (iOS uses service/version/variant)
- Includes architecture in metadata (arm64/x64)

### Manual Upload

User explicitly invokes upload target:

```bash
dotnet build -t:DatadogUploadSymbols \
  -p:DatadogApiKey=xxx \
  -p:DatadogServiceName=my-app \
  -p:DatadogVersion=1.0.0 \
  -p:DatadogPlatform=android \
  -p:DatadogSymbolType=jvm_mapping_file \
  -p:DatadogSymbolFilePath=/path/to/mapping.txt
```

Execution proceeds directly to UploadSymbolsTask with provided parameters.

### Dry Run Mode

When `DatadogSymbolUploadDryRun=true`:

1. All validation and preparation steps execute normally
2. Symbol file is located and validated
3. Git metadata is collected
4. Upload request is built
5. **Upload is skipped** - no HTTP request sent
6. Task logs detailed information about what would be uploaded:
   - File path and size
   - Target endpoint
   - Metadata values
   - Git information
7. Task returns success

This allows testing configuration without uploading files or consuming API quota.

## MSBuild Integration

### Targets File Structure

The plugin provides `.targets` files that MSBuild automatically imports from the NuGet package:

```xml
<Project>
  <!-- Task declarations -->
  <UsingTask TaskName="UploadSymbolsTask" AssemblyFile="..." />
  <UsingTask TaskName="GenerateBuildIdTask" AssemblyFile="..." />

  <!-- Platform-specific targets -->
  <Target Name="DatadogUploadAndroidSymbols"
          AfterTargets="BuildAndroid"
          Condition="'$(DatadogSymbolUploadEnabled)' == 'true' AND '$(TargetPlatformIdentifier)' == 'android'">
    <!-- Upload logic -->
  </Target>

  <Target Name="DatadogUploadIosSymbols"
          AfterTargets="Build"
          Condition="'$(DatadogSymbolUploadEnabled)' == 'true' AND '$(TargetPlatformIdentifier)' == 'ios'">
    <!-- Upload logic -->
  </Target>

  <!-- Manual target -->
  <Target Name="DatadogUploadSymbols">
    <!-- Manual upload logic -->
  </Target>
</Project>
```

### Property Flow

Properties can be set in multiple ways (in order of precedence):

1. Command line: `-p:DatadogApiKey=xxx`
2. Environment variables: `DATADOG_API_KEY` → `DatadogApiKey`
3. Project file: `<DatadogApiKey>xxx</DatadogApiKey>`
4. Defaults in targets file

The MSBuild targets pass properties to tasks:

```xml
<UploadSymbolsTask
  ApiKey="$(DatadogApiKey)"
  ServiceName="$(DatadogServiceName)"
  ... />
```

## Design Decisions

### Why .NET Standard 2.0?

MSBuild tasks must target .NET Standard 2.0 for maximum compatibility with different build environments and MSBuild versions.

### Why Gzip Compression?

ProGuard mapping files can be large (10+ MB). Gzip compression:
- Reduces upload time significantly
- Saves bandwidth
- Is supported by Datadog's API
- Has minimal CPU overhead

### Why Optional Build ID?

Android's native Gradle plugin supports build_id as an alternative to service/version/variant. This provides:
- Unique identifier for each build
- Automatic generation from APK hash
- Compatibility with existing workflows

### Why Git Metadata?

Git metadata enables powerful features in Datadog:
- Link errors to specific commits
- Show which files were involved
- Track issues across versions
- Integrate with source control

### Why Separate Locator Component?

Symbol files are stored in different locations per platform:
- Android: `build/outputs/mapping/{variant}/mapping.txt`
- iOS: Various `.dSYM` bundle locations
- Flutter: `build/app/outputs/flutter-apk/*.symbols`

SymbolFileLocator encapsulates platform-specific location logic and can be easily extended for new platforms or build configurations.

## Security Considerations

### API Key Handling

- API keys are never logged or included in error messages
- Recommended to use environment variables rather than hardcoding in project files
- Keys are passed securely via HTTP headers (over HTTPS)

### File Validation

- Symbol files are validated to exist before upload
- File size and type are checked
- Path traversal is prevented by using absolute paths

### Error Messages

- Error messages exclude sensitive information (API keys, full file paths)
- Stack traces are sanitized before logging
- HTTP responses are truncated if too large

## Performance Optimization

### Async/Await

HTTP operations use async/await to avoid blocking build process:
```csharp
public async Task<UploadResult> UploadAsync(UploadRequest request)
{
    var response = await _httpClient.SendAsync(requestMessage);
    // ...
}
```

### Efficient File Reading

Symbol files are read once as byte arrays and reused:
```csharp
var fileContent = new ByteArrayContent(File.ReadAllBytes(request.FilePath));
```

### Minimal Dependencies

Only essential NuGet packages are referenced:
- Microsoft.Build.Utilities.Core (required for MSBuild tasks)
- System.Text.Json (lightweight JSON serialization)

No additional dependencies reduces package size and build time.

## Testing Strategy

### Unit Tests

Focus on individual components:
- MetadataBuilder validation logic
- SymbolFileLocator path resolution
- Model serialization
- Isolated from I/O and network

### Integration Tests

Test complete workflows:
- Real file system operations
- Git metadata collection in actual repos
- End-to-end upload flow (without API calls)
- Dry-run mode execution

### Manual Testing

Sample MAUI app for testing:
- Real build integration
- Actual symbol file generation
- Visual verification of runtime build info
- Real API uploads (optional)
