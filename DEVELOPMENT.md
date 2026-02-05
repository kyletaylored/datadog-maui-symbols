# Development Guide

This guide covers building, testing, and contributing to the Datadog.MAUI.SymbolsUpload plugin.

## Prerequisites

- .NET 9 SDK or later
- MAUI workload: `dotnet workload install maui`
- Android SDK (for Android testing)
- Xcode (for iOS testing, macOS only)

## Project Structure

```
Datadog.MAUI.SymbolsUpload/
├── Datadog.MAUI.SymbolsUpload/          # Main plugin project
│   ├── Core/                            # Core components (API client, locators, collectors)
│   ├── Models/                          # Data models
│   ├── Tasks/                           # MSBuild tasks
│   └── build/                           # MSBuild .targets files
├── Datadog.MAUI.SymbolsUpload.Tests/    # Unit and integration tests
├── samples/MauiSampleApp/               # Sample MAUI application
└── docs/                                # Additional documentation
```

## Quick Commands (Makefile)

A Makefile is provided for common development tasks:

```bash
# Show all available commands
make help

# Build the plugin
make build

# Run all tests
make test

# Run only unit tests
make test-unit

# Run only integration tests
make test-int

# Create NuGet package
make pack

# Build sample app (Android + iOS)
make sample-build

# Build sample app for Android only
make sample-android

# Build sample app for iOS only
make sample-ios

# Build and run sample app on Android
make sample-run-android

# Build and run sample app on iOS simulator
make sample-run-ios

# Clean all build outputs
make clean
```

## Manual Commands

If you prefer not to use Make:

### Building

```bash
# Build plugin
dotnet build Datadog.MAUI.SymbolsUpload/Datadog.MAUI.SymbolsUpload.csproj -c Release

# Create package
dotnet pack Datadog.MAUI.SymbolsUpload/Datadog.MAUI.SymbolsUpload.csproj -c Release
```

### Testing

```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test --filter "Category!=Integration"

# Run only integration tests
dotnet test --filter "Category=Integration"

# Run tests with detailed output
dotnet test --verbosity detailed
```

### Sample App

```bash
# Build for Android
dotnet build samples/MauiSampleApp/MauiSampleApp.csproj -f net9.0-android -c Release

# Build for iOS
dotnet build samples/MauiSampleApp/MauiSampleApp.csproj -f net9.0-ios -c Release

# Run on Android
dotnet build samples/MauiSampleApp/MauiSampleApp.csproj -f net9.0-android -c Debug -t:Run

# Run on iOS simulator
dotnet build samples/MauiSampleApp/MauiSampleApp.csproj -f net9.0-ios -c Debug -t:Run
```

## Testing

### Unit Tests

Unit tests cover individual components without external dependencies:

- **Core components**: MetadataBuilder, SymbolFileLocator
- **Models**: GitMetadata, UploadResult serialization
- **Validation logic**: Input validation, metadata building

Run with: `make test-unit` or `dotnet test --filter "Category!=Integration"`

### Integration Tests

Integration tests validate complete workflows with real file I/O:

- Symbol file location in realistic directory structures
- Git metadata collection (requires git repository)
- Complete upload workflow (without actual API calls)

Run with: `make test-int` or `dotnet test --filter "Category=Integration"`

### Testing Against Real API

To test against the real Datadog API, set the environment variable:

```bash
export DATADOG_INTEGRATION_TEST_API_KEY=your_api_key
dotnet test --filter "Category=Integration"
```

**Note**: Currently all integration tests skip actual API calls by default.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed architecture documentation.

## Contributing

### Code Style

- Follow standard C# naming conventions
- Use nullable reference types where appropriate
- Add XML documentation comments to public APIs
- Keep methods focused and testable

### Adding New Features

1. Create/update models in `Models/` if needed
2. Implement core logic in `Core/`
3. Add/update MSBuild task in `Tasks/`
4. Update MSBuild targets in `build/` if needed
5. Add unit tests for new functionality
6. Add integration tests for end-to-end workflows
7. Update documentation

### Pull Request Process

1. Ensure all tests pass: `make test`
2. Build successfully: `make build`
3. Update relevant documentation
4. Test with the sample app if applicable
5. Submit PR with clear description of changes

## Debugging

### Debugging MSBuild Tasks

Add diagnostic logging to your build:

```bash
dotnet build -v:diag > build.log 2>&1
```

### Debugging with Sample App

1. Set breakpoints in the plugin code
2. Build the plugin: `dotnet build Datadog.MAUI.SymbolsUpload/Datadog.MAUI.SymbolsUpload.csproj`
3. Build sample app with plugin: `dotnet build samples/MauiSampleApp/MauiSampleApp.csproj`
4. Check build output for task execution

### Common Issues

**Task not executing:**
- Check target conditions in `.targets` file
- Verify `DatadogSymbolUploadEnabled=true`
- Check platform identifiers match

**Symbol files not found:**
- Verify build output directory structure
- Check `DatadogSymbolFilePath` if manually specified
- Ensure ProGuard/R8 or dSYM generation is enabled

## Release Process

1. Update version in `Datadog.MAUI.SymbolsUpload.csproj`
2. Update CHANGELOG.md with release notes
3. Run full test suite: `make test`
4. Create NuGet package: `make pack`
5. Test package with sample app
6. Push to NuGet: `dotnet nuget push`
7. Tag release in git
8. Create GitHub release with notes

## Implementation Status

- [x] Research and design
- [x] Core models (SymbolMetadata, GitMetadata, UploadResult, UploadRequest)
- [x] Core API client with gzip compression (DatadogApiClient, MetadataBuilder)
- [x] Symbol file locator
- [x] Git metadata collector
- [x] MSBuild tasks (UploadSymbolsTask, GenerateBuildIdTask)
- [x] MSBuild targets integration
- [x] NuGet package structure
- [x] Unit tests (25 tests)
- [x] Integration tests (7 end-to-end tests)
- [x] Sample MAUI application with runtime build info display
- [x] Improved user-facing messages (matches datadog-ci style)
- [x] API compatibility verified with proxy testing
- [x] Dry-run mode for testing configuration without uploads
- [ ] Production release

## Resources

- [MSBuild Task Documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-task-reference)
- [NuGet Package Creation](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package-msbuild)
- [Datadog Symbols API](https://docs.datadoghq.com/real_user_monitoring/error_tracking/mobile/android/#upload-your-mapping-file)
