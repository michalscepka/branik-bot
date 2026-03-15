# BranikBot — Claude Code Configuration

## Project

.NET 10.0 Discord bot (C#). Clean Architecture: Domain → Application → Infrastructure → ConsoleApp.

## Build & Test

```bash
dotnet build src/
dotnet test src/BranikBot.Tests/
```

Run tests before proposing a commit. If tests fail, fix the issue first.

## Coding Conventions

- Nullable reference types enabled globally (`Directory.Build.props`)
- C# 13 extension(T) syntax for new extension methods
- Never null! - fix the design instead
- Primary constructors for dependency injection
- Interfaces in Application layer, implementations in Infrastructure
- Test naming: `MethodName_Scenario_Expected` (xUnit + Moq, Arrange-Act-Assert)
- System.Text.Json only - never Newtonsoft.Json
- NuGet versions in Directory.Packages.props only - never in .csproj

## Branch & Commit Conventions

- Branch prefixes: `feat/`, `fix/`, `refactor/`
- Commit messages: Conventional Commits format (e.g., `feat: add cooldown config`)

## Obsidian Vault

Persistent context map across sessions at `~/Documents/Obsidian Vaults/branik-bot/`. Use `/obsidian-load` at session start and `/obsidian-save` at session end.

## Rules

- Do not modify the Dockerfile or CI workflows unless explicitly asked.
- Do not add NuGet packages without asking.
- Keep the Clean Architecture layer boundaries — Domain and Application must not reference Infrastructure.
- Do not change public API contracts (interfaces in Application) without discussing first.
- Security first - when convenience and security conflict, choose security. Deny by default, open selectively.
- No dead code - remove unused imports, variables, functions, files, and stale references in the same commit
- No em dashes - never use — anywhere (code, comments, docs, UI). Use - or rewrite the sentence.
