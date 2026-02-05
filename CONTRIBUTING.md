# Contributing to Datadog.MAUI.SymbolsUpload

Thank you for your interest in contributing to the Datadog.MAUI.SymbolsUpload plugin!

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Making Changes](#making-changes)
- [Testing](#testing)
- [Submitting Changes](#submitting-changes)
- [Coding Standards](#coding-standards)
- [Questions](#questions)

## Code of Conduct

This project adheres to the Datadog Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## Getting Started

1. Fork the repository on GitHub
2. Clone your fork locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/datadog-maui-symbols-upload.git
   cd datadog-maui-symbols-upload
   ```
3. Add the upstream repository as a remote:
   ```bash
   git remote add upstream https://github.com/DataDog/datadog-maui-symbols-upload.git
   ```

## Development Setup

### Prerequisites

- .NET 9 SDK or later
- MAUI workload: `dotnet workload install maui`
- Android SDK (for Android testing)
- Xcode (for iOS testing, macOS only)

### Building the Project

```bash
# Build the plugin
make build

# Or manually
dotnet build Datadog.MAUI.SymbolsUpload/Datadog.MAUI.SymbolsUpload.csproj -c Release
```

See [DEVELOPMENT.md](DEVELOPMENT.md) for detailed development instructions.

## Making Changes

### Creating a Branch

Create a feature branch for your changes:

```bash
git checkout -b feature/your-feature-name
```

Use descriptive branch names:
- `feature/add-android-support` for new features
- `fix/upload-timeout` for bug fixes
- `docs/improve-readme` for documentation
- `refactor/api-client` for refactoring

### Writing Code

1. **Follow existing patterns**: Look at similar code in the project
2. **Keep changes focused**: One feature or fix per pull request
3. **Write tests**: Add unit tests for new functionality
4. **Update documentation**: Keep README and other docs in sync with changes

### Commit Messages

Write clear, concise commit messages:

```
Add dry-run mode for testing configuration

- Implement DryRun property on UploadSymbolsTask
- Add detailed logging of what would be uploaded
- Update documentation with dry-run examples
```

Format:
- Use present tense ("Add feature" not "Added feature")
- Use imperative mood ("Move cursor to..." not "Moves cursor to...")
- First line should be 50-72 characters
- Include detailed description if needed

## Testing

### Running Tests

```bash
# Run all tests
make test

# Run only unit tests
make test-unit

# Run only integration tests
make test-int
```

### Writing Tests

- Add unit tests in `Datadog.MAUI.SymbolsUpload.Tests/`
- Follow existing test structure and naming conventions
- Test both success and failure scenarios
- Use descriptive test method names

Example:
```csharp
[Fact]
public void UploadSymbolsTask_WithValidMetadata_ShouldSucceed()
{
    // Arrange
    var task = new UploadSymbolsTask { ... };

    // Act
    var result = task.Execute();

    // Assert
    Assert.True(result);
}
```

### Testing with Sample App

Test your changes with the sample application:

```bash
# Build and run on Android
make sample-run-android

# Build and run on iOS
make sample-run-ios
```

## Submitting Changes

### Before Submitting

1. **Run all tests**: Ensure `make test` passes
2. **Build successfully**: Ensure `make build` completes without errors
3. **Update documentation**: Update README, CHANGELOG, and other docs as needed
4. **Check code style**: Follow C# naming conventions and project patterns
5. **Test manually**: Verify your changes work with the sample app

### Creating a Pull Request

1. Push your changes to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

2. Open a pull request on GitHub

3. Fill out the pull request template with:
   - Clear description of changes
   - Link to related issues
   - Testing performed
   - Screenshots (if UI changes)

4. Wait for review and address feedback

### Pull Request Guidelines

- **Keep PRs focused**: One feature or fix per PR
- **Small is better**: Break large changes into multiple PRs if possible
- **Update CHANGELOG**: Add entry to `[Unreleased]` section
- **Link issues**: Reference related issues with `Fixes #123` or `Relates to #456`
- **Respond to feedback**: Be open to suggestions and make requested changes

## Coding Standards

### C# Style

Follow standard C# conventions:

```csharp
// Use PascalCase for public members
public class UploadSymbolsTask
{
    public string ApiKey { get; set; }

    public async Task<UploadResult> UploadAsync()
    {
        // Use camelCase for local variables
        var apiClient = new DatadogApiClient();

        // Use _camelCase for private fields
        private readonly HttpClient _httpClient;
    }
}
```

### Documentation

- Add XML documentation comments to public APIs
- Keep comments concise and meaningful
- Update docs when changing functionality

```csharp
/// <summary>
/// Uploads symbol files to Datadog with the provided metadata.
/// </summary>
/// <param name="request">The upload request containing metadata and file path.</param>
/// <returns>Result indicating success or failure of the upload.</returns>
public async Task<UploadResult> UploadAsync(UploadRequest request)
{
    // Implementation
}
```

### File Organization

- Core logic: `Core/`
- Models: `Models/`
- MSBuild tasks: `Tasks/`
- Tests: `Datadog.MAUI.SymbolsUpload.Tests/`

## Questions

### Getting Help

- **Documentation**: Start with [DEVELOPMENT.md](DEVELOPMENT.md) and [ARCHITECTURE.md](docs/ARCHITECTURE.md)
- **Issues**: Search existing issues or create a new one
- **Discussions**: Use GitHub Discussions for questions

### Reporting Bugs

Create an issue with:
- Clear description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Environment details (.NET version, OS, MAUI version)
- Relevant logs or error messages

### Suggesting Features

Create an issue with:
- Clear description of the feature
- Use case and motivation
- Proposed implementation (if any)
- Examples from similar tools

## Release Process

For maintainers:

1. Update version in `Datadog.MAUI.SymbolsUpload.csproj`
2. Update CHANGELOG.md with release date
3. Create git tag: `git tag v0.1.0`
4. Push tag: `git push origin v0.1.0`
5. Create GitHub release
6. Build and publish NuGet package
7. Update documentation if needed

## License

By contributing, you agree that your contributions will be licensed under the Apache License 2.0. See [LICENSE](LICENSE) for details.

---

Thank you for contributing to Datadog.MAUI.SymbolsUpload!
