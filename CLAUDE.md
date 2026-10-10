# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

.NET 10 Discord bot (C#, NetCord). It watches chat for money amounts (CZK, EUR) and replies in Czech with how many Braník 2l bottles that would buy.

## Commands

The solution lives in `src/` (`src/BranikBot.slnx`), not the repo root.

```bash
dotnet build src/
dotnet test src/BranikBot.Tests/

# Single test class / single test
dotnet test src/BranikBot.Tests/ --filter "FullyQualifiedName~AmountParserTests"
dotnet test src/BranikBot.Tests/ --filter "FullyQualifiedName~AmountParserTests.MethodName_Scenario_Expected"

# Run the bot locally
dotnet run --project src/BranikBot/BranikBot.csproj

# Run in Docker (build context is ./src)
docker compose -f docker-compose.local.yml up -d --build
```

Running the bot needs a real Discord token in `Discord:BotToken` (env var `Discord__BotToken`). `appsettings.json` only holds the `<BOT_TOKEN>` placeholder. Options are validated on startup (`ValidateDataAnnotations().ValidateOnStart()`), so a missing config value fails the host at boot.

There is no separate lint step: `EnforceCodeStyleInBuild` and `AnalysisLevel=latest` are set in `Directory.Build.props`, so style violations surface in `dotnet build`.

CI: `tests.yml` runs the test project on pushes and PRs to `main`. `build.yml` builds a multi-arch image and pushes it to Docker Hub when a `v*` tag is pushed. That is the release mechanism (`docker-compose.prod.yml` pulls `mscepka/branik-bot:latest`).

## Architecture

A single `BranikBot` project plus `BranikBot.Tests` (xUnit + Moq). The bot has one feature, so code is grouped by what it does rather than by layer:

- `Discord/` - `DiscordBackgroundService` and `MessageHandler` (gateway wiring, bot filtering, cooldown).
- `Parsing/` - `AmountParser` and the `UserInput` record it produces.
- `Pricing/` - `IMarketPriceProvider` and `IExchangeRateProvider` with their HTTP implementations, the samplers (`PriceSampler`, `ExchangeRateSampler`) and stores (`PriceHistoryStore`, `ExchangeRateStore`) that keep their results, plus `Currency`.
- `Storage/` - `SqliteDatabase`, which owns the SQLite file, the schema of every table and opens connections for the stores.
- `Replies/` - `MessageFormatter`, `BranikCalculator` (bottles / parcels of 6 / pallets of 100 parcels), the Czech reply templates in `Messages.cs` and the plural helpers.
- `Configuration/` - options classes bound from appsettings.
- `Logging/` - Serilog bootstrap and host setup.

All DI registration is in `ServiceCollectionExtensions.AddBranikBot()`. If the bot grows a second distinct feature, add feature folders inside the same project instead of new projects.

### Message flow

1. `DiscordBackgroundService` subscribes `MessageHandler.ProcessIncomingMessage` to NetCord's `GatewayClient.MessageCreate`. The gateway needs the `GuildMessages | MessageContent` intents.
2. `MessageHandler` ignores its own messages and all other bots, except the one whose ID is `Discord:EuroChampionId`. For that bot the gateway payload arrives before its content is final, so the handler re-fetches the message over REST through a Polly pipeline (retry every 1s, up to 30 times, while `Flags != 0`) and flattens content + embeds (title, description, fields, footer) into one string.
3. `AmountParser.ExtractAmounts` (regex) turns text into `UserInput` records. It handles a number plus a currency suffix, `k` (thousands) and `mega` (millions) with an optional suffix. No suffix means CZK. Results are de-duplicated via a `HashSet`.
4. Per-channel cooldown (`Discord:ChannelCooldown`) is an in-memory dictionary on the singleton handler. It is checked only after amounts were found, and set only after a reply was sent.
5. `MessageHandler` reads the latest price from `PriceHistoryStore` and the EUR rate from `ExchangeRateStore`, so replying never makes an HTTP call for either. If no price was ever stored, it logs a warning and sends no reply.
6. `MessageFormatter` (static, no dependencies) converts EUR with the rate it is given and leaves EUR amounts out when there is none, so a message with only EUR amounts gets no reply. It picks the reply tier by bottle count: bottles (<100), parcels (<1000), pallets (<100 000), otherwise the "too rich" message. Czech plural forms come from the `extension(int)` block in `StringExtensions`.

Both providers are typed `HttpClient`s and are only called by the samplers.

### Sampled prices

`PriceSampler` is a hosted service that calls `AkcniCenyProvider` once at startup and then every `Market:SampleInterval`. The provider scrapes akcniceny.cz (`//span[@itemprop='lowPrice']`) with HtmlAgilityPack and returns `null` on any failure. There is no fallback price: a failed sample stores nothing, so replies keep using the last real price.

`ExchangeRateSampler` does the same for `CnbExchangeRateProvider` (CNB daily XML feed, returns `0m` on failure) every `ExchangeRate:SampleInterval`. A failed sample stores nothing, so the last real rate stays in use.

Both stores write to the SQLite file at `Database:Path` (`data/branik.db`, a Docker volume in both compose files). The `price_history` table has one row per price period: an unchanged price only moves `last_seen_at` forward, a changed price inserts a new row. The `exchange_rate` table has one row per currency that is overwritten by each sample. New tables go into the schema in `SqliteDatabase`.

### Culture

`Program.cs` sets the default thread culture to `cs-CZ`. This affects `{0:C2}` in the reply templates and culture-sensitive parsing. Parsing of external data must pass an explicit culture (see how the parser and providers use `InvariantCulture` / `cs-CZ`). Tests that assert on formatted output depend on this.

## Coding Conventions

- Nullable reference types are enabled globally (`Directory.Build.props`).
- C# 14 `extension(T)` blocks for new extension methods.
- Never `null!` - fix the design instead. The only existing use is on options classes bound from configuration.
- Primary constructors for dependency injection.
- Interfaces only at real seams (the HTTP providers in `Pricing/`). Do not add an interface for a class with a single implementation that tests do not need to substitute.
- Test naming: `MethodName_Scenario_Expected` (Arrange-Act-Assert).
- System.Text.Json only - never Newtonsoft.Json.
- NuGet versions go in `Directory.Packages.props` only (central package management) - never in a .csproj.
- No dead code - remove unused imports, variables, functions, files and stale references in the same commit.
- No em dashes anywhere (code, comments, docs, UI). Use - or rewrite the sentence.

## Branch & Commit Conventions

- Branch prefixes: `feat/`, `fix/`, `refactor/`
- Commit messages: Conventional Commits (e.g. `feat: add cooldown config`)
- Run tests before proposing a commit. If tests fail, fix the issue first.

## Rules

- Do not modify the Dockerfile or CI workflows unless explicitly asked.
- Do not add NuGet packages without asking.
- Keep `Parsing/` and `Replies/` free of Discord and HTTP dependencies so they stay unit-testable.
- Do not change the provider interfaces in `Pricing/` without discussing first.
- Security first - when convenience and security conflict, choose security. Deny by default, open selectively.
