# Build vs Publish for Symbol Generation

## Key Difference

**IMPORTANT**: Symbol files are only generated during `dotnet publish`, not `dotnet build`.

## Commands

### Development (Debug Builds)
Use `dotnet build` for fast iteration during development:

```bash
# Quick development builds (no symbols needed)
dotnet build -c Debug -f net9.0-android
dotnet build -c Debug -f net9.0-ios

# Or use Makefile
make sample-android  # Android Debug
make sample-ios      # iOS Debug
```

### Release (Production Builds)
Use `dotnet publish` to generate symbol files:

```bash
# Generate mapping.txt for Android
dotnet publish -c Release -f net9.0-android

# Generate dSYM for iOS
dotnet publish -c Release -f net9.0-ios

# Or use Makefile
make sample-publish-android  # Generates mapping.txt
make sample-publish-ios      # Generates dSYM bundle
```

## What Gets Generated

### Android Publish
- **APK**: `bin/Release/net9.0-android/*-Signed.apk`
- **Mapping File**: `bin/Release/net9.0-android/mapping.txt`
- **Plugin Action**: Automatically uploads mapping.txt to Datadog

### iOS Publish
- **App Bundle**: `bin/Release/net9.0-ios/ios-arm64/*.app` (device) or `bin/Release/net9.0-ios/iossimulator-arm64/*.app` (simulator)
- **dSYM Bundle**: `bin/Release/net9.0-ios/ios-arm64/*.app.dSYM` (device only)
- **Plugin Action**: Automatically uploads dSYM to Datadog

**Important**: iOS simulator builds typically do not generate dSYM files. For dSYM generation and upload:
- Use device builds with `dotnet publish -p:RuntimeIdentifier=ios-arm64`
- Requires Apple Developer account and provisioning profile
- For local testing without provisioning, use Android or test with dry-run mode

## Workflow

### Complete Release Workflow

```bash
# 1. Build the plugin
make build

# 2. Publish Android with symbols
make sample-publish-android

# 3. Install to device/emulator
make sample-install-android

# 4. Publish iOS with symbols
make sample-publish-ios

# 5. Install to simulator
make sample-install-ios

# 6. Verify symbols were generated
make sample-find-symbols
```

### Separate Build and Install

The Makefile now separates building from installing:

```bash
# Publish once (slow, generates symbols, uploads to Datadog)
make sample-publish-ios

# Install multiple times (fast, no rebuild)
make sample-install-ios
make sample-install-ios  # Install again to different simulator
```

## Why Build Doesn't Generate Symbols

- **Build**: Compiles code, produces intermediate outputs
- **Publish**: Creates deployable artifacts, applies optimizations, generates debug symbols

For Release builds:
- Android R8 runs during publish to shrink/obfuscate code and generate mapping.txt
- iOS dSYM extraction happens during publish to create debug symbols

## Checking Symbol Generation Status

### Using postprocessing.items

After an iOS build or publish, check the postprocessing status:

```bash
# Find and check postprocessing file
find . -name "postprocessing.items" -exec grep -H "dSYMSourcePathExists" {} \;

# Example output:
# samples/.../postprocessing.items:<dSYMSourcePathExists>true</dSYMSourcePathExists>
```

- `true` = dSYM was generated ✓
- `false` = dSYM was not generated (need to use dotnet publish) ✗

### Using Makefile

```bash
# Quick check for all symbol files
make sample-find-symbols

# Output shows:
# - Android mapping files (if any)
# - iOS dSYM bundles (if any)
# - postprocessing.items status
# - Helpful tips
```

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Publish Android Release
  run: dotnet publish -c Release -f net9.0-android
  env:
    DATADOG_API_KEY: ${{ secrets.DATADOG_API_KEY }}

- name: Publish iOS Release
  run: dotnet publish -c Release -f net9.0-ios
  env:
    DATADOG_API_KEY: ${{ secrets.DATADOG_API_KEY }}
```

### Dry Run in CI

Test without actually uploading:

```yaml
- name: Test Symbol Upload (Dry Run)
  run: |
    dotnet publish -c Release -f net9.0-android \
      -p:DatadogSymbolUploadDryRun=true \
      -p:DatadogApiKey=test-key
```

## Troubleshooting

### "Symbol file not found" after build

**Problem**: Ran `dotnet build` instead of `dotnet publish`

**Solution**: Use `dotnet publish` for Release builds

```bash
# Wrong (symbols not generated)
dotnet build -c Release -f net9.0-android

# Correct (symbols generated)
dotnet publish -c Release -f net9.0-android
```

### iOS: "Could not find any available provisioning profiles"

**Problem**: Trying to publish iOS app for device without provisioning profile

**Solutions**:

1. **For local testing (recommended)**: Use Android instead
   ```bash
   make sample-publish-android  # No provisioning needed
   ```

2. **For simulator testing**: Use build instead of publish (but won't generate dSYM)
   ```bash
   dotnet build -c Release -f net9.0-ios -p:RuntimeIdentifier=iossimulator-arm64
   ```

3. **For device deployment**: Set up provisioning profile in `.csproj`
   ```xml
   <PropertyGroup Condition="$(TargetFramework.Contains('ios'))">
     <CodesignKey>Apple Development</CodesignKey>
     <CodesignProvision>Your Provisioning Profile Name</CodesignProvision>
     <DevelopmentTeam>YOUR_TEAM_ID</DevelopmentTeam>
   </PropertyGroup>
   ```

   Find your provisioning profiles:
   ```bash
   # List available provisioning profiles
   security find-identity -v -p codesigning

   # View installed provisioning profiles
   ls ~/Library/MobileDevice/Provisioning\ Profiles/
   ```

4. **Create provisioning profile**:
   - Sign in to [Apple Developer Portal](https://developer.apple.com/)
   - Go to Certificates, Identifiers & Profiles
   - Create new provisioning profile for your app
   - Download and install (double-click to install)

### dSYM not generated even with publish

**Problem**: iOS simulator builds do not generate dSYM files

**Solutions**:
1. Check `MtouchDebugSymbols` and `GenerateDsymBundle` are set
2. **Use device build** (not simulator):
   ```bash
   make sample-publish-ios-device  # Requires provisioning profile
   ```
3. For testing without device provisioning, use Android:
   ```bash
   make sample-publish-android  # Generates mapping.txt, no provisioning needed
   ```
4. Check postprocessing.items for dSYM generation status:
   ```bash
   make sample-find-symbols
   ```

## Performance Notes

- `dotnet build`: Fast (~30 seconds)
- `dotnet publish`: Slower (~2-20 minutes depending on platform and app size)
- Installing already-published app: Very fast (~5-10 seconds)

This is why the Makefile separates publish from install!

## See Also

- [README.md](../README.md) - Main documentation
- [SYMBOL_FILES.md](SYMBOL_FILES.md) - Symbol file management guide
- [ARCHITECTURE.md](ARCHITECTURE.md) - Technical details
