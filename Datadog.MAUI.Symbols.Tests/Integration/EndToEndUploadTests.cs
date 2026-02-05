using Datadog.MAUI.Symbols.Core;
using Datadog.MAUI.Symbols.Models;

namespace Datadog.MAUI.Symbols.Tests.Integration;

/// <summary>
/// End-to-end integration tests for the complete upload workflow.
/// These tests use real file I/O but mock network calls.
/// To run against real Datadog API, set environment variable: DATADOG_INTEGRATION_TEST_API_KEY
/// </summary>
[Trait("Category", "Integration")]
public class EndToEndUploadTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string? _realApiKey;

    public EndToEndUploadTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"datadog-e2e-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        // Allow running against real API if key is provided
        _realApiKey = Environment.GetEnvironmentVariable("DATADOG_INTEGRATION_TEST_API_KEY");
    }

    [Fact]
    public void SymbolFileLocator_WithRealFileStructure_ShouldFindFiles()
    {
        // Arrange: Create Android mapping file structure
        var mappingDir = Path.Combine(_tempDir, "build", "outputs", "mapping", "release");
        Directory.CreateDirectory(mappingDir);
        var mappingFile = Path.Combine(mappingDir, "mapping.txt");
        File.WriteAllText(mappingFile, "# ProGuard mapping file\ncom.example.MainActivity -> a:\n");

        var locator = new SymbolFileLocator();

        // Act
        var files = locator.FindAndroidMappingFiles(_tempDir);

        // Assert
        Assert.Single(files);
        Assert.Equal(mappingFile, files[0]);
        Assert.True(File.Exists(files[0]));
    }

    [Fact]
    public void GitMetadataCollector_InGitRepository_ShouldCollectMetadata()
    {
        // Arrange: This test runs in the actual git repo
        var repoRoot = FindGitRepositoryRoot();

        if (repoRoot == null)
        {
            // Skip if not in a git repo
            return;
        }

        var collector = new GitMetadataCollector(repoRoot);

        // Act
        var metadata = collector.CollectMetadata();

        // Assert
        Assert.NotNull(metadata);
        Assert.NotEmpty(metadata.Hash);
        Assert.NotEmpty(metadata.Remote);
        Assert.True(metadata.Hash.Length >= 7); // At least short hash
        Assert.NotEmpty(metadata.TrackedFiles);
    }

    [Fact]
    public void GitMetadataCollector_OutsideGitRepository_ShouldReturnNull()
    {
        // Arrange
        var collector = new GitMetadataCollector(_tempDir);

        // Act
        var metadata = collector.CollectMetadata();

        // Assert
        Assert.Null(metadata);
    }

    [Fact]
    public void MetadataBuilder_WithCompleteMetadata_ShouldBuildValidPayload()
    {
        // Arrange
        var builder = new MetadataBuilder();
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "jvm_mapping_file",
            Service = "test-service",
            Version = "1.2.3",
            Variant = "release",
            Platform = "android",
            Arch = "arm64",
            GitCommitSha = "abc123def456",
            GitRepositoryUrl = "https://github.com/kyletaylored/datadog-maui-symbols.git"
        };

        // Act
        var payload = builder.BuildEventMetadata(metadata);

        // Assert
        Assert.Equal(9, payload.Count);
        Assert.Equal("1.0.0", payload["cli_version"]);
        Assert.Equal("jvm_mapping_file", payload["type"]);
        Assert.Equal("test-service", payload["service"]);
        Assert.Equal("1.2.3", payload["version"]);
        Assert.Equal("release", payload["variant"]);
        Assert.Equal("android", payload["platform"]);
        Assert.Equal("arm64", payload["arch"]);
        Assert.Equal("abc123def456", payload["git_commit_sha"]);
        Assert.Equal("https://github.com/kyletaylored/datadog-maui-symbols.git", payload["git_repository_url"]);
    }

    [Fact]
    public void CompleteWorkflow_AndroidMapping_WithoutApi()
    {
        // Arrange: Set up complete Android project structure
        var projectDir = Path.Combine(_tempDir, "android-project");
        var mappingDir = Path.Combine(projectDir, "build", "outputs", "mapping", "release");
        Directory.CreateDirectory(mappingDir);
        var mappingFile = Path.Combine(mappingDir, "mapping.txt");
        File.WriteAllText(mappingFile, GenerateProGuardMapping());

        // Act: Locate file
        var locator = new SymbolFileLocator();
        var foundFile = locator.LocateSymbolFile(projectDir, "android", "release");

        // Assert: File found
        Assert.NotNull(foundFile);
        Assert.Equal(mappingFile, foundFile);

        // Act: Build metadata
        var builder = new MetadataBuilder();
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "jvm_mapping_file",
            Service = "com.example.app",
            Version = "1.0.0",
            Variant = "release",
            Platform = "android"
        };

        // Assert: Metadata valid
        var isValid = builder.ValidateMetadata(metadata, out var error);
        Assert.True(isValid);
        Assert.Null(error);

        // Act: Build payload
        var payload = builder.BuildEventMetadata(metadata);

        // Assert: Payload complete
        Assert.Contains("service", payload.Keys);
        Assert.Contains("version", payload.Keys);
        Assert.Contains("variant", payload.Keys);
        Assert.Contains("type", payload.Keys);
    }

    [Fact]
    public void CompleteWorkflow_AndroidMappingWithBuildId_WithoutApi()
    {
        // Arrange: Set up Android project with mapping file
        var projectDir = Path.Combine(_tempDir, "android-buildid-project");
        var mappingDir = Path.Combine(projectDir, "build", "outputs", "mapping", "release");
        Directory.CreateDirectory(mappingDir);
        var mappingFile = Path.Combine(mappingDir, "mapping.txt");
        File.WriteAllText(mappingFile, GenerateProGuardMapping());

        // Act: Locate file
        var locator = new SymbolFileLocator();
        var foundFile = locator.LocateSymbolFile(projectDir, "android");

        // Assert: File found
        Assert.NotNull(foundFile);

        // Act: Build metadata with BuildId (service/version still required)
        var builder = new MetadataBuilder();
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "jvm_mapping_file",
            Service = "com.example.app",
            Version = "1.0.0",
            BuildId = "abc123def456",
            Platform = "android"
        };

        // Assert: Metadata valid
        var isValid = builder.ValidateMetadata(metadata, out var error);
        Assert.True(isValid);
        Assert.Null(error);

        // Act: Build payload
        var payload = builder.BuildEventMetadata(metadata);

        // Assert: Payload has BuildId AND service/version (BuildId is supplemental)
        Assert.Contains("build_id", payload.Keys);
        Assert.Contains("service", payload.Keys);
        Assert.Contains("version", payload.Keys);
    }

    [Fact]
    public void CompleteWorkflow_IosDsym_WithoutApi()
    {
        // Arrange: Set up iOS project structure with dSYM bundle
        var projectDir = Path.Combine(_tempDir, "ios-project");
        var dsymBundle = Path.Combine(projectDir, "MyApp.app.dSYM");
        Directory.CreateDirectory(dsymBundle);
        var dsymContents = Path.Combine(dsymBundle, "Contents");
        Directory.CreateDirectory(dsymContents);
        File.WriteAllText(Path.Combine(dsymContents, "Info.plist"), "<plist></plist>");

        // Act: Locate dSYM
        var locator = new SymbolFileLocator();
        var foundDsym = locator.LocateSymbolFile(projectDir, "ios");

        // Assert: dSYM found
        Assert.NotNull(foundDsym);
        Assert.Equal(dsymBundle, foundDsym);
        Assert.True(Directory.Exists(foundDsym));

        // Act: Build iOS metadata
        var builder = new MetadataBuilder();
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "ios_dsym",
            Service = "com.example.app",
            Version = "1.0.0",
            Platform = "ios",
            Arch = "arm64"
        };

        // Assert: Metadata valid
        var isValid = builder.ValidateMetadata(metadata, out var error);
        Assert.True(isValid);
        Assert.Null(error);

        // Act: Build payload
        var payload = builder.BuildEventMetadata(metadata);

        // Assert: Payload complete
        Assert.Equal("ios_dsym", payload["type"]);
        Assert.Equal("ios", payload["platform"]);
        Assert.Equal("arm64", payload["arch"]);
    }

    private string? FindGitRepositoryRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(current))
        {
            if (Directory.Exists(Path.Combine(current, ".git")))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }
        return null;
    }

    private string GenerateProGuardMapping()
    {
        return @"# ProGuard mapping file
com.example.MainActivity -> a:
    void onCreate(android.os.Bundle) -> a
    void onDestroy() -> b
com.example.MyService -> b:
    void onStartCommand(android.content.Intent,int,int) -> a
";
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
