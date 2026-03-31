# Contributing to Chrono

Thank you for your interest in contributing to Chrono! This guide explains how to contribute effectively and how our automated release pipeline works.

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- Git

### Build & Test

```bash
# Build
dotnet build Chrono.slnx

# Run tests
dotnet test tests/Chrono.TimeSeries.Test/Chrono.TimeSeries.Test.csproj

# Run benchmarks
dotnet run --project benchmarks/Chrono.TimeSeries.Benchmark/Chrono.TimeSeries.Benchmark.csproj -c Release
```

## Commit Message Convention

This project uses [Conventional Commits](https://www.conventionalcommits.org/) to automate versioning and changelog generation. **PR titles must follow this format** since we use squash merging.

### Format

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Types

| Type | Description | Version Bump |
|------|-------------|--------------|
| `feat` | A new feature | Minor (0.x.0) |
| `fix` | A bug fix | Patch (0.0.x) |
| `docs` | Documentation only changes | — |
| `style` | Code style changes (formatting, semicolons, etc.) | — |
| `refactor` | Code change that neither fixes a bug nor adds a feature | — |
| `perf` | Performance improvement | Patch (0.0.x) |
| `test` | Adding or updating tests | — |
| `build` | Changes to build system or dependencies | — |
| `ci` | Changes to CI configuration | — |
| `chore` | Other changes that don't modify src or test files | — |
| `revert` | Reverts a previous commit | — |

### Breaking Changes

For breaking changes, add `!` after the type/scope or include `BREAKING CHANGE:` in the footer:

```
feat!: redesign storage API

BREAKING CHANGE: FixedSlotTimeSeries constructor now requires a TimeZoneInfo parameter.
```

Breaking changes trigger a **major** version bump (or minor bump while version is < 1.0.0).

### Examples

```
feat: add quarterly aggregation period
fix(storage): handle duplicate timestamps in SortedArrayTimeSeries
docs: update getting started guide
perf: use SIMD for decimal operations
test: add edge case tests for empty series arithmetic
chore: update benchmark dependencies
feat(aggregation)!: rename Sum to Aggregate with strategy parameter
```

## Pull Request Process

1. **Fork and branch** — create a feature branch from `main`
2. **Make your changes** — write code, add tests
3. **Ensure tests pass** — run `dotnet test` locally
4. **Open a PR** — the PR title must follow conventional commit format (this is enforced by CI)
5. **Squash merge** — all PRs are squash-merged to `main`, so the PR title becomes the commit message

### CI Checks

All PRs must pass:
- **CI** — build and test (`.github/workflows/ci.yml`)
- **PR Title Lint** — validates PR title follows conventional commits (`.github/workflows/pr-title-lint.yml`)

## How Releases Work

We use [release-please](https://github.com/googleapis/release-please) to fully automate releases:

1. When PRs with `feat:` or `fix:` titles are merged to `main`, release-please automatically creates (or updates) a **Release PR**
2. The Release PR bumps the version in `Chrono.TimeSeries.csproj`, updates `CHANGELOG.md`, and shows what's included
3. When the Release PR is merged, release-please automatically:
   - Creates a **GitHub Release** with release notes
   - Tags the commit with the version (e.g., `v1.2.3`)
   - Triggers the NuGet publish workflow to push the package to [nuget.org](https://www.nuget.org/)

### Version Bump Rules

| Commit Type | Example | Version Change |
|-------------|---------|----------------|
| `fix:` | `fix: handle null timestamps` | `0.1.0` → `0.1.1` |
| `feat:` | `feat: add weekly period` | `0.1.0` → `0.2.0` |
| `feat!:` or `BREAKING CHANGE` | `feat!: new storage API` | `0.1.0` → `0.2.0`* |

\* While version is below 1.0.0, breaking changes bump minor. After 1.0.0, they bump major.

## Optional: Local Commit Hook

For faster feedback, you can install a local Git hook that validates commit messages before they're created:

```bash
# Linux / macOS
./scripts/setup-hooks.sh

# Windows (PowerShell)
.\scripts\setup-hooks.ps1
```

This is optional — the CI will catch any issues regardless — but it saves a round-trip if you write conventional commits directly.

## Repository Branch Protection

Maintainers should ensure the following branch protection rules are enabled on `main`:

- ✅ Require pull request reviews before merging
- ✅ Require status checks to pass: `build-and-test`, `validate-pr-title`
- ✅ Allow only squash merging
- ✅ Require linear history
