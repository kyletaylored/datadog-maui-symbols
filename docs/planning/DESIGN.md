# Datadog.MAUI.Symbols - Native .NET Plugin Design

> **📋 Historical Document**: This is the original design document created during the planning phase. For current documentation, see [README.md](../../README.md). Some implementation details may have evolved during development.

## Executive Summary

**Current State**: The `Datadog.MAUI.Symbols` plugin wraps the `datadog-ci` CLI via `npx`, requiring Node.js/npm as a dependency.

**Proposed Solution**: Build a native .NET plugin (`Datadog.MAUI.Symbols`) that directly calls the Datadog Symbols Intake API, eliminating the Node.js dependency and providing better integration with the .NET ecosystem.

## Rationale

### Why Native .NET?

1. **Eliminate External Dependencies**
   - No Node.js/npm/npx required
   - Reduced installation complexity
   - Faster execution (no process spawning overhead)

2. **Better MSBuild Integration**
   - Direct access to MSBuild properties and item groups
   - Native error handling and logging
   - Proper async/await support

3. **Improved Reliability**
   - No cross-process communication failures
   - Consistent behavior across platforms
   - Better error messages and debugging

4. **Feature Parity + Extensions**
   - Implement build_id support immediately
   - Add .NET-specific features (e.g., embedded resources)
   - Custom retry logic for enterprise scenarios

5. **Performance**
   - Single process execution
   - Reuse HTTP connections across uploads
   - Parallel uploads with .NET Task library

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│  Datadog.MAUI.Symbols.csproj (NuGet Package)         │
├─────────────────────────────────────────────────────────────┤
│  MSBuild Tasks (C#)                                         │
│  ├── UploadSymbolsTask.cs       (Main task)                │
│  ├── GenerateBuildIdTask.cs     (Build ID generation)      │
│  └── ValidateConfigTask.cs      (Pre-flight checks)        │
├─────────────────────────────────────────────────────────────┤
│  Core Logic (C#)                                            │
│  ├── DatadogApiClient.cs        (HTTP client wrapper)      │
│  ├── SymbolFileLocator.cs       (Find symbols)             │
│  ├── MetadataBuilder.cs         (Build JSON payloads)      │
│  ├── MultipartUploader.cs       (Multipart form-data)      │
│  └── GitMetadataCollector.cs    (Git info extraction)      │
├─────────────────────────────────────────────────────────────┤
│  Models (C#)                                                │
│  ├── UploadRequest.cs                                       │
│  ├── SymbolMetadata.cs                                      │
│  └── UploadResult.cs                                        │
├─────────────────────────────────────────────────────────────┤
│  MSBuild Targets                                            │
│  └── build/Datadog.MAUI.Symbols.targets              │
└─────────────────────────────────────────────────────────────┘
```

## Technical Specifications

### Target Framework
- **Primary**: `netstandard2.0` (maximum compatibility with MSBuild)
- **Alternative**: Multi-target `netstandard2.0;net6.0;net8.0` for modern optimizations

### Dependencies
```xml
<ItemGroup>
  <!-- MSBuild integration -->
  <PackageReference Include="Microsoft.Build.Framework" Version="17.0.0" />
  <PackageReference Include="Microsoft.Build.Utilities.Core" Version="17.0.0" />

  <!-- HTTP client -->
  <PackageReference Include="System.Net.Http" Version="4.3.4" />

  <!-- JSON serialization -->
  <PackageReference Include="System.Text.Json" Version="8.0.0" Condition="'$(TargetFramework)' != 'netstandard2.0'" />
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" Condition="'$(TargetFramework)' == 'netstandard2.0'" />

  <!-- Multipart form-data -->
  <PackageReference Include="System.Net.Http.Formatting" Version="5.2.9" />
</ItemGroup>
```

## Implementation Plan

### Phase 1: Core API Client (Week 1)

#### 1.1 DatadogApiClient.cs

**Purpose**: Handles all HTTP communication with Datadog

```csharp
public class DatadogApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _site;

    public DatadogApiClient(string apiKey, string site = "datadoghq.com")
    {
        _apiKey = apiKey;
        _site = site;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"https://sourcemap-intake.{site}"),
            Timeout = TimeSpan.FromMinutes(10)
        };

        // Set headers
        _httpClient.DefaultRequestHeaders.Add("DD-API-KEY", apiKey);
        _httpClient.DefaultRequestHeaders.Add("DD-EVP-ORIGIN", "datadog-ci_maui-symbols");
        _httpClient.DefaultRequestHeaders.Add("DD-EVP-ORIGIN-VERSION", GetAssemblyVersion());
    }

    public async Task<UploadResult> UploadSymbolsAsync(
        SymbolMetadata metadata,
        string filePath,
        GitMetadata gitMetadata = null,
        CancellationToken cancellationToken = default)
    {
        using var content = CreateMultipartContent(metadata, filePath, gitMetadata);

        // Retry logic with exponential backoff
        return await RetryAsync(
            async () => await UploadWithRetryAsync(content, cancellationToken),
            maxRetries: 5,
            cancellationToken);
    }

    private MultipartFormDataContent CreateMultipartContent(
        SymbolMetadata metadata,
        string filePath,
        GitMetadata gitMetadata)
    {
        var content = new MultipartFormDataContent();

        // Add event metadata (JSON)
        var metadataJson = SerializeMetadata(metadata);
        content.Add(new StringContent(metadataJson, Encoding.UTF8, "application/json"),
                   "event", "event");

        // Add symbol file
        var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
        var fieldName = GetFieldName(metadata.Type);
        var fileName = GetFileName(metadata.Type);
        content.Add(fileContent, fieldName, fileName);

        // Add git metadata (optional)
        if (gitMetadata != null)
        {
            var repoJson = SerializeGitMetadata(gitMetadata);
            content.Add(new StringContent(repoJson, Encoding.UTF8, "application/json"),
                       "repository", "repository");
        }

        return content;
    }

    private async Task<UploadResult> UploadWithRetryAsync(
        MultipartFormDataContent content,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(
            "/api/v2/srcmap/v1/input",
            content,
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return UploadResult.Success();

        // Handle specific error codes
        var statusCode = (int)response.StatusCode;
        if (statusCode == 400 || statusCode == 403 || statusCode == 413)
            return UploadResult.Failure($"Non-retryable error: {response.ReasonPhrase}");

        throw new HttpRequestException($"Upload failed: {response.ReasonPhrase}");
    }
}
```

#### 1.2 MetadataBuilder.cs

**Purpose**: Constructs metadata JSON payloads

```csharp
public class MetadataBuilder
{
    public SymbolMetadata BuildAndroidMetadata(
        string serviceName,
        string version,
        string flavor,
        string buildId = null,
        GitInfo gitInfo = null)
    {
        var metadata = new SymbolMetadata
        {
            CliVersion = GetAssemblyVersion(),
            Type = "jvm_mapping_file"
        };

        // Conditional fields
        if (!string.IsNullOrEmpty(buildId))
        {
            metadata.BuildId = buildId;
        }
        else
        {
            metadata.Service = serviceName;
            metadata.Version = SanitizeVersion(version);
            metadata.Variant = flavor;
        }

        // Git metadata
        if (gitInfo != null)
        {
            metadata.GitCommitSha = gitInfo.CommitSha;
            metadata.GitRepositoryUrl = gitInfo.RepositoryUrl;
        }

        return metadata;
    }

    public SymbolMetadata BuildiOSMetadata(
        string serviceName,
        string version,
        string flavor,
        GitInfo gitInfo = null)
    {
        // iOS doesn't support build_id (yet)
        return new SymbolMetadata
        {
            CliVersion = GetAssemblyVersion(),
            Type = "ios_dsym",
            Service = serviceName,
            Version = SanitizeVersion(version),
            Variant = flavor,
            GitCommitSha = gitInfo?.CommitSha,
            GitRepositoryUrl = gitInfo?.RepositoryUrl
        };
    }

    private string SanitizeVersion(string version)
    {
        // Replace + with - (e.g., "1.2.3+456" -> "1.2.3-456")
        return version?.Replace('+', '-');
    }
}
```

### Phase 2: Symbol File Discovery (Week 1)

#### 2.1 SymbolFileLocator.cs

**Purpose**: Finds symbol files in standard and custom locations

```csharp
public class SymbolFileLocator
{
    private readonly ILogger _logger;

    public string FindAndroidMapping(
        string projectDir,
        string outputPath,
        string intermediateOutputPath,
        string targetFramework,
        string configuration,
        string customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
            return customPath;

        var searchPaths = new[]
        {
            Path.Combine(outputPath, "mapping.txt"),
            Path.Combine(Path.GetDirectoryName(outputPath), "mapping.txt"),
            Path.Combine(intermediateOutputPath, "mapping.txt"),
            Path.Combine(projectDir, "obj", configuration, targetFramework, "android-arm64", "mapping.txt"),
            Path.Combine(projectDir, "obj", configuration, targetFramework, "android-x86_64", "mapping.txt")
        };

        foreach (var path in searchPaths)
        {
            _logger.LogDebug($"Searching for mapping.txt at: {path}");
            if (File.Exists(path))
            {
                _logger.LogInformation($"Found mapping.txt at: {path}");
                return path;
            }
        }

        _logger.LogWarning("Android mapping.txt not found in any standard location");
        return null;
    }

    public string FindiOSDsym(
        string outputPath,
        string appBundleDir,
        string assemblyName,
        string customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath) && Directory.Exists(customPath))
            return customPath;

        var searchPaths = new[]
        {
            Path.Combine(outputPath, $"{assemblyName}.app.dSYM"),
            Path.Combine(Path.GetDirectoryName(outputPath), $"{assemblyName}.app.dSYM"),
            $"{appBundleDir}.dSYM"
        };

        foreach (var path in searchPaths)
        {
            _logger.LogDebug($"Searching for dSYM at: {path}");
            if (Directory.Exists(path))
            {
                _logger.LogInformation($"Found dSYM at: {path}");
                return path;
            }
        }

        _logger.LogWarning("iOS dSYM directory not found in any standard location");
        return null;
    }
}
```

### Phase 3: Git Metadata Collection (Week 2)

#### 3.1 GitMetadataCollector.cs

**Purpose**: Extracts git repository information

```csharp
public class GitMetadataCollector
{
    public GitMetadata CollectMetadata(string projectDirectory)
    {
        if (!IsGitRepository(projectDirectory))
            return null;

        return new GitMetadata
        {
            Hash = GetCommitSha(projectDirectory),
            Remote = GetRemoteUrl(projectDirectory),
            TrackedFiles = GetTrackedFiles(projectDirectory)
        };
    }

    private bool IsGitRepository(string directory)
    {
        var gitDir = Path.Combine(directory, ".git");
        return Directory.Exists(gitDir) || File.Exists(gitDir); // Support git worktrees
    }

    private string GetCommitSha(string directory)
    {
        return ExecuteGitCommand(directory, "rev-parse HEAD");
    }

    private string GetRemoteUrl(string directory)
    {
        return ExecuteGitCommand(directory, "config --get remote.origin.url");
    }

    private string[] GetTrackedFiles(string directory)
    {
        var output = ExecuteGitCommand(directory, "ls-files");
        return output?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private string ExecuteGitCommand(string workingDirectory, string arguments)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return process.ExitCode == 0 ? output.Trim() : null;
        }
        catch
        {
            return null; // Git not available or command failed
        }
    }
}
```

### Phase 4: MSBuild Task Implementation (Week 2)

#### 4.1 UploadSymbolsTask.cs

```csharp
public class UploadSymbolsTask : Task, ICancelableTask
{
    private CancellationTokenSource _cancellationTokenSource;

    // Required properties
    [Required]
    public string TargetPlatform { get; set; } // "android" or "ios"

    [Required]
    public string AppVersion { get; set; }

    // Optional properties with defaults
    public string ServiceName { get; set; }
    public string ServiceNameAndroid { get; set; }
    public string ServiceNameIOS { get; set; }
    public string ApiKey { get; set; }
    public string Site { get; set; } = "datadoghq.com";
    public bool DryRun { get; set; } = true;
    public string Flavor { get; set; } = "release";
    public string BuildId { get; set; }
    public bool DisableGit { get; set; } = false;
    public string CustomSymbolPath { get; set; }

    // MSBuild context properties
    public string ProjectDirectory { get; set; }
    public string OutputPath { get; set; }
    public string IntermediateOutputPath { get; set; }
    public string AssemblyName { get; set; }
    public string TargetFramework { get; set; }
    public string Configuration { get; set; }

    public override bool Execute()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            return ExecuteAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.LogError($"[Datadog] Symbol upload failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteAsync()
    {
        // Validate configuration
        if (!ValidateConfiguration())
            return false;

        // Resolve service name
        var serviceName = ResolveServiceName();
        if (string.IsNullOrEmpty(serviceName) && string.IsNullOrEmpty(BuildId))
        {
            Log.LogError("[Datadog] Either ServiceName or BuildId must be provided");
            return false;
        }

        // Get API key
        var apiKey = ResolveApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            Log.LogWarning("[Datadog] No API key found. Set DatadogSymbolsApiKey or DD_API_KEY environment variable.");
            return true; // Don't fail build
        }

        // Find symbol files
        var locator = new SymbolFileLocator(new MSBuildLogger(Log));
        var symbolPath = TargetPlatform.ToLower() == "android"
            ? locator.FindAndroidMapping(ProjectDirectory, OutputPath, IntermediateOutputPath,
                                        TargetFramework, Configuration, CustomSymbolPath)
            : locator.FindiOSDsym(OutputPath, null, AssemblyName, CustomSymbolPath);

        if (string.IsNullOrEmpty(symbolPath))
        {
            Log.LogWarning($"[Datadog] Symbol file not found for {TargetPlatform}");
            return true; // Don't fail build
        }

        if (DryRun)
        {
            Log.LogMessage(MessageImportance.High,
                $"[Datadog] DRY RUN: Would upload {symbolPath} for {serviceName} v{AppVersion}");
            return true;
        }

        // Collect git metadata
        GitMetadata gitMetadata = null;
        if (!DisableGit)
        {
            var gitCollector = new GitMetadataCollector();
            gitMetadata = gitCollector.CollectMetadata(ProjectDirectory);
        }

        // Build metadata
        var metadataBuilder = new MetadataBuilder();
        var metadata = TargetPlatform.ToLower() == "android"
            ? metadataBuilder.BuildAndroidMetadata(serviceName, AppVersion, Flavor, BuildId,
                                                   gitMetadata?.ToGitInfo())
            : metadataBuilder.BuildiOSMetadata(serviceName, AppVersion, Flavor,
                                              gitMetadata?.ToGitInfo());

        // Upload
        using var client = new DatadogApiClient(apiKey, Site);
        var result = await client.UploadSymbolsAsync(
            metadata,
            symbolPath,
            gitMetadata,
            _cancellationTokenSource.Token);

        if (result.Success)
        {
            Log.LogMessage(MessageImportance.High,
                $"[Datadog] Successfully uploaded symbols for {serviceName} v{AppVersion}");
            return true;
        }
        else
        {
            Log.LogWarning($"[Datadog] Upload failed: {result.ErrorMessage}");
            return true; // Don't fail build on upload errors
        }
    }

    private string ResolveServiceName()
    {
        if (TargetPlatform.ToLower() == "android" && !string.IsNullOrEmpty(ServiceNameAndroid))
            return ServiceNameAndroid;
        if (TargetPlatform.ToLower() == "ios" && !string.IsNullOrEmpty(ServiceNameIOS))
            return ServiceNameIOS;
        return ServiceName;
    }

    private string ResolveApiKey()
    {
        return ApiKey
               ?? Environment.GetEnvironmentVariable("DD_API_KEY")
               ?? Environment.GetEnvironmentVariable("DATADOG_API_KEY");
    }

    private bool ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(TargetPlatform))
        {
            Log.LogError("[Datadog] TargetPlatform is required");
            return false;
        }

        if (string.IsNullOrEmpty(AppVersion))
        {
            Log.LogError("[Datadog] AppVersion is required");
            return false;
        }

        return true;
    }

    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }
}
```

#### 4.2 GenerateBuildIdTask.cs

```csharp
public class GenerateBuildIdTask : Task
{
    [Output]
    public string BuildId { get; set; }

    public string OutputDirectory { get; set; }

    public override bool Execute()
    {
        // Generate 8-character build ID
        BuildId = Guid.NewGuid().ToString("N").Substring(0, 8);

        Log.LogMessage(MessageImportance.Low, $"[Datadog] Generated build ID: {BuildId}");

        // Generate C# file with build ID
        if (!string.IsNullOrEmpty(OutputDirectory))
        {
            GenerateBuildInfoFile(OutputDirectory, BuildId);
        }

        return true;
    }

    private void GenerateBuildInfoFile(string outputDir, string buildId)
    {
        var content = $@"// <auto-generated />
namespace Datadog.MAUI.Symbols
{{
    /// <summary>
    /// Build metadata generated at compile time.
    /// </summary>
    public static class DatadogBuildInfo
    {{
        /// <summary>
        /// Unique identifier for this build.
        /// </summary>
        public const string BuildId = ""{buildId}"";
    }}
}}
";

        Directory.CreateDirectory(outputDir);
        var filePath = Path.Combine(outputDir, "DatadogBuildInfo.g.cs");
        File.WriteAllText(filePath, content);

        Log.LogMessage(MessageImportance.Low, $"[Datadog] Generated build info at: {filePath}");
    }
}
```

### Phase 5: MSBuild Targets Integration (Week 3)

#### 5.1 Datadog.MAUI.Symbols.targets

```xml
<Project>
  <!-- Default property values -->
  <PropertyGroup>
    <DatadogSymbolsUploadEnabled Condition="'$(DatadogSymbolsUploadEnabled)' == ''">true</DatadogSymbolsUploadEnabled>
    <DatadogSymbolsUploadInDebug Condition="'$(DatadogSymbolsUploadInDebug)' == ''">false</DatadogSymbolsUploadInDebug>
    <DatadogSymbolsDryRun Condition="'$(DatadogSymbolsDryRun)' == ''">true</DatadogSymbolsDryRun>
    <DatadogSymbolsDisableGit Condition="'$(DatadogSymbolsDisableGit)' == ''">false</DatadogSymbolsDisableGit>
    <DatadogSymbolsSite Condition="'$(DatadogSymbolsSite)' == ''">datadoghq.com</DatadogSymbolsSite>
    <DatadogSymbolsFlavor Condition="'$(DatadogSymbolsFlavor)' == ''">release</DatadogSymbolsFlavor>
    <DatadogSymbolsFlavor Condition="'$(Configuration)' == 'Debug'">debug</DatadogSymbolsFlavor>
    <DatadogSymbolsAppVersion Condition="'$(DatadogSymbolsAppVersion)' == ''">$(ApplicationDisplayVersion)</DatadogSymbolsAppVersion>
    <DatadogSymbolsAppVersion Condition="'$(DatadogSymbolsAppVersion)' == ''">1.0.0</DatadogSymbolsAppVersion>
  </PropertyGroup>

  <!-- Import task assembly -->
  <UsingTask TaskName="Datadog.MAUI.Symbols.UploadSymbolsTask"
             AssemblyFile="$(MSBuildThisFileDirectory)\..\lib\netstandard2.0\Datadog.MAUI.Symbols.dll" />
  <UsingTask TaskName="Datadog.MAUI.Symbols.GenerateBuildIdTask"
             AssemblyFile="$(MSBuildThisFileDirectory)\..\lib\netstandard2.0\Datadog.MAUI.Symbols.dll" />

  <!-- Generate Build ID (before compilation) -->
  <Target Name="DatadogGenerateBuildId"
          BeforeTargets="CoreCompile"
          Condition="'$(DatadogSymbolsUploadEnabled)' == 'true' AND '$(DatadogSymbolsGenerateBuildId)' == 'true'">

    <GenerateBuildIdTask OutputDirectory="$(IntermediateOutputPath)">
      <Output TaskParameter="BuildId" PropertyName="DatadogSymbolsBuildId" />
    </GenerateBuildIdTask>

    <!-- Add generated file to compilation -->
    <ItemGroup>
      <Compile Include="$(IntermediateOutputPath)DatadogBuildInfo.g.cs" />
    </ItemGroup>
  </Target>

  <!-- Upload Symbols (after publish) -->
  <Target Name="DatadogUploadSymbols"
          AfterTargets="Publish"
          Condition="'$(DatadogSymbolsUploadEnabled)' == 'true' AND
                     ('$(Configuration)' == 'Release' OR '$(DatadogSymbolsUploadInDebug)' == 'true')">

    <!-- Android Upload -->
    <UploadSymbolsTask Condition="'$(TargetPlatformIdentifier)' == 'android'"
                       TargetPlatform="android"
                       ServiceName="$(DatadogSymbolsServiceName)"
                       ServiceNameAndroid="$(DatadogSymbolsServiceNameAndroid)"
                       AppVersion="$(DatadogSymbolsAppVersion)"
                       ApiKey="$(DatadogSymbolsApiKey)"
                       Site="$(DatadogSymbolsSite)"
                       DryRun="$(DatadogSymbolsDryRun)"
                       Flavor="$(DatadogSymbolsFlavor)"
                       BuildId="$(DatadogSymbolsBuildId)"
                       DisableGit="$(DatadogSymbolsDisableGit)"
                       CustomSymbolPath="$(DatadogSymbolsAndroidMappingPath)"
                       ProjectDirectory="$(MSBuildProjectDirectory)"
                       OutputPath="$(OutputPath)"
                       IntermediateOutputPath="$(IntermediateOutputPath)"
                       AssemblyName="$(AssemblyName)"
                       TargetFramework="$(TargetFramework)"
                       Configuration="$(Configuration)"
                       ContinueOnError="true" />

    <!-- iOS Upload -->
    <UploadSymbolsTask Condition="'$(TargetPlatformIdentifier)' == 'ios'"
                       TargetPlatform="ios"
                       ServiceName="$(DatadogSymbolsServiceName)"
                       ServiceNameIOS="$(DatadogSymbolsServiceNameIOS)"
                       AppVersion="$(DatadogSymbolsAppVersion)"
                       ApiKey="$(DatadogSymbolsApiKey)"
                       Site="$(DatadogSymbolsSite)"
                       DryRun="$(DatadogSymbolsDryRun)"
                       Flavor="$(DatadogSymbolsFlavor)"
                       DisableGit="$(DatadogSymbolsDisableGit)"
                       CustomSymbolPath="$(DatadogSymbolsiOSDsymPath)"
                       ProjectDirectory="$(MSBuildProjectDirectory)"
                       OutputPath="$(OutputPath)"
                       IntermediateOutputPath="$(IntermediateOutputPath)"
                       AssemblyName="$(AssemblyName)"
                       TargetFramework="$(TargetFramework)"
                       Configuration="$(Configuration)"
                       ContinueOnError="true" />
  </Target>
</Project>
```

### Phase 6: Testing & Documentation (Week 3)

#### 6.1 Unit Tests

Create `Datadog.MAUI.Symbols.Tests` project:

```csharp
[TestClass]
public class MetadataBuilderTests
{
    [TestMethod]
    public void BuildAndroidMetadata_WithBuildId_ExcludesServiceVersion()
    {
        var builder = new MetadataBuilder();
        var metadata = builder.BuildAndroidMetadata(
            serviceName: "test.app",
            version: "1.0.0",
            flavor: "release",
            buildId: "abc123");

        Assert.AreEqual("abc123", metadata.BuildId);
        Assert.IsNull(metadata.Service);
        Assert.IsNull(metadata.Version);
        Assert.IsNull(metadata.Variant);
    }

    [TestMethod]
    public void BuildAndroidMetadata_WithoutBuildId_IncludesServiceVersion()
    {
        var builder = new MetadataBuilder();
        var metadata = builder.BuildAndroidMetadata(
            serviceName: "test.app",
            version: "1.0.0+456",
            flavor: "release",
            buildId: null);

        Assert.IsNull(metadata.BuildId);
        Assert.AreEqual("test.app", metadata.Service);
        Assert.AreEqual("1.0.0-456", metadata.Version); // Sanitized
        Assert.AreEqual("release", metadata.Variant);
    }
}
```

#### 6.2 Integration Tests

Mock HTTP server to test actual API calls

#### 6.3 Documentation

- **README.md**: Installation and quick start
- **CONFIGURATION.md**: All available properties
- **ADVANCED.md**: CI/CD, custom scenarios
- **MIGRATION.md**: Guide to migrate from wrapper plugin

## Configuration Reference

### MSBuild Properties

```xml
<PropertyGroup>
  <!-- Required -->
  <DatadogSymbolsServiceName>com.company.app</DatadogSymbolsServiceName>

  <!-- Platform-specific (optional) -->
  <DatadogSymbolsServiceNameAndroid>com.company.app.android</DatadogSymbolsServiceNameAndroid>
  <DatadogSymbolsServiceNameiOS>com.company.app.ios</DatadogSymbolsServiceNameiOS>

  <!-- Authentication -->
  <DatadogSymbolsApiKey>your-api-key</DatadogSymbolsApiKey>
  <!-- Fallback: DD_API_KEY or DATADOG_API_KEY env var -->

  <!-- Configuration -->
  <DatadogSymbolsAppVersion>1.2.3</DatadogSymbolsAppVersion>
  <DatadogSymbolsSite>datadoghq.com</DatadogSymbolsSite>
  <DatadogSymbolsDryRun>false</DatadogSymbolsDryRun>
  <DatadogSymbolsFlavor>release</DatadogSymbolsFlavor>

  <!-- Features -->
  <DatadogSymbolsUploadEnabled>true</DatadogSymbolsUploadEnabled>
  <DatadogSymbolsUploadInDebug>false</DatadogSymbolsUploadInDebug>
  <DatadogSymbolsGenerateBuildId>true</DatadogSymbolsGenerateBuildId>
  <DatadogSymbolsDisableGit>false</DatadogSymbolsDisableGit>

  <!-- Custom paths (optional) -->
  <DatadogSymbolsAndroidMappingPath>/custom/mapping.txt</DatadogSymbolsAndroidMappingPath>
  <DatadogSymbolsiOSDsymPath>/custom/app.dSYM</DatadogSymbolsiOSDsymPath>
</PropertyGroup>
```

## Migration Path

### From Wrapper Plugin

1. Uninstall old package:
   ```bash
   dotnet remove package Datadog.MAUI.Symbols
   ```

2. Install native plugin:
   ```bash
   dotnet add package Datadog.MAUI.Symbols
   ```

3. Update properties (rename prefix):
   ```xml
   <!-- Before -->
   <DatadogSymbolsUseBundledCi>true</DatadogSymbolsUseBundledCi>

   <!-- After (property removed, always native) -->
   <!-- No change needed -->
   ```

4. Remove Node.js dependency (no longer needed)

## Benefits Summary

| Aspect | Wrapper Plugin | Native Plugin |
|--------|---------------|---------------|
| **Dependencies** | Node.js + npm + CLI | None |
| **Installation** | Complex | Simple |
| **Performance** | Slower (process spawn) | Faster (in-process) |
| **Error Handling** | Limited | Rich .NET exceptions |
| **Debugging** | Difficult | Standard .NET debugging |
| **Features** | Limited by CLI | Full API access |
| **Build Integration** | Indirect | Direct |
| **Maintenance** | Two codebases | Single codebase |

## Risk Mitigation

1. **API Changes**: Monitor Datadog API changelog, version headers in requests
2. **Backwards Compatibility**: Support both build_id and service/version patterns
3. **Graceful Degradation**: Never fail builds on upload errors (ContinueOnError="true")
4. **Logging**: Comprehensive MSBuild logs for troubleshooting
5. **Testing**: Extensive unit + integration tests before release

## Success Metrics

- Zero Node.js dependencies
- <2s upload time for typical symbol files
- 100% feature parity with CLI
- <5 minutes migration time from wrapper plugin
- Support for .NET 6, 7, 8+ and .NET MAUI

## Timeline

- **Week 1**: Core API client + metadata builder + tests
- **Week 2**: File locator + git metadata + MSBuild tasks
- **Week 3**: Integration testing + documentation + NuGet packaging
- **Week 4**: Beta release + migration guide + feedback loop

## Next Steps

1. Create project structure in `Datadog.MAUI.Symbols/`
2. Implement Phase 1 (API Client)
3. Add comprehensive tests
4. Iterate based on testing feedback
5. Prepare NuGet package
6. Document migration path
