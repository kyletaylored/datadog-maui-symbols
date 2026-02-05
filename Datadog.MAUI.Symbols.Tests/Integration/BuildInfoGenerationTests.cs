using System.Diagnostics;

namespace Datadog.MAUI.Symbols.Tests.Integration;

/// <summary>
/// Integration tests for MSBuild target that generates DatadogBuildInfo class.
/// These tests verify that the build-time code generation works correctly.
/// </summary>
[Trait("Category", "Integration")]
public class BuildInfoGenerationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _testProjectPath;

    public BuildInfoGenerationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"datadog-buildinfo-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _testProjectPath = Path.Combine(_tempDir, "TestProject.csproj");
    }

    [Fact]
    public void BuildTarget_GeneratesDatadogBuildInfoClass()
    {
        // Arrange: Create minimal test project that references our package
        var projectContent = CreateTestProject(
            serviceName: "test-service",
            version: "1.2.3",
            variant: "TestVariant"
        );
        File.WriteAllText(_testProjectPath, projectContent);

        // Act: Build the project
        var buildSuccess = BuildProject(_testProjectPath, out var objDir);

        // Assert: Build succeeded
        Assert.True(buildSuccess, "Project build should succeed");

        // Assert: Generated file exists
        var generatedFile = Path.Combine(objDir, "Datadog.MAUI.Symbols", "DatadogBuildInfo.g.cs");
        Assert.True(File.Exists(generatedFile), $"Generated file should exist at {generatedFile}");

        // Assert: Generated file has correct content
        var generatedCode = File.ReadAllText(generatedFile);
        Assert.Contains("namespace Datadog.MAUI.Symbols", generatedCode);
        Assert.Contains("public static class DatadogBuildInfo", generatedCode);
        Assert.Contains("public const string ServiceName = \"test-service\";", generatedCode);
        Assert.Contains("public const string Version = \"1.2.3\";", generatedCode);
        Assert.Contains("public const string Variant = \"TestVariant\";", generatedCode);
        Assert.Contains("public const string BuildId = ", generatedCode);
    }

    [Fact]
    public void BuildTarget_GeneratesBuildId_WithCorrectFormat()
    {
        // Arrange
        var projectContent = CreateTestProject(
            serviceName: "my-app",
            version: "2.0.0",
            variant: "Release"
        );
        File.WriteAllText(_testProjectPath, projectContent);

        // Act
        var buildSuccess = BuildProject(_testProjectPath, out var objDir);

        // Assert
        Assert.True(buildSuccess);

        var generatedFile = Path.Combine(objDir, "Datadog.MAUI.Symbols", "DatadogBuildInfo.g.cs");
        var generatedCode = File.ReadAllText(generatedFile);

        // Extract BuildId value from generated code
        var buildIdMatch = System.Text.RegularExpressions.Regex.Match(
            generatedCode,
            @"public const string BuildId = ""([^""]+)"";");

        Assert.True(buildIdMatch.Success, "BuildId should be present in generated code");

        var buildId = buildIdMatch.Groups[1].Value;
        Assert.NotEmpty(buildId);
        Assert.Matches("^[a-f0-9]{16}$", buildId); // Should be 16-character hex string
    }

    [Fact]
    public void BuildTarget_WithDifferentVersions_GeneratesDifferentBuildIds()
    {
        // Arrange: Build with version 1.0.0
        var project1 = CreateTestProject("service", "1.0.0", "Release");
        var project1Path = Path.Combine(_tempDir, "Project1.csproj");
        File.WriteAllText(project1Path, project1);

        BuildProject(project1Path, out var objDir1);
        var generatedFile1 = Path.Combine(objDir1, "Datadog.MAUI.Symbols", "DatadogBuildInfo.g.cs");
        var code1 = File.ReadAllText(generatedFile1);
        var buildId1 = ExtractBuildId(code1);

        // Arrange: Build with version 2.0.0
        var project2 = CreateTestProject("service", "2.0.0", "Release");
        var project2Path = Path.Combine(_tempDir, "Project2.csproj");
        File.WriteAllText(project2Path, project2);

        BuildProject(project2Path, out var objDir2);
        var generatedFile2 = Path.Combine(objDir2, "Datadog.MAUI.Symbols", "DatadogBuildInfo.g.cs");
        var code2 = File.ReadAllText(generatedFile2);
        var buildId2 = ExtractBuildId(code2);

        // Assert: Different versions should produce different build IDs
        Assert.NotEqual(buildId1, buildId2);
    }

    [Fact]
    public void BuildTarget_GeneratedFile_IsValidCSharp()
    {
        // Arrange
        var projectContent = CreateTestProject("test", "1.0.0", "Debug");
        File.WriteAllText(_testProjectPath, projectContent);

        // Act
        var buildSuccess = BuildProject(_testProjectPath, out var objDir);

        // Assert: Build succeeded means generated code is valid C#
        Assert.True(buildSuccess, "Build should succeed, indicating generated code is valid C#");

        // Additional verification: Check for auto-generated comment
        var generatedFile = Path.Combine(objDir, "Datadog.MAUI.Symbols", "DatadogBuildInfo.g.cs");
        var code = File.ReadAllText(generatedFile);
        Assert.Contains("// <auto-generated />", code);
        Assert.Contains("// This file is automatically generated by Datadog.MAUI.Symbols", code);
    }

    private string CreateTestProject(string serviceName, string version, string variant)
    {
        // Get the path to the built Datadog.MAUI.Symbols package
        var solutionDir = FindSolutionRoot();
        var packageProjectDir = Path.Combine(solutionDir, "Datadog.MAUI.Symbols");

        return $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputType>Library</OutputType>

    <!-- Datadog configuration -->
    <DatadogServiceName>{serviceName}</DatadogServiceName>
    <DatadogVersion>{version}</DatadogVersion>
    <DatadogVariant>{variant}</DatadogVariant>
  </PropertyGroup>

  <!-- Reference the Datadog.MAUI.Symbols project directly for testing -->
  <ItemGroup>
    <ProjectReference Include=""{packageProjectDir}\Datadog.MAUI.Symbols.csproj""
                      ReferenceOutputAssembly=""false""
                      PrivateAssets=""All"" />
  </ItemGroup>

  <!-- Import UsingTask declarations (needed when using ProjectReference) -->
  <UsingTask TaskName=""Datadog.MAUI.Symbols.Tasks.GenerateBuildIdTask""
             AssemblyFile=""{packageProjectDir}\bin\$(Configuration)\netstandard2.0\Datadog.MAUI.Symbols.dll"" />

  <!-- Import the targets file -->
  <Import Project=""{packageProjectDir}\build\Datadog.MAUI.Symbols.targets"" />
</Project>";
    }

    private bool BuildProject(string projectPath, out string objDir)
    {
        // First, build the Datadog.MAUI.Symbols project
        var solutionDir = FindSolutionRoot();
        var packageProject = Path.Combine(solutionDir, "Datadog.MAUI.Symbols", "Datadog.MAUI.Symbols.csproj");

        RunDotnetBuild(packageProject);

        // Then build the test project
        var projectDir = Path.GetDirectoryName(projectPath)!;
        objDir = Path.Combine(projectDir, "obj", "Debug", "net9.0");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\" -c Debug",
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            return false;

        process.WaitForExit();
        return process.ExitCode == 0;
    }

    private void RunDotnetBuild(string projectPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\" -c Debug",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        process?.WaitForExit();
    }

    private string ExtractBuildId(string generatedCode)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            generatedCode,
            @"public const string BuildId = ""([^""]+)"";");

        return match.Success ? match.Groups[1].Value : "";
    }

    private string FindSolutionRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(current))
        {
            if (Directory.GetFiles(current, "*.sln").Length > 0)
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }
        throw new InvalidOperationException("Could not find solution root");
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
