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

Three projects plus `BranikBot.Tests` (xUnit + Moq, references all of them). References only point downwards:

- `BranikBot.Core` - pure logic with no package references and no project references. Its root namespace is `BranikBot`, so namespaces follow the folders (`BranikBot.Parsing`, `BranikBot.Replies`, `BranikBot.Pricing`).
  - `Parsing/` - `AmountParser` and the `UserInput` record it produces.
  - `Replies/` - `MessageFormatter`, `ChannelCooldown`, `BranikCalculator` (bottles / parcels of 6 / pallets of 100 parcels), the Czech reply templates in `Messages.cs` and the plural helpers.
  - `Pricing/` - the shared `Currency` enum and `PricePeriod` record.
- `BranikBot.Persistence` - SQLite access, references Core. `SqliteDatabase` owns the file, the schema of every table and opens connections. Stores such as `PriceHistoryStore` only hold queries. It takes plain values (the file path), not options classes.
- `BranikBot` - the host, references both. The project root holds only `Program.cs`. The rest is grouped by what it does:
  - `Discord/` - everything that needs a NetCord type: `DiscordBackgroundService` (gateway wiring), `MessageHandler` (bot filtering and orchestration) and `EuroChampionMessageReader`.
  - `Replies/` - `ReplyBuilder`, which has no NetCord dependency. Same namespace as `Replies/` in Core.
  - `Pricing/` - the HTTP providers (`AkcniCenyProvider`, `CnbExchangeRateProvider`), their interfaces `IMarketPriceProvider` and `IExchangeRateProvider` in `Pricing/Abstractions/`, the samplers (`PriceSamplingBackgroundService`, `ExchangeRateSamplingBackgroundService`) and `LatestExchangeRate` (in-memory rate).
  - `Configuration/` - options classes bound from appsettings.
  - `Logging/` - Serilog bootstrap and host setup.
  - `Extensions/` - `ServiceCollectionExtensions`.

All DI registration is in `ServiceCollectionExtensions.AddBranikBot()` in the host. A new project needs its `.csproj` added to the `COPY` lines in `src/BranikBot/Dockerfile`, otherwise the image build fails at restore.

### Message flow

1. `DiscordBackgroundService` subscribes `MessageHandler.ProcessIncomingMessage` to NetCord's `GatewayClient.MessageCreate`. The gateway needs the `GuildMessages | MessageContent` intents.
2. `MessageHandler` ignores its own messages and all other bots, except the one whose ID is `Discord:EuroChampionId`. For that bot the gateway payload arrives before its content is final, so `EuroChampionMessageReader` re-fetches the message over REST through a Polly pipeline (retry every 1s, up to 30 times, while `Flags != 0`) and flattens content + embeds (title, description, fields, footer) into one string.
3. `ReplyBuilder.Build` turns the text into a reply, or `null` when there is nothing to say. It calls `AmountParser.ExtractAmounts` (regex), which produces `UserInput` records. The parser handles a number plus a currency suffix, `k` (thousands) and `mega` (millions) with an optional suffix. No suffix means CZK. Results are de-duplicated via a `HashSet`.
4. `ReplyBuilder` reads the latest price from `PriceHistoryStore` and the EUR rate from `LatestExchangeRate`, so replying never makes an HTTP call for either. If no price was ever stored, it logs a warning and returns `null`.
5. Per-channel cooldown (`Discord:ChannelCooldown`) is the in-memory singleton `ChannelCooldown`. `TryStart` checks and starts it in one step, only after a reply was built and before it is sent, so a failed send still starts the cooldown.
6. `MessageFormatter` (static, no dependencies) converts EUR with the rate it is given and leaves EUR amounts out when there is none, so a message with only EUR amounts gets no reply. It picks the reply tier by bottle count: bottles (<100), parcels (<1000), pallets (<100 000), otherwise the "too rich" message. Czech plural forms come from the `extension(int)` block in `StringExtensions`.

Both providers are typed `HttpClient`s and are only called by the samplers, which hold them for the whole process. Their handler is therefore registered with an infinite handler lifetime and a `PooledConnectionLifetime` of 2 minutes, so DNS changes are still picked up. Register any new long-lived client the same way (`ConfigureLongLivedClient`).

### Sampled prices

`PriceSamplingBackgroundService` is a hosted service that calls `AkcniCenyProvider` once at startup and then every `Market:SampleInterval`. The provider scrapes akcniceny.cz (`//span[@itemprop='lowPrice']`) with HtmlAgilityPack and returns `null` on any failure. There is no fallback price: a failed sample stores nothing, so replies keep using the last real price.

`ExchangeRateSamplingBackgroundService` does the same for `CnbExchangeRateProvider` (CNB daily XML feed, returns `null` on failure) every `ExchangeRate:SampleInterval`. The rate is kept only in memory in `LatestExchangeRate`: a failed sample changes nothing, so the last real rate stays in use, and after a restart there is no rate until the first successful sample.

`PriceHistoryStore` writes to the SQLite file at `Database:Path` (`data/branik.db`, a Docker volume in both compose files). The `price_history` table has one row per price period: an unchanged price only moves `last_seen_at` forward, a changed price inserts a new row. The table schema lives in `SqliteDatabase`, not in the store. New tables go into the schema in `SqliteDatabase`.

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
- Keep `BranikBot.Core` free of package and project references (no Discord, HTTP or SQLite) so it stays unit-testable.
- Keep `BranikBot.Persistence` free of Discord and HTTP dependencies.
- Security first - when convenience and security conflict, choose security. Deny by default, open selectively.
- Never log message content or usernames. Log IDs (message, channel) only.
