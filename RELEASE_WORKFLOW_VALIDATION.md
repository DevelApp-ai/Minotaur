# Release Workflow Validation Guide

This document describes how to validate the NuGet release workflow for the Minotaur project.

## Overview

The CI/CD workflow (`ci-cd-enhanced.yml`) implements the following release strategy:

- **Full Release** (PR merged to main): Release tagged to both GitHub Packages and NuGet.org
- **Beta Release** (PR to main): Release to GitHub Packages only with beta tag
- **Alpha Release** (PR to other branches): Release to GitHub Packages only with alpha tag

## Workflow Changes

### GitVersion Configuration Fix (December 2025)

Fixed the semantic versioning calculation for main branch releases by updating `GitVersion.yml`:

**Problem:** When PRs were merged to main, GitVersion in `ContinuousDeployment` mode generated prerelease versions (e.g., `1.0.0-ci.1`), preventing the stable release job from triggering (which requires `prerelease == ''`).

**Solution:** Changed the main branch configuration in `GitVersion.yml`:

```yaml
main:
  regex: ^master$|^main$
  mode: ContinuousDelivery  # Changed from ContinuousDeployment
  tag: ''                   # Added to prevent prerelease tags
  increment: Patch
  source-branches: ['develop', 'feature', 'support', 'hotfix']
```

This ensures that merges to main generate clean version numbers (e.g., `1.0.0`) without prerelease metadata, allowing the stable release workflow to execute properly.

### Package Job Fix (Previous)

Removed the `if: github.event_name != 'pull_request'` condition from the `package` job in `.github/workflows/ci-cd-enhanced.yml`.

**Before:**
{% raw %}
```yaml
package:
  runs-on: ubuntu-latest
  needs: [calculate-version, build-and-test, code-quality, lint-and-format]
  if: github.event_name != 'pull_request'  # <-- This blocked PR packaging
  outputs:
    package-path: ${{ steps.package.outputs.package-path }}
```
{% endraw %}

**After:**
{% raw %}
```yaml
package:
  runs-on: ubuntu-latest
  needs: [calculate-version, build-and-test, code-quality, lint-and-format]
  # Condition removed - now packages are created for all events
  outputs:
    package-path: ${{ steps.package.outputs.package-path }}
```
{% endraw %}

## Test Scenarios

### Scenario 1: Pull Request to Main (Beta Release)

**Expected Behavior:**
1. Version calculated as `X.Y.Z-beta.PR#` (e.g., `1.0.0-beta.PR123`)
2. Build and test jobs run successfully
3. Package job creates NuGet package with beta version
4. `publish-pr-package` job publishes to GitHub Packages only
5. Release job does NOT run (event is PR, not push)

**Validation Steps:**
1. Create a pull request targeting the `main` branch
2. Wait for CI/CD workflow to complete
3. Check Actions tab for workflow run
4. Verify version in logs: `PreReleaseTag: beta`, `FullSemVer: X.Y.Z-beta.PR#`
5. Verify package artifact is created with beta version
6. Verify package is published to GitHub Packages at `https://github.com/DevelApp-ai/Minotaur/packages`
7. Verify NuGet.org does NOT have the beta package (expected)

### Scenario 2: Pull Request to Other Branches (Alpha Release)

**Expected Behavior:**
1. Version calculated as `X.Y.Z-alpha.PR#` (e.g., `1.0.0-alpha.PR456`)
2. Build and test jobs run successfully
3. Package job creates NuGet package with alpha version
4. `publish-pr-package` job publishes to GitHub Packages only
5. Release job does NOT run (event is PR, not push)

**Validation Steps:**
1. Create a pull request targeting the `develop` branch (or any other branch)
2. Wait for CI/CD workflow to complete
3. Check Actions tab for workflow run
4. Verify version in logs: `PreReleaseTag: alpha`, `FullSemVer: X.Y.Z-alpha.PR#`
5. Verify package artifact is created with alpha version
6. Verify package is published to GitHub Packages
7. Verify NuGet.org does NOT have the alpha package (expected)

### Scenario 3: Merge to Main (Full Release)

**Expected Behavior:**
1. Version calculated as `X.Y.Z` (e.g., `1.0.0` or `1.0.1` - stable release, no prerelease tag)
2. Build and test jobs run successfully
3. Package job creates NuGet package with stable version
4. `release` job runs and:
   - Creates a GitHub Release with tag `vX.Y.Z`
   - Publishes to GitHub Packages
   - Publishes to NuGet.org (may fail on first release - see Troubleshooting)
5. `publish-pr-package` job does NOT run (event is push, not PR)
6. `prerelease` job does NOT run (ref is main, not develop/copilot/feature)

**Validation Steps:**
1. Merge a pull request into the `main` branch
2. Wait for CI/CD workflow to complete
3. Check Actions tab for workflow run
4. Verify version in logs: `PreReleaseTag: (empty)`, `version: X.Y.Z`
5. Verify package artifact is created with stable version
6. Verify GitHub Release is created at `https://github.com/DevelApp-ai/Minotaur/releases`
   - Check that release has tag `vX.Y.Z`
   - Check that release has attached `.nupkg` files
   - Check that release is marked as "Latest" (not pre-release)
7. Verify package is published to GitHub Packages
8. Verify package is published to NuGet.org at `https://www.nuget.org/packages/DevelApp.Minotaur/`
   - **Note**: First release may require manual upload to NuGet.org (see Troubleshooting section)

## Version Calculation Logic

The workflow uses GitVersion for version calculation:

```bash
# For pull requests to main
PRERELEASE="beta"
FULL_VERSION="${VERSION}-beta.PR${PR_NUMBER}"

# For pull requests to other branches
PRERELEASE="alpha"
FULL_VERSION="${VERSION}-alpha.PR${PR_NUMBER}"

# For pushes to main (after PR merge)
PRERELEASE=""  # Empty for stable releases
VERSION="${VERSION}"  # e.g., "1.0.1"
```

## Job Execution Flow

### Pull Request Event (Beta/Alpha)
```
calculate-version → build-and-test → code-quality/lint-and-format → package → publish-pr-package
                                                                               (GitHub Packages only)
```

### Push to Main Event (Full Release)
```
calculate-version → build-and-test → code-quality/lint-and-format → package → release
                                                                               ├─ Create GitHub Release
                                                                               ├─ Publish to GitHub Packages
                                                                               └─ Publish to NuGet.org
```

## Required Secrets

Ensure the following secrets are configured in GitHub repository settings:

- `GITHUB_TOKEN`: Automatically provided by GitHub Actions (for GitHub Packages)
- `NUGET_API_KEY`: NuGet.org API key (for NuGet.org publishing)

## Manual Verification Commands

### Check Package on GitHub Packages
```bash
# List packages for organization repositories
gh api /orgs/DevelApp-ai/packages?package_type=nuget

# List packages for user repositories
# Replace USERNAME with the actual GitHub username
gh api /users/USERNAME/packages?package_type=nuget

# Or visit in browser:
# https://github.com/orgs/DevelApp-ai/packages?repo_name=Minotaur
```

### Check Package on NuGet.org
```bash
# Search for package using NuGet CLI
nuget search DevelApp.Minotaur

# Or visit in browser:
# https://www.nuget.org/packages/DevelApp.Minotaur/
```

### Install Package from Different Sources
```bash
# Install from NuGet.org (full releases only)
dotnet add package DevelApp.Minotaur --version X.Y.Z

# Install from GitHub Packages (all releases)
dotnet add package DevelApp.Minotaur --version X.Y.Z-beta.PR123 \
  --source https://nuget.pkg.github.com/DevelApp-ai/index.json
```

## Troubleshooting

### Package Job Fails on PR
- **Symptom**: Package job is skipped or fails during PR builds
- **Cause**: The `if: github.event_name != 'pull_request'` condition was preventing execution
- **Solution**: This is now fixed - the condition has been removed

### Version Not Calculated Correctly
- **Symptom**: Version doesn't have beta/alpha tag on PRs
- **Cause**: GitVersion configuration or branch reference issue
- **Solution**: Check GitVersion.yml configuration and ensure PR base branch is correctly detected

### Main Branch Creates Prerelease Instead of Stable Release
- **Symptom**: Merge to main creates a prerelease (e.g., `1.0.0-ci.1`) instead of stable version
- **Cause**: GitVersion was using `ContinuousDeployment` mode which adds prerelease metadata
- **Solution**: This is now fixed - main branch uses `ContinuousDelivery` mode with `tag: ''`

### Publishing Fails
- **Symptom**: Package creation succeeds but publishing fails
- **Cause**: Missing or invalid secrets (GITHUB_TOKEN or NUGET_API_KEY)
- **Solution**: Verify secrets are configured in repository settings

### Release Not Created on Merge to Main
- **Symptom**: Merge to main doesn't create a GitHub release
- **Cause**: Version may have a prerelease tag (condition requires empty prerelease tag)
- **Solution**: Verify GitVersion outputs empty PreReleaseTag for main branch

### First NuGet.org Release
- **Symptom**: First release to NuGet.org may fail due to package validation
- **Cause**: NuGet.org requires manual verification for the first release of a new package
- **Solution**: This is expected behavior. After the automated release workflow completes, manually:
  1. Download the `.nupkg` file from the GitHub Release artifacts (e.g., `DevelApp.Minotaur.1.0.0.nupkg`)
  2. Go to [NuGet.org](https://www.nuget.org) and sign in
  3. Click "Upload" and select the `.nupkg` file
  4. Complete the package verification process and publish
  5. Subsequent automated releases from the CI/CD workflow will work normally
  
  **Note**: The `.nupkg` file can also be found in the GitHub Actions workflow artifacts under "nuget-packages"

## Success Criteria

✅ Pull requests to main create beta packages published to GitHub Packages only
✅ Pull requests to other branches create alpha packages published to GitHub Packages only
✅ Merges to main create stable releases with tags published to both GitHub Packages and NuGet.org
✅ All tests pass before packaging and publishing
✅ Code quality and linting checks pass before packaging

## Build and Test Verification

Local verification that the solution builds and tests correctly:

```bash
# Restore dependencies
dotnet restore src/Minotaur.sln

# Build
dotnet build src/Minotaur.sln --configuration Release --no-restore

# Run tests
dotnet test src/Minotaur.sln --configuration Release --no-build

# Create package (test)
dotnet pack src/Minotaur/Minotaur.csproj --configuration Release --no-build \
  --output ./packages -p:PackageVersion=1.0.0-test.1
```

All commands should complete successfully with no errors.
