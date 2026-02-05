---
name: Bug Report
about: Report a bug or issue with the plugin
title: '[BUG] '
labels: bug
assignees: ''
---

## Describe the Bug

<!-- A clear and concise description of what the bug is -->

## To Reproduce

Steps to reproduce the behavior:

1. Configure project with '...'
2. Build with '...'
3. See error

## Expected Behavior

<!-- A clear and concise description of what you expected to happen -->

## Actual Behavior

<!-- What actually happened -->

## Environment

- **Plugin Version**: [e.g., 0.1.0]
- **.NET SDK Version**: [e.g., 9.0.101]
- **MAUI Version**: [e.g., 9.0.0]
- **Platform**: [e.g., Android, iOS]
- **OS**: [e.g., macOS 14.0, Windows 11]
- **Build Configuration**: [e.g., Debug, Release]

## Project Configuration

```xml
<!-- Relevant parts of your .csproj configuration -->
<PropertyGroup>
  <DatadogServiceName>...</DatadogServiceName>
  <DatadogVersion>...</DatadogVersion>
  <!-- Add other relevant properties -->
</PropertyGroup>
```

## Build Output / Logs

```
<!-- Paste relevant build output or error messages here -->
<!-- Use -v:diag for detailed output: dotnet build -v:diag > build.log 2>&1 -->
```

## Stack Trace (if applicable)

```
<!-- Paste any stack traces or error details here -->
```

## Additional Context

<!-- Add any other context about the problem here -->
<!-- Screenshots, related issues, workarounds attempted, etc. -->

## Checklist

- [ ] I have searched existing issues to ensure this is not a duplicate
- [ ] I have included all relevant configuration details
- [ ] I have included build output/logs showing the error
- [ ] I have tested with the latest version of the plugin
