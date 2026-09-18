# Contributing to Minotaur

Thanks for your interest in contributing! This document covers the basics for issues, branches, commits, and pull requests.

## Getting started

1. Fork the repository and clone your fork.
2. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
3. Restore and build:

   ```bash
   dotnet restore src/Minotaur.sln
   dotnet build src/Minotaur.sln -c Release
   dotnet test src/Minotaur.sln -c Release --no-build
   ```

> **Note:** The `NuGet.config` references a private GitHub Packages feed via the
> `NUGET_AUTH_TOKEN` environment variable. All published dependencies are also
> available on nuget.org, so builds work without the token as long as it is
> absent/ignored.

## Branching

- Branch from `main`.
- Use descriptive, kebab-case branch names prefixed by intent:
  - `feature/<short-description>` — new functionality
  - `fix/<short-description>` — bug fixes
  - `docs/<short-description>` — documentation only
  - `refactor/<short-description>` — restructures without behavior change
  - `vibe/<description>-<suffix>` — AI-assisted contribution branches (the
    suffix keeps parallel attempts unique)
- Avoid long-lived branches; rebase onto `main` before opening a PR.

## Commits

- Use concise, imperative subjects: `Add round-trip integration test`, not
  `added test`.
- Reference the issue number in the body or subject when applicable, e.g.
  `... (#60)`.
- Keep commits focused; one logical change per commit.

## Pull requests

- Open PRs against `main`.
- Fill in the [pull request template](.github/PULL_REQUEST_TEMPLATE.md).
- Link the issue being fixed using GitHub keywords (`Fixes #123`).
- Build and tests must pass locally before requesting review — CI runs
  `dotnet build` and `dotnet test` on `src/Minotaur.sln` plus CodeQL and
  formatting checks.
- New features need tests; bug fixes should include a regression test when
  practical.

## Reporting issues

Use the issue templates ([bug report](.github/ISSUE_TEMPLATE/bug_report.md) /
[feature request](.github/ISSUE_TEMPLATE/feature_request.md)). Include steps
to reproduce, expected vs. actual behavior, and version/commit information.

## Code style

- Follow the existing style; `dotnet format` is enforced in CI
  (`--verify-no-changes`).
- Keep public APIs documented — the build generates documentation files and
  missing-doc warnings are suppressed, but good XML docs are appreciated.

## License

By contributing, you agree that your contributions will be licensed under the
[AGPL-3.0](LICENSE).
