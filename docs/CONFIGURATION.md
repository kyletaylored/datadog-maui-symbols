# Configuration Guide

Complete reference for configuring the Datadog.MAUI.Symbols plugin.

## Configuration Methods

You can configure the plugin in multiple ways. Choose the method that best fits your workflow:

### 1. Environment Variables (Recommended for API Keys)

Best for keeping secrets out of source control:

```bash
# Set once in your shell or CI/CD environment
export DATADOG_API_KEY=your_api_key_here
export DATADOG_SITE=us3.datadoghq.com

# Then publish normally
dotnet publish -c Release -f net9.0-android
```

### 2. Command Line Properties

Pass properties directly via `-p:` flag:

```bash
dotnet publish -c Release -f net9.0-android \
  -p:DatadogApiKey=$DATADOG_API_KEY \
  -p:DatadogSite=us3.datadoghq.com \
  -p:DatadogServiceName=my-app
```

### 3. Project File (.csproj)

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

### 4. Directory.Build.props (Multi-Project)

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

## Required Settings

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

## Optional Settings

```xml
<PropertyGroup>
  <!-- Build ID - supplemental identifier for Android -->
  <DatadogBuildId></DatadogBuildId>

  <!-- Build variant/flavor - defaults to $(Configuration) -->
  <DatadogVariant>release</DatadogVariant>

  <!-- Datadog site - defaults to datadoghq.com -->
  <DatadogSite>us3.datadoghq.com</DatadogSite>

  <!-- Disable git operations - prevents git invocation and repository metadata - defaults to false -->
  <DatadogDisableGit>false</DatadogDisableGit>

  <!-- Custom repository URL - overrides git-detected remote URL -->
  <DatadogRepositoryUrl>https://github.com/my-company/my-project</DatadogRepositoryUrl>

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

## Datadog Site Configuration

The `DatadogSite` property specifies which Datadog region to upload symbols to. This must match the region where your Datadog organization is hosted.

**Valid site values:**

| Region  | Site Value          | Description          |
| ------- | ------------------- | -------------------- |
| US1     | `datadoghq.com`     | Default US region    |
| US3     | `us3.datadoghq.com` | US3 region           |
| US5     | `us5.datadoghq.com` | US5 region           |
| EU1     | `datadoghq.eu`      | European region      |
| AP1     | `ap1.datadoghq.com` | Asia-Pacific region  |
| US1-FED | `ddog-gov.com`      | US government region |

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

## Git Integration Control

By default, the plugin automatically collects git metadata (commit SHA, remote URL, tracked files) to enhance crash symbolication and link errors to specific commits. You can control this behavior:

**Disable Git Entirely**:

```xml
<PropertyGroup>
  <!-- Prevents git invocation and repository metadata collection -->
  <DatadogDisableGit>true</DatadogDisableGit>
</PropertyGroup>
```

**Override Repository URL**:

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

## API Key Validation

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

## Example Configurations

### Basic Android App

```xml
<PropertyGroup>
  <DatadogApiKey Condition="'$(DatadogApiKey)' == ''">$(DATADOG_API_KEY)</DatadogApiKey>
  <DatadogServiceName>my-android-app</DatadogServiceName>
  <DatadogVersion>1.2.3</DatadogVersion>
</PropertyGroup>
```

### Multi-Platform MAUI App

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net9.0-android'">
  <DatadogApiKey Condition="'$(DatadogApiKey)' == ''">$(DATADOG_API_KEY)</DatadogApiKey>
  <DatadogServiceName>my-maui-app-android</DatadogServiceName>
  <DatadogVersion>$(ApplicationVersion)</DatadogVersion>
</PropertyGroup>

<PropertyGroup Condition="'$(TargetFramework)' == 'net9.0-ios'">
  <DatadogApiKey Condition="'$(DatadogApiKey)' == ''">$(DATADOG_API_KEY)</DatadogApiKey>
  <DatadogServiceName>my-maui-app-ios</DatadogServiceName>
  <DatadogVersion>$(ApplicationVersion)</DatadogVersion>
</PropertyGroup>
```

### Using Build ID (Android)

```xml
<PropertyGroup>
  <DatadogApiKey Condition="'$(DatadogApiKey)' == ''">$(DATADOG_API_KEY)</DatadogApiKey>
  <DatadogServiceName>my-mobile-app</DatadogServiceName>
  <DatadogVersion>1.0.0</DatadogVersion>
  <!-- Build ID will be auto-generated from APK hash or configuration -->
  <DatadogBuildId></DatadogBuildId>
</PropertyGroup>
```

**Note**: `BuildId` is supplemental and does not replace `ServiceName`/`Version`.
