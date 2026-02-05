using Datadog.MAUI.Symbols.Core;

namespace Datadog.MAUI.Symbols.Tests.Core;

public class SymbolFileLocatorTests
{
    private readonly SymbolFileLocator _locator = new();
    private readonly string _tempDir;

    public SymbolFileLocatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"datadog-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void FindAndroidMappingFiles_WithNoMappingDirectory_ShouldReturnEmpty()
    {
        // Act
        var files = _locator.FindAndroidMappingFiles(_tempDir);

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public void FindAndroidMappingFiles_WithMappingFile_ShouldFindIt()
    {
        // Arrange
        var mappingDir = Path.Combine(_tempDir, "build", "outputs", "mapping", "release");
        Directory.CreateDirectory(mappingDir);
        var mappingFile = Path.Combine(mappingDir, "mapping.txt");
        File.WriteAllText(mappingFile, "# Mapping file");

        // Act
        var files = _locator.FindAndroidMappingFiles(_tempDir);

        // Assert
        Assert.Single(files);
        Assert.Equal(mappingFile, files[0]);

        // Cleanup
        Directory.Delete(Path.Combine(_tempDir, "build"), true);
    }

    [Fact]
    public void FindAndroidMappingFiles_WithSpecificVariant_ShouldFindOnlyThatVariant()
    {
        // Arrange
        var releaseDir = Path.Combine(_tempDir, "build", "outputs", "mapping", "release");
        var debugDir = Path.Combine(_tempDir, "build", "outputs", "mapping", "debug");
        Directory.CreateDirectory(releaseDir);
        Directory.CreateDirectory(debugDir);
        File.WriteAllText(Path.Combine(releaseDir, "mapping.txt"), "# Release");
        File.WriteAllText(Path.Combine(debugDir, "mapping.txt"), "# Debug");

        // Act
        var files = _locator.FindAndroidMappingFiles(_tempDir, "release");

        // Assert
        Assert.Single(files);
        Assert.Contains("release", files[0]);

        // Cleanup
        Directory.Delete(Path.Combine(_tempDir, "build"), true);
    }

    [Fact]
    public void FindAndroidMappingFiles_WithMultipleVariants_ShouldFindAll()
    {
        // Arrange
        var releaseDir = Path.Combine(_tempDir, "build", "outputs", "mapping", "release");
        var debugDir = Path.Combine(_tempDir, "build", "outputs", "mapping", "debug");
        Directory.CreateDirectory(releaseDir);
        Directory.CreateDirectory(debugDir);
        File.WriteAllText(Path.Combine(releaseDir, "mapping.txt"), "# Release");
        File.WriteAllText(Path.Combine(debugDir, "mapping.txt"), "# Debug");

        // Act
        var files = _locator.FindAndroidMappingFiles(_tempDir);

        // Assert
        Assert.Equal(2, files.Count);

        // Cleanup
        Directory.Delete(Path.Combine(_tempDir, "build"), true);
    }

    [Fact]
    public void FindIosDsymBundles_WithNoDsym_ShouldReturnEmpty()
    {
        // Act
        var files = _locator.FindIosDsymBundles(_tempDir);

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public void FindIosDsymBundles_WithDsym_ShouldFindIt()
    {
        // Arrange
        var dsymBundle = Path.Combine(_tempDir, "MyApp.app.dSYM");
        Directory.CreateDirectory(dsymBundle);

        // Act
        var files = _locator.FindIosDsymBundles(_tempDir);

        // Assert
        Assert.Single(files);
        Assert.Equal(dsymBundle, files[0]);

        // Cleanup
        Directory.Delete(dsymBundle, true);
    }

    [Fact]
    public void LocateSymbolFile_WithAndroidPlatform_ShouldFindMappingFile()
    {
        // Arrange
        var mappingDir = Path.Combine(_tempDir, "build", "outputs", "mapping", "release");
        Directory.CreateDirectory(mappingDir);
        var mappingFile = Path.Combine(mappingDir, "mapping.txt");
        File.WriteAllText(mappingFile, "# Mapping file");

        // Act
        var file = _locator.LocateSymbolFile(_tempDir, "android");

        // Assert
        Assert.NotNull(file);
        Assert.Equal(mappingFile, file);

        // Cleanup
        Directory.Delete(Path.Combine(_tempDir, "build"), true);
    }

    [Fact]
    public void LocateSymbolFile_WithIosPlatform_ShouldFindDsym()
    {
        // Arrange
        var dsymBundle = Path.Combine(_tempDir, "MyApp.app.dSYM");
        Directory.CreateDirectory(dsymBundle);

        // Act
        var file = _locator.LocateSymbolFile(_tempDir, "ios");

        // Assert
        Assert.NotNull(file);
        Assert.Equal(dsymBundle, file);

        // Cleanup
        Directory.Delete(dsymBundle, true);
    }

    [Fact]
    public void LocateSymbolFile_WithInvalidPlatform_ShouldReturnNull()
    {
        // Act
        var file = _locator.LocateSymbolFile(_tempDir, "invalid");

        // Assert
        Assert.Null(file);
    }
}
