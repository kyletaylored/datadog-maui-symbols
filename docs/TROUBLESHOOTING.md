# Troubleshooting Guide

Common issues and their solutions when using the Datadog.MAUI.Symbols plugin.

## Upload is Skipped

**Symptoms:**

```
⚠️  Symbol upload skipped.
To enable symbol upload, ensure DatadogApiKey is set via environment variable or project property.
```

**Solution**: Set the `DATADOG_API_KEY` environment variable or `DatadogApiKey` MSBuild property.

```bash
# Via environment variable
export DATADOG_API_KEY=your_api_key_here
dotnet publish -c Release -f net9.0-android

# Via command line
dotnet publish -c Release -f net9.0-android -p:DatadogApiKey=your_api_key_here
```

## Symbol File Not Found

**Symptoms:**

```
Could not locate symbol file for platform 'android'
```

**Solutions:**

### For Android

Ensure R8/ProGuard is enabled in Release builds:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release' AND $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">
  <AndroidLinkMode>Full</AndroidLinkMode>
  <AndroidLinkTool>r8</AndroidLinkTool>
</PropertyGroup>
```

**Verify mapping.txt is generated:**

```bash
find . -name "mapping.txt" -type f
# Should show: ./obj/Release/net9.0-android/lp/*/mapping.txt
```

### For iOS

Ensure dSYM generation is enabled:

```xml
<PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
  <MtouchDebugSymbols>true</MtouchDebugSymbols>
  <GenerateDsymBundle>true</GenerateDsymBundle>
</PropertyGroup>
```

**Verify .dSYM bundles are generated:**

```bash
find . -name "*.dSYM" -type d
# Should show: ./bin/Release/net9.0-ios/*/*.app.dSYM
```

### Manual Path Override

If symbol files exist but aren't auto-detected:

```xml
<PropertyGroup>
  <DatadogSymbolFilePath>/path/to/your/symbols</DatadogSymbolFilePath>
</PropertyGroup>
```

## Git Metadata Warning

**Symptoms:**

```
⚠ An error occurred while collecting git metadata.
⚠ Make sure the command is running within your git repository...
```

**Solutions:**

1. **Run from git repository**: Ensure you're building from within a cloned git repository
2. **Disable git metadata**:
   ```xml
   <PropertyGroup>
     <DatadogDisableGit>true</DatadogDisableGit>
   </PropertyGroup>
   ```

**Note**: This warning doesn't prevent uploads - git metadata is optional.

## Invalid API Key

**Symptoms:**

```
Invalid API key for Datadog site us3.datadoghq.com
Please verify your DATADOG_API_KEY environment variable or DatadogApiKey property.
```

**Solutions:**

1. **Verify API key format**: Check that your API key is correct (32-character hex string)
2. **Check site configuration**: Ensure `DatadogSite` matches your Datadog organization's region
3. **Verify permissions**: Ensure the API key has symbol upload permissions
4. **Check expiration**: API keys can expire - verify in Datadog UI

**For air-gapped environments**, skip validation:

```xml
<PropertyGroup>
  <DatadogSkipApiKeyValidation>true</DatadogSkipApiKeyValidation>
</PropertyGroup>
```

## Missing Service or Version

**Symptoms:**

```
ServiceName is required
```

or

```
Version is required
```

**Solution**: Both fields are always required:

```xml
<PropertyGroup>
  <DatadogServiceName>my-app</DatadogServiceName>
  <DatadogVersion>1.0.0</DatadogVersion>
</PropertyGroup>
```

**Note**: `BuildId` does not replace these fields - it's supplemental.

## Upload Failures (HTTP Errors)

**Symptoms:**

```
Symbol upload failed: HTTP 400 Bad Request
```

**Solution**: Enable verbose mode to see full request/response details:

```bash
dotnet publish -c Release -f net9.0-android -p:DatadogVerbose=true
```

### Common HTTP Errors

#### 400 Bad Request

- **Cause**: Missing required fields or invalid format
- **Fix**: Check verbose output to see which fields are being sent
- **Verify**: Ensure `service`, `version`, and `type` are all present

#### 403 Forbidden

- **Cause**: Invalid API key or wrong site configuration
- **Fix**: Verify your API key and ensure `DatadogSite` matches your org's region

#### 404 Not Found

- **Cause**: Incorrect `DatadogSite` value
- **Fix**: Verify the site value matches one of the valid options:
  - `datadoghq.com` (US1)
  - `us3.datadoghq.com` (US3)
  - `us5.datadoghq.com` (US5)
  - `datadoghq.eu` (EU1)
  - `ap1.datadoghq.com` (AP1)
  - `ddog-gov.com` (US1-FED)

#### 429 Too Many Requests

- **Cause**: Rate limiting
- **Fix**: Reduce upload frequency or contact Datadog support

## Build vs Publish Confusion

**Symptoms:**

```
Symbol file not found
```

even though R8/dSYM is enabled.

**Solution**: Use `dotnet publish` (not `dotnet build`) for Release builds:

```bash
# ❌ Wrong
dotnet build -c Release -f net9.0-android

# ✅ Correct
dotnet publish -c Release -f net9.0-android
```

See [BUILD_VS_PUBLISH.md](BUILD_VS_PUBLISH.md) for details.

## Plugin Not Running

**Symptoms:**
No Datadog output appears in build logs.

**Possible Causes:**

1. **Debug build** (default): Symbol upload is skipped for Debug configuration

   ```xml
   <!-- Enable for Debug builds -->
   <DatadogSymbolUploadSkip Condition="'$(Configuration)' == 'Debug'">false</DatadogSymbolUploadSkip>
   ```

2. **Plugin disabled**:

   ```xml
   <!-- Ensure this is true or omitted -->
   <DatadogSymbolUploadEnabled>true</DatadogSymbolUploadEnabled>
   ```

3. **Package not installed**: Verify package reference:
   ```xml
   <PackageReference Include="kyletaylored.Datadog.MAUI.Symbols" Version="0.1.0" />
   ```

## Verbose Mode Shows Masked API Key

**Symptoms:**
Verbose output shows `DD-API-KEY: ***masked***`

**This is expected behavior**: API keys are always masked in logs for security, even in verbose mode.

## Network/Proxy Issues

**Symptoms:**

```
Connection timeout or network error
```

**Solutions:**

1. **Check firewall/proxy**: Ensure outbound HTTPS to `sourcemap-intake.<site>` is allowed
2. **Corporate proxy**: Configure .NET HTTP proxy settings
3. **Air-gapped environment**: Use `DatadogSkipApiKeyValidation=true` and ensure network access to Datadog endpoints

## Large Symbol Files

**Symptoms:**
Upload times out or fails with large mapping files (>100MB)

**Solutions:**

1. **ProGuard optimization** (Android):
   - Ensure you're only including necessary mappings
   - Consider ProGuard configuration to reduce mapping size

2. **Check compression**: The plugin automatically gzips uploads - verify this in verbose mode

3. **Network timeout**: Increase timeout if needed (requires custom build)

## Getting Help

If you're still experiencing issues:

1. **Enable verbose mode**: `-p:DatadogVerbose=true`
2. **Collect logs**: Save complete build output
3. **Verify requirements**:
   - Symbol files are being generated
   - API key is valid
   - Site configuration is correct
4. **Create an issue**: [GitHub Issues](https://github.com/kyletaylored/datadog-maui-symbols/issues) with:
   - Full build command
   - Verbose output (with API key masked)
   - Platform (Android/iOS)
   - .NET version
