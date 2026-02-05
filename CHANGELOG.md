# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial implementation of Datadog.MAUI.Symbols plugin
- Support for Android ProGuard/R8 mapping files
- Support for iOS dSYM bundles
- Support for Flutter symbol files
- Automatic symbol file location
- Git metadata collection (commit SHA, remote URL, tracked files)
- Build ID generation for Android
- Gzip compression for uploads
- MSBuild task integration
- Dry-run mode for testing configuration
- Sample MAUI application demonstrating plugin usage
- Comprehensive documentation (README, DEVELOPMENT, ARCHITECTURE)
- Unit tests (25 tests)
- Integration tests (7 tests)

### Features

- No Node.js dependency - pure .NET implementation
- Direct API communication with Datadog sourcemap intake
- Automatic upload via MSBuild targets
- Configurable via MSBuild properties or environment variables
- Build ID support matching native Gradle task behavior
- User-friendly output messages matching datadog-ci style

## [0.1.0] - TBD

### Added

- Initial release (planned)

[Unreleased]: https://github.com/kyletaylored/datadog-maui-symbols/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/kyletaylored/datadog-maui-symbols/releases/tag/v0.1.0
