# Datadog.MAUI.SymbolsUpload Makefile
# Common build and test commands

.PHONY: help build test pack clean restore sample-android sample-ios sample-build sample-run-android sample-run-ios sample-publish-android sample-publish-ios sample-publish-ios-device sample-install-android sample-install-ios sample-find-symbols all

# Default target
help:
	@echo "Datadog.MAUI.SymbolsUpload - Available Commands"
	@echo ""
	@echo "Build & Test:"
	@echo "  make build          - Build the plugin"
	@echo "  make test           - Run all tests (unit + integration)"
	@echo "  make test-unit      - Run only unit tests"
	@echo "  make test-int       - Run only integration tests"
	@echo "  make pack           - Create NuGet package"
	@echo ""
	@echo "Sample App - Development:"
	@echo "  make sample-build           - Build sample app (Debug)"
	@echo "  make sample-android         - Build sample app for Android (Debug)"
	@echo "  make sample-ios             - Build sample app for iOS (Debug)"
	@echo "  make sample-run-android     - Build and run on Android device/emulator"
	@echo "  make sample-run-ios         - Build and run on iOS simulator"
	@echo ""
	@echo "Sample App - Release (with symbols):"
	@echo "  make sample-publish-android     - Publish Android Release (generates mapping.txt)"
	@echo "  make sample-publish-ios         - Publish iOS Release for simulator (generates dSYM)"
	@echo "  make sample-publish-ios-device  - Publish iOS Release for device (requires provisioning)"
	@echo "  make sample-install-android     - Install published Android APK"
	@echo "  make sample-install-ios         - Install published iOS app to simulator"
	@echo ""
	@echo "  Add V=1 or VERBOSE=1 to enable verbose logging (e.g., make sample-publish-android V=1)"
	@echo ""
	@echo "Sample App - Utilities:"
	@echo "  make sample-find-symbols    - Search for generated symbol files"
	@echo "  make sample-clean           - Clean sample app build outputs"
	@echo ""
	@echo "Maintenance:"
	@echo "  make clean          - Clean all build outputs"
	@echo "  make restore        - Restore NuGet packages"
	@echo "  make all            - Build everything and run tests"

# Build the plugin
build:
	@echo "Building Datadog.MAUI.SymbolsUpload..."
	dotnet build Datadog.MAUI.SymbolsUpload/Datadog.MAUI.SymbolsUpload.csproj -c Release

# Run all tests
test:
	@echo "Running all tests..."
	dotnet test --nologo

# Run only unit tests
test-unit:
	@echo "Running unit tests..."
	dotnet test --filter "Category!=Integration" --nologo

# Run only integration tests
test-int:
	@echo "Running integration tests..."
	dotnet test --filter "Category=Integration" --nologo

# Create NuGet package
pack:
	@echo "Creating NuGet package..."
	dotnet pack Datadog.MAUI.SymbolsUpload/Datadog.MAUI.SymbolsUpload.csproj -c Release
	@echo ""
	@echo "Package created at:"
	@ls -lh Datadog.MAUI.SymbolsUpload/bin/Release/*.nupkg | tail -1

# Build sample app for all platforms
sample-build: sample-android sample-ios

# Build sample app for Android (Debug)
sample-android:
	@echo "Building sample app for Android (Debug)..."
	dotnet build samples/MauiSampleApp/MauiSampleApp.csproj -f net9.0-android -c Debug

# Build sample app for iOS (Debug)
sample-ios:
	@echo "Building sample app for iOS (Debug)..."
	dotnet build samples/MauiSampleApp/MauiSampleApp.csproj -f net9.0-ios -c Debug

# Publish Android Release (generates mapping.txt and uploads symbols)
sample-publish-android:
	@echo "Publishing Android Release..."
	@if [ -z "$$DATADOG_API_KEY" ]; then \
		echo "⚠️  Warning: DATADOG_API_KEY not set, symbol upload will be skipped"; \
	fi
	dotnet publish samples/MauiSampleApp/MauiSampleApp.csproj \
		-f net9.0-android -c Release \
		$$([ -n "$$DATADOG_API_KEY" ] && echo "-p:DatadogApiKey=$$DATADOG_API_KEY") \
		$$([ -n "$$DATADOG_SITE" ] && echo "-p:DatadogSite=$$DATADOG_SITE") \
		$$([ "$$V" = "1" -o "$$VERBOSE" = "1" ] && echo "-p:DatadogVerbose=true")
	@echo ""
	@echo "✓ Android APK published"
	@echo "To install: make sample-install-android"

# Publish iOS Release for simulator (generates dSYM and uploads symbols)
sample-publish-ios:
	@echo "Publishing iOS Release for simulator..."
	@if [ -z "$$DATADOG_API_KEY" ]; then \
		echo "⚠️  Warning: DATADOG_API_KEY not set, symbol upload will be skipped"; \
	fi
	dotnet build samples/MauiSampleApp/MauiSampleApp.csproj \
		-f net9.0-ios -c Release \
		-p:RuntimeIdentifier=iossimulator-arm64 \
		-p:BuildIpa=true \
		$$([ -n "$$DATADOG_API_KEY" ] && echo "-p:DatadogApiKey=$$DATADOG_API_KEY") \
		$$([ -n "$$DATADOG_SITE" ] && echo "-p:DatadogSite=$$DATADOG_SITE") \
		$$([ "$$V" = "1" -o "$$VERBOSE" = "1" ] && echo "-p:DatadogVerbose=true")
	@echo ""
	@echo "✓ iOS app built for simulator"
	@echo "Note: Simulator builds may not generate dSYM files"
	@echo "To install: make sample-install-ios"

# Publish iOS Release for device (requires provisioning profile)
sample-publish-ios-device:
	@echo "Publishing iOS Release for device..."
	@if [ -z "$$DATADOG_API_KEY" ]; then \
		echo "⚠️  Warning: DATADOG_API_KEY not set, symbol upload will be skipped"; \
	fi
	dotnet publish samples/MauiSampleApp/MauiSampleApp.csproj \
		-f net9.0-ios -c Release \
		-p:RuntimeIdentifier=ios-arm64 \
		$$([ -n "$$DATADOG_API_KEY" ] && echo "-p:DatadogApiKey=$$DATADOG_API_KEY") \
		$$([ -n "$$DATADOG_SITE" ] && echo "-p:DatadogSite=$$DATADOG_SITE") \
		$$([ "$$V" = "1" -o "$$VERBOSE" = "1" ] && echo "-p:DatadogVerbose=true")
	@echo ""
	@echo "✓ iOS app published for device"
	@echo "Note: Install manually via Xcode or other deployment tools"

# Install published Android APK to connected device/emulator
sample-install-android:
	@echo "Installing Android APK..."
	@APK_FILE=$$(find samples/MauiSampleApp/bin/Release/net9.0-android -name "*-Signed.apk" -type f 2>/dev/null | head -1); \
	if [ -z "$$APK_FILE" ]; then \
		echo "❌ No APK found. Run 'make sample-publish-android' first"; \
		exit 1; \
	fi; \
	if ! command -v adb >/dev/null 2>&1; then \
		echo "❌ adb not found in PATH"; \
		exit 1; \
	fi; \
	echo "Installing $$APK_FILE"; \
	adb install -r "$$APK_FILE" && \
	echo "✓ APK installed successfully"

# Install published iOS app to running simulator
sample-install-ios:
	@echo "Installing iOS app to simulator..."
	@if [ "$$(uname)" != "Darwin" ]; then \
		echo "❌ iOS operations require macOS"; \
		exit 1; \
	fi; \
	APP_DIR=$$(find samples/MauiSampleApp/bin/Release/net9.0-ios/iossimulator-arm64 -type d -name "*.app" 2>/dev/null | head -1); \
	if [ -z "$$APP_DIR" ]; then \
		echo "❌ No .app bundle found. Run 'make sample-publish-ios' first"; \
		exit 1; \
	fi; \
	SIMULATOR_ID=$$(xcrun simctl list devices booted | grep -E "iPhone|iPad" | head -1 | sed 's/.*(\\([^)]*\\)).*/\\1/'); \
	if [ -z "$$SIMULATOR_ID" ]; then \
		echo "❌ No booted simulator found. Start a simulator first"; \
		exit 1; \
	fi; \
	echo "Installing to simulator $$SIMULATOR_ID"; \
	xcrun simctl install $$SIMULATOR_ID "$$APP_DIR" && \
	echo "✓ App installed to simulator" && \
	APP_ID=$$(defaults read "$$APP_DIR/Info.plist" CFBundleIdentifier 2>/dev/null || echo "com.datadog.mauisample"); \
	echo "To launch: xcrun simctl launch $$SIMULATOR_ID $$APP_ID"

# Build and run sample app on Android device/emulator
sample-run-android:
	@echo "Building and running sample app on Android..."
	dotnet build samples/MauiSampleApp/MauiSampleApp.csproj -f net9.0-android -c Debug -t:Run

# Build and run sample app on iOS simulator
sample-run-ios:
	@echo "Building and running sample app on iOS simulator..."
	dotnet build samples/MauiSampleApp/MauiSampleApp.csproj -f net9.0-ios -c Debug -t:Run

# Search for generated symbol files
sample-find-symbols:
	@echo "Searching for symbol files in sample app..."
	@echo ""
	@echo "=== Android R8 Mapping Files ==="
	@FILES=$$(find samples/MauiSampleApp -name "mapping.txt" -type f 2>/dev/null); \
	if [ -n "$$FILES" ]; then \
		echo "$$FILES" | while read file; do \
			echo "Found: $$file"; \
			echo "  Size: $$(du -h "$$file" | cut -f1)"; \
			echo "  Modified: $$(stat -f "%Sm" "$$file")"; \
			echo ""; \
		done; \
	else \
		echo "No Android mapping files found"; \
	fi
	@echo ""
	@echo "=== iOS dSYM Bundles ==="
	@FILES=$$(find samples/MauiSampleApp/bin -name "*.dSYM" -type d 2>/dev/null); \
	if [ -n "$$FILES" ]; then \
		echo "$$FILES" | while read file; do \
			echo "Found: $$file"; \
			echo "  Size: $$(du -sh "$$file" | cut -f1)"; \
			echo "  Modified: $$(stat -f "%Sm" "$$file")"; \
			echo ""; \
		done; \
	else \
		echo "No iOS dSYM files found"; \
		echo ""; \
		echo "Checking postprocessing.items for dSYM info..."; \
		PP_FILE=$$(find samples/MauiSampleApp/bin -name "postprocessing.items" -type f 2>/dev/null | head -1); \
		if [ -n "$$PP_FILE" ]; then \
			DSYM_EXISTS=$$(grep -o "<dSYMSourcePathExists>[^<]*</dSYMSourcePathExists>" "$$PP_FILE" 2>/dev/null | sed 's/<[^>]*>//g'); \
			DSYM_NAME=$$(grep -o "<DSymName>[^<]*</DSymName>" "$$PP_FILE" 2>/dev/null | sed 's/<[^>]*>//g'); \
			if [ -n "$$DSYM_EXISTS" ]; then \
				echo "  dSYM generated: $$DSYM_EXISTS"; \
				echo "  Expected name: $$DSYM_NAME"; \
			fi; \
		fi; \
	fi
	@echo ""
	@echo "💡 Tip: Use 'dotnet publish' (not build) to generate dSYM files"
	@echo "   make sample-publish-android  - Generates mapping.txt"
	@echo "   make sample-publish-ios      - Generates dSYM bundle"

# Clean sample app builds
sample-clean:
	@echo "Cleaning sample app..."
	dotnet clean samples/MauiSampleApp/MauiSampleApp.csproj
	rm -rf samples/MauiSampleApp/bin samples/MauiSampleApp/obj

# Clean all build outputs
clean:
	@echo "Cleaning all build outputs..."
	dotnet clean
	rm -rf Datadog.MAUI.SymbolsUpload/bin Datadog.MAUI.SymbolsUpload/obj
	rm -rf Datadog.MAUI.SymbolsUpload.Tests/bin Datadog.MAUI.SymbolsUpload.Tests/obj
	rm -rf samples/MauiSampleApp/bin samples/MauiSampleApp/obj

# Restore NuGet packages
restore:
	@echo "Restoring NuGet packages..."
	dotnet restore

# Build everything and run tests
all: restore build test pack
	@echo ""
	@echo "✓ Build complete!"
	@echo "✓ All tests passed!"
	@echo "✓ NuGet package created!"
