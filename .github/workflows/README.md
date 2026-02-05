# GitHub Actions Workflows

This directory contains GitHub Actions workflows for CI/CD automation of the Datadog.MAUI.Symbols project.

## Workflows

### 1. Test (`test.yml`)

**Triggers:**
- Push to `main`/`master` branch
- Pull requests to `main`/`master` branch

**What it does:**
- Runs tests on Ubuntu, macOS, and Windows
- Tests with .NET 9.0
- Runs unit tests (excludes integration tests)
- Uploads test results as artifacts

**Usage:**
Automatic - runs on every push and PR.

---

### 2. Build and Release (`build-release.yml`)

**Triggers:**
- Push of version tags (e.g., `v0.1.0`)
- Manual workflow dispatch

**What it does:**
- Builds the project in Release configuration
- Runs tests to verify quality
- Creates NuGet packages with specified version
- Generates SHA256 checksums
- Creates a GitHub Release with packages and checksums attached

**Usage:**

#### Option 1: Create a tag (Recommended)
```bash
git tag v0.1.0
git push origin v0.1.0
```

#### Option 2: Manual dispatch
1. Go to Actions → "Build and Release"
2. Click "Run workflow"
3. Enter version (e.g., `0.1.0`)
4. Click "Run workflow"

**Output:**
- GitHub Release with NuGet packages
- Packages ready for publishing to NuGet.org

---

### 3. Publish to NuGet (`publish-to-nuget.yml`)

**Triggers:**
- Manual workflow dispatch only

**What it does:**
- Downloads packages from a GitHub Release
- Validates package structure and metadata
- Optionally publishes to NuGet.org (when not in dry-run mode)

**Usage:**

1. **First, create a release** using the "Build and Release" workflow
2. **Then publish to NuGet.org:**
   - Go to Actions → "Publish to NuGet"
   - Click "Run workflow"
   - Enter the release tag (e.g., `v0.1.0`)
   - Check "Dry run" to validate first (recommended)
   - Click "Run workflow"
3. **After dry run succeeds:**
   - Run workflow again
   - **Uncheck** "Dry run"
   - Packages will be published to NuGet.org

**Prerequisites:**
- `NUGET_API_KEY` secret must be configured in repository settings

---

### 4. Validate Version (`validate-version.yml`)

**Triggers:**
- Called by other workflows (reusable workflow)

**What it does:**
- Validates version format (vX.Y.Z)
- Checks that the GitHub Release exists

**Usage:**
Not called directly - used by `publish-to-nuget.yml`.

---

## Complete Release Process

### Step 1: Create a Release

**Option A: Via Git Tag (Recommended)**
```bash
# Create and push a version tag
git tag v0.1.0 -m "Release v0.1.0"
git push origin v0.1.0
```

The workflow will:
1. Build the project
2. Run tests
3. Create NuGet packages
4. Create a GitHub Release

**Option B: Via GitHub UI**
1. Go to Actions → "Build and Release"
2. Click "Run workflow"
3. Enter version number (e.g., `0.1.0`)
4. Click "Run workflow"

### Step 2: Publish to NuGet.org

1. **Dry Run First** (recommended):
   - Go to Actions → "Publish to NuGet"
   - Click "Run workflow"
   - Enter: `v0.1.0` (the release tag)
   - **Check** "Dry run"
   - Click "Run workflow"
   - Wait for validation to complete

2. **Publish for Real**:
   - Go to Actions → "Publish to NuGet"
   - Click "Run workflow"
   - Enter: `v0.1.0` (same release tag)
   - **Uncheck** "Dry run"
   - Click "Run workflow"
   - Packages will be published to NuGet.org

3. **Verify**:
   - Packages appear on NuGet.org within 15-30 minutes
   - Check: https://www.nuget.org/packages/Datadog.MAUI.Symbols/

---

## Environment Setup

### Required Secrets

Configure in **Settings → Secrets and variables → Actions**:

- `NUGET_API_KEY` - Your NuGet.org API key
  - Get from: https://www.nuget.org/account/apikeys
  - Required permissions: Push new packages

### Environment Protection (Optional)

For the `publish-to-nuget.yml` workflow, you can add environment protection:

1. Go to **Settings → Environments**
2. Create environment: `nuget-production`
3. Add protection rules:
   - Required reviewers (optional)
   - Wait timer (optional)

---

## Workflow Files

| File | Purpose | Trigger |
|------|---------|---------|
| `test.yml` | Run tests on PRs and pushes | Automatic |
| `build-release.yml` | Build and create releases | Tag push or manual |
| `publish-to-nuget.yml` | Publish to NuGet.org | Manual only |
| `validate-version.yml` | Version validation helper | Called by other workflows |

---

## Examples

### Release v0.1.0

```bash
# 1. Create and push tag
git tag v0.1.0 -m "Initial release"
git push origin v0.1.0

# 2. Wait for "Build and Release" workflow to complete
# 3. Go to Actions → "Publish to NuGet"
#    - Enter: v0.1.0
#    - Check "Dry run"
#    - Run workflow

# 4. After dry run succeeds, run again with:
#    - Enter: v0.1.0
#    - Uncheck "Dry run"
#    - Run workflow
```

### Release v0.2.0 (patch release)

```bash
# Same process
git tag v0.2.0 -m "Bug fixes and improvements"
git push origin v0.2.0

# Then use "Publish to NuGet" workflow
```

---

## Troubleshooting

### Build fails
- Check .NET version compatibility
- Verify tests pass locally: `dotnet test`
- Review workflow logs

### Publish fails with "Package already exists"
- NuGet.org doesn't allow republishing the same version
- Increment version number and create new release

### "Invalid version format" error
- Version tags must be in format: `vX.Y.Z`
- Examples: `v0.1.0`, `v1.2.3`
- NOT: `0.1.0`, `v1.2`, `release-1.0`

### NuGet.org shows old version
- Wait 15-30 minutes for package indexing
- Clear your NuGet cache: `dotnet nuget locals all --clear`

---

## Version Naming

Follow Semantic Versioning (semver.org):

- **v0.1.0** - Initial release
- **v0.1.1** - Patch (bug fixes)
- **v0.2.0** - Minor (new features, backward compatible)
- **v1.0.0** - Major (first stable release or breaking changes)
