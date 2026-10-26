# AGENTS.md

This file provides guidance to working with code in this repository.

## Shared AI Harness Rules

This project uses the **central AI harness** for consistent conventions across godot-gdunit-labs projects.

Shared rules are defined in `../ai-harness-core/shared-rules/`:

- **[Security](../ai-harness-core/shared-rules/security.md)** — Secret handling, prompt injection, code safety
- **[Bot Conventions](../ai-harness-core/shared-rules/bot-conventions.md)** — Bot identity, CI/CD automation
- **[Commit Style](../ai-harness-core/shared-rules/commit-style.md)** — Commit message format
- **[PR Style](../ai-harness-core/shared-rules/pr-style.md)** — Pull request title/body format
- **[C# Style](../ai-harness-core/shared-rules/csharp-style.md)** — Formatting, naming, the enforced ruleset
- **[Documentation Style](../ai-harness-core/shared-rules/documentation-style.md)** — Markdown, structure, tone
- **[Git Branching](../ai-harness-core/shared-rules/git-branching.md)** — Branch naming and flow
- Agent memory conventions: `../ai-harness-core/shared-rules/agent-memory-conventions.md`

All code changes must follow these shared rules. When in doubt, consult the appropriate shared rule.

## What This Project Is

GdUnit4Net is the C# implementation of GdUnit4: a unit testing framework for Godot C# projects that is fully
compatible with the VSTest standard. It ships as three NuGet packages: `gdUnit4.api`, `gdUnit4.test.adapter`,
and `gdUnit4.analyzers`.

Supported Godot versions: the versions in the test matrix of `.github/workflows/ci-pr.yml`
(`Godot.NET.Sdk` from `global.json`). Package versions, the Godot version, target frameworks, and the
language version are defined in `ProjectVersions.props` (single source); .NET SDK pinned in `global.json`.
Keep `global.json` `msbuild-sdks.Godot.NET.Sdk` in sync with `GodotVersion`.

## Commands

### Running Tests

```bash
# Analyzer and adapter tests (no Godot runtime)
dotnet test Analyzers.Test --settings Analyzers.Test/.runsettings
dotnet test TestAdapter.Test --settings TestAdapter.Test/.runsettings

# API tests (start the Godot runtime)
dotnet test Api.Test --settings Api.Test/.runsettings

# Filter by name
dotnet test Api.Test --settings Api.Test/.runsettings --filter "FullyQualifiedName~StringAssertTest"
```

`Api.Test`, `Example` and `Example.MultiProject/Game.Test` are Godot projects whose tests start the Godot runtime.
Their `.runsettings` set `GODOT_BIN` to the Godot Mono binary (`EnvironmentVariables`); adjust that path to your machine.
Annotate tests that touch Godot features (nodes, scenes, resources) with `[RequireGodotRuntime]`.

### C# Build and Format

```bash
# Build
dotnet build

# Verify formatting (CI-style check, no changes applied), per project as in .github/workflows/ci-pr.yml
dotnet format Api/GdUnit4Api.csproj --verify-no-changes --verbosity diagnostic
dotnet format TestAdapter/GdUnit4TestAdapter.csproj --verify-no-changes --verbosity diagnostic
dotnet format TestAdapter.Test/GdUnit4TestAdapter.Tests.csproj --verify-no-changes --verbosity diagnostic
dotnet format Analyzers/GdUnit4Analyzers.csproj --verify-no-changes --verbosity diagnostic
dotnet format Analyzers.Test/GdUnit4Analyzers.Tests.csproj --verify-no-changes --verbosity diagnostic
dotnet format Api.Test/GdUnit4ApiTest.csproj --verify-no-changes --verbosity diagnostic
dotnet format Example/ExampleProject.csproj --verify-no-changes --verbosity diagnostic
dotnet format Example.MultiProject/Game/Game.csproj --verify-no-changes --verbosity diagnostic
dotnet format Example.MultiProject/Game.Test/Game.Test.csproj --verify-no-changes --verbosity diagnostic
```

Test projects relax rules that do not fit test code through their `test.ruleset`. `Example` is the consumer
sample: it follows the `.editorconfig` but does not use the repository build settings, so it stays copyable.

Many `Api.Test` tests assert source line numbers (`HasFileLineNumber`, expected stack traces, exception
attributes). Moving lines in a test file means updating those numbers, so run the `Api.Test` suite after
formatting changes.

### C# Linting

No standalone linter. StyleCop.Analyzers and the IDE code-style rules run during `dotnet build`
(`EnforceCodeStyleInBuild = true`, `TreatWarningsAsErrors = true`, see `Directory.Build.props`);
the build must finish without errors.

### Markdown Linting

The repository has no markdownlint configuration and CI does not lint Markdown yet. Lint changed
Markdown locally with the gdUnit4 configuration:

```bash
markdownlint-cli2 --config ../gdUnit4/.github/actions/formatting_checks/.markdownlint.jsonc "**/*.md"
```

## Architecture

### Solution Structure

| Project | Package | Purpose |
| ------- | ------- | ------- |
| `Api/` | `gdUnit4.api` | Test API: attributes, fluent assertions, scene runner, test engine |
| `TestAdapter/` | `gdUnit4.test.adapter` | VSTest adapter: discovery and execution in IDEs and `dotnet test` |
| `Analyzers/` | `gdUnit4.analyzers` | Roslyn analyzers that validate test attributes at compile time |
| `Example/` | — | Example Godot project consuming the packages |
| `Example.MultiProject/` | — | Example of a game and a separate test project referencing it (`Game`, `Game.Test`) |

### Source Modules

| Directory | Purpose |
| --------- | ------- |
| `Api/src/api/` | Public engine contracts (`ITestEngine`, `ITestEventListener`, test nodes, settings) |
| `Api/src/asserts/` | Fluent assertion implementations |
| `Api/src/core/` | Engine, discovery, execution, hooks, logging, scene runner |
| `TestAdapter/src/` | VSTest discoverer, executor, debugger frameworks, settings |
| `Analyzers/src/` | Analyzers and diagnostic rules |

### Test Organization

Each project has its own test project: `Api` → `Api.Test`, `TestAdapter` → `TestAdapter.Test`,
`Analyzers` → `Analyzers.Test`.

## Coding Style

All code, comments, and documentation are written in **English**.

**C#:** Follow the shared rule [C# Style](../ai-harness-core/shared-rules/csharp-style.md); the ruleset
is the repository `.editorconfig`. StyleCop.Analyzers enforced, `TreatWarningsAsErrors = true`, nullable
reference types enabled, language version from `ProjectVersions.props`.

This repository deviates from the shared rule in two points; the `.editorconfig` wins:

- **`var`** is used everywhere (IDE0007), not only when the type is apparent.
- **Private fields** are `camelCase` without the `_` prefix.

The file header is the `file_header_template` from `.editorconfig`; copy it from a neighbouring file.

Do not skip linting or proceed without it.

## Godot API Compatibility

All Godot API usage must be compatible with every Godot version in the CI test matrix. If an API is not
available in all of them, it cannot be used without a version guard. When uncertain, check the
[Godot class reference changelog](https://docs.godotengine.org/en/stable/classes/) or the Godot GitHub commit history.

## Testing Requirements

Every code change must be accompanied by new or updated tests in the matching test project
(see [Test Organization](#test-organization)).

## Documentation

The user documentation of GdUnit4Net lives in the gdUnit4 repository, a Jekyll site under
`../gdUnit4/documentation/`: C# setup and the VSTest adapter in `documentation/doc/_csharp_project_setup/`
(`csharp-setup.md`, `vstest-adapter.md`). Update it there when a change affects users.

All documentation content must follow the shared rule `../ai-harness-core/shared-rules/documentation-style.md`.

## Branch and PR Conventions

- Main branch: `master`
- Feature branches should be named after the issue number (e.g., `GD-111`)
- PRs must link to an issue and pass all CI checks before merging

### Before Pushing

Run all linting locally before pushing to avoid CI failures:

```bash
# C# build (code style enforced)
dotnet build

# C# format check of the changed projects
dotnet format <project>.csproj --verify-no-changes --verbosity diagnostic

# Markdown lint (if Markdown files changed)
markdownlint-cli2 --config ../gdUnit4/.github/actions/formatting_checks/.markdownlint.jsonc "**/*.md"
```
