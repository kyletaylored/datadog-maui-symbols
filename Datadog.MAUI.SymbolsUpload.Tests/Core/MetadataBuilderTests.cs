using Datadog.MAUI.SymbolsUpload.Core;
using Datadog.MAUI.SymbolsUpload.Models;

namespace Datadog.MAUI.SymbolsUpload.Tests.Core;

public class MetadataBuilderTests
{
    private readonly MetadataBuilder _builder = new();

    [Fact]
    public void BuildEventMetadata_WithRequiredFieldsOnly_ShouldIncludeOnlyRequired()
    {
        // Arrange
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "jvm_mapping_file"
        };

        // Act
        var result = _builder.BuildEventMetadata(metadata);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("1.0.0", result["cli_version"]);
        Assert.Equal("jvm_mapping_file", result["type"]);
    }

    [Fact]
    public void BuildEventMetadata_WithOptionalFields_ShouldIncludeAll()
    {
        // Arrange
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "jvm_mapping_file",
            Arch = "arm64",
            Platform = "android",
            GitCommitSha = "abc123",
            GitRepositoryUrl = "https://github.com/test/repo"
        };

        // Act
        var result = _builder.BuildEventMetadata(metadata);

        // Assert
        Assert.Equal(6, result.Count);
        Assert.Equal("arm64", result["arch"]);
        Assert.Equal("android", result["platform"]);
        Assert.Equal("abc123", result["git_commit_sha"]);
        Assert.Equal("https://github.com/test/repo", result["git_repository_url"]);
    }

    [Fact]
    public void BuildEventMetadata_WithServiceVersionVariant_ShouldIncludeThem()
    {
        // Arrange
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "jvm_mapping_file",
            Service = "my-app",
            Version = "1.2.3",
            Variant = "release"
        };

        // Act
        var result = _builder.BuildEventMetadata(metadata);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal("my-app", result["service"]);
        Assert.Equal("1.2.3", result["version"]);
        Assert.Equal("release", result["variant"]);
    }

    [Fact]
    public void BuildEventMetadata_WithBuildId_ShouldIncludeServiceVersionVariantAndBuildId()
    {
        // Arrange
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "jvm_mapping_file",
            BuildId = "abc123def456",
            Service = "my-app",
            Version = "1.2.3",
            Variant = "release"
        };

        // Act
        var result = _builder.BuildEventMetadata(metadata);

        // Assert
        // BuildId is supplemental, not a replacement - all fields should be included
        Assert.Equal(6, result.Count);
        Assert.Equal("abc123def456", result["build_id"]);
        Assert.Equal("my-app", result["service"]);
        Assert.Equal("1.2.3", result["version"]);
        Assert.Equal("release", result["variant"]);
    }

    [Fact]
    public void ValidateMetadata_WithValidRequiredFields_ShouldReturnTrue()
    {
        // Arrange
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "jvm_mapping_file",
            Service = "my-app",
            Version = "1.2.3"
        };

        // Act
        var result = _builder.ValidateMetadata(metadata, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateMetadata_WithBuildId_ShouldStillRequireServiceAndVersion()
    {
        // Arrange - BuildId is supplemental, service and version are always required
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "jvm_mapping_file",
            BuildId = "abc123",
            Service = "my-app",
            Version = "1.2.3"
        };

        // Act
        var result = _builder.ValidateMetadata(metadata, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateMetadata_WithoutCliVersion_ShouldReturnFalse()
    {
        // Arrange
        var metadata = new SymbolMetadata
        {
            CliVersion = "",
            Type = "jvm_mapping_file",
            Service = "my-app",
            Version = "1.2.3"
        };

        // Act
        var result = _builder.ValidateMetadata(metadata, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("CliVersion is required", errorMessage);
    }

    [Fact]
    public void ValidateMetadata_WithoutType_ShouldReturnFalse()
    {
        // Arrange
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "",
            Service = "my-app",
            Version = "1.2.3"
        };

        // Act
        var result = _builder.ValidateMetadata(metadata, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Type is required", errorMessage);
    }

    [Fact]
    public void ValidateMetadata_WithoutService_ShouldReturnFalse()
    {
        // Arrange
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "jvm_mapping_file",
            Version = "1.2.3"
        };

        // Act
        var result = _builder.ValidateMetadata(metadata, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Service is required", errorMessage);
    }

    [Fact]
    public void ValidateMetadata_WithoutVersion_ShouldReturnFalse()
    {
        // Arrange
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "jvm_mapping_file",
            Service = "my-app"
        };

        // Act
        var result = _builder.ValidateMetadata(metadata, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Version is required", errorMessage);
    }

    [Fact]
    public void ValidateMetadata_WithBuildIdButNoService_ShouldReturnFalse()
    {
        // Arrange - Service is always required, even with BuildId
        var metadata = new SymbolMetadata
        {
            CliVersion = "1.0.0",
            Type = "jvm_mapping_file",
            BuildId = "abc123",
            Version = "1.2.3"
        };

        // Act
        var result = _builder.ValidateMetadata(metadata, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Service is required", errorMessage);
    }
}
