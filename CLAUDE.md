# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

.NET 10 Discord bot (C#, NetCord). It watches chat for money amounts (CZK, EUR, USD, GBP, PLN) and replies in Czech with how many Braník 2l bottles that would buy.

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
  - `Replies/` - `MessageFormatter`, `ReplyCooldown`, `ReplyChance`, `BranikCalculator` (bottles / parcels of 6 / pallets of 100 parcels), the Czech reply templates in `Messages.cs` and the plural helpers.
  - `Pricing/` - the shared `Currency` enum and `PricePeriod` record.
- `BranikBot.Persistence` - SQLite access, references Core. `SqliteDatabase` owns the file, the schema of every table and opens connections. Stores such as `PriceHistoryStore` and `TrustedBotStore` only hold queries. It takes plain values (the file path), not options classes.
- `BranikBot` - the host, references both. The project root holds only `Program.cs`. The rest is grouped by what it does:
  - `Discord/` - everything that needs a NetCord type: `DiscordBackgroundService` (gateway wiring), `MessageHandler` (bot filtering and orchestration), `BotMessageReader` and the slash commands in `ConfigCommands`.
  - `Replies/` - `ReplyBuilder`, which has no NetCord dependency. Same namespace as `Replies/` in Core.
  - `Pricing/` - the HTTP providers (`AkcniCenyProvider`, `CnbExchangeRateProvider`), their interfaces `IMarketPriceProvider` and `IExchangeRateProvider` in `Pricing/Abstractions/`, the samplers (`PriceSamplingBackgroundService`, `ExchangeRateSamplingBackgroundService`) and `LatestExchangeRate` (in-memory rate).
  - `Configuration/` - options classes bound from appsettings.
  - `Logging/` - Serilog bootstrap and host setup.
  - `Extensions/` - `ServiceCollectionExtensions`.

All DI registration is in `ServiceCollectionExtensions.AddBranikBot()` in the host. `Program.cs` additionally calls `AddModules` and `UseGatewayEventHandlers` on the built host, which is what discovers the slash command modules and routes interactions to them. A new project needs its `.csproj` added to the `COPY` lines in `src/BranikBot/Dockerfile`, otherwise the image build fails at restore.

### Message flow

1. `DiscordBackgroundService` subscribes `MessageHandler.ProcessIncomingMessage` to NetCord's `GatewayClient.MessageCreate`. The gateway needs the `GuildMessages | MessageContent` intents.
2. `MessageHandler` ignores its own messages and all other bots, except those in `TrustedBotStore` for the guild the message came from (see Trusted bots). A bot can post a placeholder and fill it in later, so for a trusted bot `BotMessageReader` re-fetches the message over REST through a Polly pipeline (retry every 1s, up to 30 times, while the `Loading` flag is set) and flattens content + embeds (title, description, fields, footer) into one string.
3. `ReplyBuilder.Build` turns the text into a reply, or `null` when there is nothing to say. It calls `AmountParser.ExtractAmounts` (regex), which produces `UserInput` records. The parser recognises three shapes. The first is a number followed by a currency suffix, a multiplier or both. Currencies are the `CurrencyDefinitions` table (CZK, EUR, USD, GBP, PLN): suffixes come after the number, and a suffix must end where the word ends, so "100 kcal" is not "100 kc". Multipliers are the `MultiplierDefinitions` table: thousands (`k`, `tisic`, `tis.`, slang `tac`), millions (`mega`, `milion`, `mil.`, `mio`, slang `meloun`) and billions (`miliarda`, `mld`), with their common inflections. The second is slang for a coin or banknote from `DenominationDefinitions` (`dvacka`, `stovka`, `petistovka`, `tisicovka` and so on): the singular is an amount on its own ("dej mi stovku"), the plural only with a number ("5 stovek"), because "stovky lidi" is not money. The third is a currency symbol in front of the number, with an optional multiplier ("$20", "€5k"). Words must match exactly, abbreviations may end with a dot, and a new word is one more entry in a table. The message is searched with diacritics removed, so every word list is written without them and "tisíc", "tisic" and "TISÍC" all match, while the quoted `OriginalText` is cut from the original message. No currency means CZK. Amounts are capped at `MaxAmount`, so absurd numbers cannot overflow. Ambiguous slang is left out on purpose: `kilo`, `litr` and `pade` (which would also match "pádě"). Results are de-duplicated via a `HashSet`.
4. `ReplyBuilder` reads the latest price from `PriceHistoryStore` and the rate of each foreign currency used in the message from `LatestExchangeRate`, so replying never makes an HTTP call for either. A currency without a rate is logged as a warning and left out. If no price was ever stored, it logs a warning and returns `null`.
5. Automatic replies are limited twice, both configured in appsettings for all guilds:
   - Cooldown: `Discord:ReplyCooldown` is the in-memory singleton `ReplyCooldown`, keyed by guild, so a reply in one channel silences the whole server. `MessageHandler` first calls `IsActive` as a cheap early exit, before reading the content, so a guild on cooldown costs no parsing, database read or bot message re-fetch. `TryStart` then checks and starts it in one step, right before the reply is sent, so a failed send still starts the cooldown. Messages without a guild are ignored.
   - Chance: `Discord:ReplyChance` (0-1) is `ReplyChance`. It is rolled only for a message that produced a reply and before `TryStart`, so a skipped message does not start the cooldown. People and trusted bots share the same chance.
6. A message that mentions the bot (which includes a Discord reply to one of its messages) is a deliberate question: it skips the `IsActive` exit, the chance and `TryStart`, so it is always answered and never starts the cooldown. If such a message has no amount of its own and is a Discord reply, the message it replies to is answered instead, under the same rules (not the bot's own message, bots only when trusted).
7. Every answer is sent as a Discord reply to the triggering message, with all mentions disabled so nobody is notified. Replying needs the Read Message History permission: on a 403 the answer is sent again as a plain message. If the original message was deleted, Discord still delivers the answer, unattached.
8. `MessageFormatter` (static, takes everything it needs as arguments) converts each foreign amount with the rate of its currency and leaves it out when there is none, so a message with only such amounts gets no reply. The exchange rate note names the currency through `Messages.GetOneUnit`. It answers only the largest amounts (compared in CZK), at most `Discord:MaxAmountsPerReply` of them (default 1, allowed 1-10), and drops the rest silently, so a message full of prices cannot flood the channel or exceed Discord's 2000 character limit. It picks the reply tier by bottle count: bottles (<100), parcels (<1000), pallets (<100 000), otherwise the "too rich" message. Czech plural forms come from the `extension(int)` block in `StringExtensions`. Each kind of answer (conversion, too little, too rich) has several phrasings in `Messages`, and the formatter picks one per answer with the `Random` it is given. `Random.Shared` is registered in DI, and tests pass a fixed one (`FixedPickRandom`) to know which phrasing they get. A conversion phrasing has one `{0}` for the quantity, which is in the accusative, so a new phrasing must read correctly after it ("Za to máš {0}", not "To je {0}").

Both providers are typed `HttpClient`s and are only called by the samplers, which hold them for the whole process. Their handler is therefore registered with an infinite handler lifetime and a `PooledConnectionLifetime` of 2 minutes, so DNS changes are still picked up. Register any new long-lived client the same way (`ConfigureLongLivedClient`).

### Trusted bots

Which bots are read is configured per guild by its admins, not in appsettings. `ConfigCommands` is the `/branik-config` slash command with the subcommands `bot-add`, `bot-remove` and `bot-list`. It is guild only, requires Manage Server by default (Discord enforces this, and guild admins can re-assign it in the server's integration settings) and always answers ephemerally. The list is the `trusted_bot` table (`guild_id`, `bot_id`), so a bot trusted in one guild is still ignored in every other. Command texts are Czech, like the replies.

The commands are registered globally at startup, which is a REST call: the host fails to start if Discord rejects the token.

### Sampled prices

`PriceSamplingBackgroundService` is a hosted service that calls `AkcniCenyProvider` once at startup and then every `Market:SampleInterval`. The provider scrapes akcniceny.cz (`//span[@itemprop='lowPrice']`) with HtmlAgilityPack and returns `null` on any failure. There is no fallback price: a failed sample stores nothing, so replies keep using the last real price.

`ExchangeRateSamplingBackgroundService` does the same for `CnbExchangeRateProvider` (CNB daily XML feed, returns `null` on failure) every `ExchangeRate:SampleInterval`. One download returns the rate of every member of the `Currency` enum that the feed lists, matched by ISO code. The rates are kept only in memory in `LatestExchangeRate`, one per currency: a failed sample changes nothing, so the last real rates stay in use, and after a restart there is no rate until the first successful sample.

Adding a currency takes three entries: a member of the `Currency` enum named after its ISO code, a line in `CurrencyDefinitions` in the parser, and its name in `Messages.GetOneUnit`.

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
- Never log message content or usernames. Log IDs (message, channel, guild, bot) only.
- `MessageHandler` logs every decision about a message: Information when a message with an amount was skipped by chance or answered, Debug for the per-message noise (cooldown early exit, untrusted bot, nothing to reply to).
