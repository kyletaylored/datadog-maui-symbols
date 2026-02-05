# Implementation Checklist

> **📋 Historical Document**: This is the original development roadmap created during planning. The project is now complete - see [README.md](../../README.md) for current status and documentation.

## Phase 1: Project Setup

- [ ] Create solution structure
  ```bash
  dotnet new classlib -n Datadog.MAUI.Symbols -f netstandard2.0
  dotnet new xunit -n Datadog.MAUI.Symbols.Tests
  dotnet new sln -n Datadog.MAUI.Symbols
  dotnet sln add Datadog.MAUI.Symbols/Datadog.MAUI.Symbols.csproj
  dotnet sln add Datadog.MAUI.Symbols.Tests/Datadog.MAUI.Symbols.Tests.csproj
  ```

- [ ] Add NuGet dependencies
  - Microsoft.Build.Framework (17.0.0)
  - Microsoft.Build.Utilities.Core (17.0.0)
  - System.Net.Http (4.3.4)
  - Newtonsoft.Json (13.0.3) for netstandard2.0
  - System.Text.Json (8.0.0) for net6.0+

- [ ] Configure project properties
  - Target frameworks: netstandard2.0;net6.0;net8.0
  - Package metadata (ID, version, authors, description)
  - Enable XML documentation generation

## Phase 2: Core Models (Day 1)

- [ ] Create `Models/SymbolMetadata.cs`
  ```csharp
  public class SymbolMetadata
  {
      public string Arch { get; set; }
      public string BuildId { get; set; }
      public string CliVersion { get; set; }
      public string GitCommitSha { get; set; }
      public string GitRepositoryUrl { get; set; }
      public string Platform { get; set; }
      public string Service { get; set; }
      public string Type { get; set; }
      public string Variant { get; set; }
      public string Version { get; set; }
  }
  ```

- [ ] Create `Models/GitMetadata.cs`
- [ ] Create `Models/UploadResult.cs`
- [ ] Create `Models/UploadRequest.cs`
- [ ] Write unit tests for models

## Phase 3: API Client (Days 2-3)

- [ ] Implement `Core/DatadogApiClient.cs`
  - [ ] Constructor with API key and site
  - [ ] HTTP client configuration
  - [ ] Request headers setup
  - [ ] CreateMultipartContent method
  - [ ] UploadSymbolsAsync method
  - [ ] Retry logic with exponential backoff
  - [ ] IDisposable implementation

- [ ] Implement `Core/MetadataBuilder.cs`
  - [ ] BuildAndroidMetadata method
  - [ ] BuildiOSMetadata method
  - [ ] Version sanitization (+ to -)
  - [ ] Conditional field inclusion logic

- [ ] Write unit tests
  - [ ] Test metadata generation with build_id
  - [ ] Test metadata generation without build_id
  - [ ] Test version sanitization
  - [ ] Test HTTP request formatting

- [ ] Write integration tests (mock HTTP server)
  - [ ] Test successful upload
  - [ ] Test retry on transient failures
  - [ ] Test non-retryable errors (400, 403, 413)
  - [ ] Test timeout handling

## Phase 4: File Locator (Day 4)

- [ ] Implement `Core/SymbolFileLocator.cs`
  - [ ] FindAndroidMapping method with multiple search paths
  - [ ] FindiOSDsym method with multiple search paths
  - [ ] Logging for each search attempt
  - [ ] Custom path override support

- [ ] Write unit tests
  - [ ] Test Android mapping discovery
  - [ ] Test iOS dSYM discovery
  - [ ] Test custom path override
  - [ ] Test missing file warnings

## Phase 5: Git Integration (Day 5)

- [ ] Implement `Core/GitMetadataCollector.cs`
  - [ ] IsGitRepository check (.git directory or file)
  - [ ] GetCommitSha via git command
  - [ ] GetRemoteUrl via git command
  - [ ] GetTrackedFiles via git ls-files
  - [ ] ExecuteGitCommand helper method
  - [ ] Error handling for missing git

- [ ] Write unit tests
  - [ ] Test with real git repository
  - [ ] Test without git (graceful failure)
  - [ ] Test git command failures
  - [ ] Test worktree support

## Phase 6: MSBuild Tasks (Days 6-7)

- [ ] Implement `Tasks/UploadSymbolsTask.cs`
  - [ ] Define required properties with [Required] attribute
  - [ ] Define optional properties with defaults
  - [ ] Implement Execute method
  - [ ] Implement ExecuteAsync method
  - [ ] Service name resolution logic
  - [ ] API key resolution logic
  - [ ] Configuration validation
  - [ ] Platform-specific logic (Android vs iOS)
  - [ ] Dry-run mode
  - [ ] Error handling with ContinueOnError
  - [ ] ICancelableTask implementation

- [ ] Implement `Tasks/GenerateBuildIdTask.cs`
  - [ ] Generate 8-char GUID substring
  - [ ] Create DatadogBuildInfo.g.cs file
  - [ ] Output BuildId property
  - [ ] Directory creation logic

- [ ] Write MSBuild task tests
  - [ ] Test with valid configuration
  - [ ] Test with missing required properties
  - [ ] Test service name resolution hierarchy
  - [ ] Test dry-run mode
  - [ ] Test build ID generation

## Phase 7: MSBuild Targets (Day 8)

- [ ] Create `build/Datadog.MAUI.Symbols.targets`
  - [ ] Define default properties
  - [ ] UsingTask declarations
  - [ ] DatadogGenerateBuildId target (BeforeTargets="CoreCompile")
  - [ ] DatadogUploadSymbols target (AfterTargets="Publish")
  - [ ] Platform-specific conditional execution
  - [ ] Debug/Release conditional execution
  - [ ] Property passing to tasks

- [ ] Create `buildTransitive/Datadog.MAUI.Symbols.targets`
  - [ ] Same as build/ for transitive references

- [ ] Test targets integration
  - [ ] Create test MAUI project
  - [ ] Add PackageReference
  - [ ] Verify targets are imported
  - [ ] Verify tasks execute at correct times
  - [ ] Verify properties flow correctly

## Phase 8: NuGet Packaging (Day 9)

- [ ] Update `.csproj` for packaging
  - [ ] Set `<GeneratePackageOnBuild>true</GeneratePackageOnBuild>`
  - [ ] Define package metadata
  - [ ] Include targets in build/ and buildTransitive/
  - [ ] Include task assembly in lib/
  - [ ] Set `<DevelopmentDependency>false</DevelopmentDependency>`

- [ ] Test local NuGet package
  - [ ] `dotnet pack`
  - [ ] Create local NuGet source
  - [ ] Install in test project
  - [ ] Verify all files included correctly

## Phase 9: Documentation (Day 10)

- [ ] Write README.md
  - [ ] Installation instructions
  - [ ] Quick start example
  - [ ] Configuration reference
  - [ ] Feature overview

- [ ] Write CONFIGURATION.md
  - [ ] All MSBuild properties
  - [ ] Environment variables
  - [ ] Default values table
  - [ ] Platform-specific options

- [ ] Write ADVANCED.md
  - [ ] CI/CD integration examples
  - [ ] Build ID usage
  - [ ] Debug build uploads
  - [ ] Custom symbol paths
  - [ ] Multi-platform builds

- [ ] Write MIGRATION.md
  - [ ] Migration steps from wrapper plugin
  - [ ] Property mapping table
  - [ ] Breaking changes
  - [ ] Benefits of migration

- [ ] Write TROUBLESHOOTING.md
  - [ ] Common errors and solutions
  - [ ] Logging configuration
  - [ ] Debugging MSBuild tasks
  - [ ] API authentication issues

## Phase 10: Testing & Validation (Days 11-12)

- [ ] Unit test coverage
  - [ ] Aim for >80% code coverage
  - [ ] Test all error paths
  - [ ] Test all configuration combinations

- [ ] Integration testing
  - [ ] Test with real MAUI Android project
  - [ ] Test with real MAUI iOS project
  - [ ] Test with mock Datadog API
  - [ ] Test in CI/CD environment

- [ ] Performance testing
  - [ ] Measure upload time vs wrapper plugin
  - [ ] Test with large symbol files (>10MB)
  - [ ] Test with many files

- [ ] Compatibility testing
  - [ ] .NET 6, 7, 8
  - [ ] .NET MAUI versions
  - [ ] Windows, macOS, Linux
  - [ ] Different MSBuild versions

## Phase 11: Release Preparation (Day 13)

- [ ] Version 1.0.0-beta.1
  - [ ] Tag in git
  - [ ] Build NuGet package
  - [ ] Publish to NuGet.org (or private feed)

- [ ] Create GitHub release
  - [ ] Release notes
  - [ ] Binary artifacts
  - [ ] Migration guide link

- [ ] Announce beta
  - [ ] Internal testing
  - [ ] Collect feedback
  - [ ] Iterate

## Phase 12: Production Release (Day 14)

- [ ] Address beta feedback
- [ ] Final testing pass
- [ ] Version 1.0.0 release
- [ ] Update documentation
- [ ] Publish announcement

## Ongoing Maintenance

- [ ] Monitor Datadog API changes
- [ ] Update for new .NET versions
- [ ] Add requested features
- [ ] Fix reported bugs
- [ ] Keep dependencies updated

## Success Criteria

- ✅ Zero Node.js dependencies
- ✅ <2s upload time for typical files
- ✅ 100% feature parity with CLI
- ✅ Works on Windows, macOS, Linux
- ✅ Supports .NET 6, 7, 8
- ✅ <80% code coverage
- ✅ Comprehensive documentation
- ✅ Easy migration path

## Notes

- Use `ContinueOnError="true"` on upload task to never break builds
- Include verbose logging with `[Datadog]` prefix
- Test with both build_id and service/version patterns
- Support environment variables as fallback for all config
