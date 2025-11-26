# ResultR Agents Guide

This document configures autonomous or semi-autonomous agents (like Cascade) so they follow the house style for this repository.

## 1. Runtime & Language Baseline
- Target **.NET 10** and **C# 14** for any new projects or code files, matching the requirements documented in `README.md`.
- Enable nullable reference types and treat warnings as errors in new project files to maintain strict correctness.
- Prefer the latest SDK project style (`<Project Sdk="Microsoft.NET.Sdk">`) and minimize manual build steps.

## 2. Repository Structure Expectations
- Keep product code under `src/ResultR` and tests under `src/ResultR.Tests`. Add new libraries/apps in sibling folders under `src/`.
- Place general documentation in the repo root or within `docs/`. Use `docs/` for multi-page guides or diagrams.
- When scaffolding new features, update `README.md` or other docs so they remain source-of-truth.

## 3. C# Coding Conventions
1. **Namespaces & Files**
   - Use file-scoped namespaces when possible.
   - Keep one public type per file; co-locate private record/structs when tightly coupled.
2. **Types & Members**
   - Name classes, records, and interfaces using PascalCase; private fields use `_camelCase`. Prefix interface names with "I" (e.g., IUserService).
   - Mark classes `sealed` unless extensibility is required. Use `readonly record struct` or `record class` to model immutable data.
   - Prefer dependency injection via constructors; avoid service locators and statics.
3. **Results & Handlers**
   - All request handlers should return `Result`/`Result<T>` and leverage `Validate`, `OnPreHandle`, and `OnPostHandle` hooks when useful.
   - Surface domain failures via `Result.Failure(...)` instead of exceptions where possible; reserve exceptions for unexpected conditions.
4. **Async & Tasks**
   - Use `async`/`await` end-to-end. Avoid `Task.Result`/`Wait()`.
   - Pass `CancellationToken` through mediator pipelines and to all async dependencies.
5. **Style Enforcement**
   - Run `dotnet format` (with `--verify-no-changes` in CI) before merging.
   - Keep using expression-bodied members for simple properties/methods to reduce noise.

## 4. Dependency Guidelines
- Favor BCL/`Microsoft.Extensions.*` packages. Add third-party dependencies only with a clear justification and lightweight footprint.
- Centralize package versions via `Directory.Packages.props` if the solution grows beyond a few projects.
- Prefer DI registration extensions for handler wiring (`services.AddResultR(...)`).

## 5. Testing & Quality Gates
- Use xUnit (default template) for unit/integration tests in `ResultR.Tests`.
- Follow the Arrange/Act/Assert pattern; favor `Fact` over `Theory` unless multiple datasets are required.
- New functionality should include coverage for success and failure `Result` paths.
- Run `dotnet test` locally before opening a PR. Consider enabling coverage tooling (`coverlet` or `dotnet test --collect:"XPlat Code Coverage"`).

## 6. Documentation & Communication
- Update `README.md` whenever public APIs or requirements change. Mention new hooks, usage patterns, or version support.
- Keep changelogs or release notes in `docs/` if features grow.
- When adding samples, ensure they compile and align with the recommended pipeline (validation → pre-handle → handle → post-handle).

## 7. Pull Request Expectations
- Keep PRs small and focused; describe the ResultR concepts touched (e.g., new handler hook, DI registration change).
- Include before/after context, test evidence, and any migration steps.
- Make sure CI checks (formatting, tests) pass before requesting review.

## 8. General Instructions
- Make only high confidence suggestions when reviewing code changes.
- Write code with good maintainability practices, including comments on why certain design decisions were made.
- Handle edge cases and write clear exception handling.
- For libraries or external dependencies, mention their usage and purpose in comments.

Following these instructions will keep autonomous agents aligned with the repository's C#/.NET standards while evolving ResultR responsibly.
