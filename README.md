# BranikBot

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET 10" />
  <img src="https://img.shields.io/badge/C%23-239120?logo=c-sharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white" alt="Docker" />
  <img src="https://img.shields.io/badge/Discord-5865F2?logo=discord&logoColor=white" alt="Discord" />
</p>

<p align="center">
  <img src="https://github.com/michalscepka/branik-bot/actions/workflows/tests.yml/badge.svg" alt="Tests Status" />
</p>

---

**BranikBot** is a Discord bot inspired by the Reddit bot from [r/czech](https://www.reddit.com/r/czech/). It monitors your chat for mentions of money and tells you exactly how many **Braník 2l bottles** you could have bought instead.

<p align="center">
  <a href="https://discord.com/oauth2/authorize?client_id=1200554094708924426">
    <img src="https://img.shields.io/badge/Add%20to%20Server-5865F2?style=for-the-badge&logo=discord&logoColor=white" alt="Add to Server" />
  </a>
</p>

---

## Features

-   **Price Detection**: Parses messages for money amounts in CZK and EUR.
-   **Market Comparison**: Calculates how many Braník 2l bottles, parcels or pallets the amount would buy at the current shop price.
-   **Sampled Prices**: The shop price and the EUR exchange rate are fetched in the background, so a reply never waits for an HTTP call. If a fetch fails, the last real value stays in use.
-   **Price History**: Every price change is stored in a SQLite file that survives restarts.
-   **Not Spammy**: Replies only to some messages, by chance, keeps a cooldown for the whole server after each reply and answers only the largest amount in a message.
-   **Ask on Purpose**: Mention the bot with an amount and it always answers. Reply to someone else's message with just a mention and it answers for that message.
-   **Trusted Bots**: Other bots are ignored by default. A server admin picks the ones to read with a slash command.
-   **Dockerized**: Ready-to-use `docker-compose` setup for both production and local development.

---

## Commands

All commands need the **Manage Server** permission and answer only to the admin who ran them.

| Command | Description |
| :--- | :--- |
| `/branik-config bot-add <bot>` | Start reading the messages of the chosen bot on this server. |
| `/branik-config bot-remove <bot>` | Stop reading the messages of the chosen bot. |
| `/branik-config bot-list` | List the bots whose messages are read on this server. |

Messages written by people are always read. The list is kept per server.

---

## Prerequisites

Before you begin, ensure you have the following:

-   [Docker Desktop](https://www.docker.com/products/docker-desktop)
-   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (if running locally without Docker)
-   A bot application in the [Developer Portal](https://discord.com/developers/applications) with the **Message Content Intent** enabled

---

## Getting Started

Follow these steps to set up the bot:

### 1. Clone the Repository

```bash
git clone https://github.com/michalscepka/branik-bot.git
cd branik-bot
```

### 2. Configure Environment

The only required setting is the bot token. Pass it as an environment variable and keep it out of `appsettings.json`:

| Variable | Description |
| :--- | :--- |
| `Discord__BotToken` | Your bot token from the [Developer Portal](https://discord.com/developers/applications). |

Optional settings, with their defaults from `appsettings.json`:

| Variable | Default | Description |
| :--- | :--- | :--- |
| `Discord__ReplyCooldown` | `00:02:00` | Minimum time between two replies on one server. |
| `Discord__ReplyChance` | `0.25` | Chance that a message with an amount gets a reply (0-1). |
| `Discord__MaxAmountsPerReply` | `1` | How many amounts from one message get an answer, largest first (1-10). |
| `Market__SampleInterval` | `06:00:00` | How often the shop price is fetched. |
| `ExchangeRate__SampleInterval` | `06:00:00` | How often the EUR exchange rate is fetched. |
| `Database__Path` | `data/branik.db` | Location of the SQLite file. |

### 3. Run with Docker

Replace `<BOT_TOKEN>` in `docker-compose.local.yml`, then start the bot:

```bash
docker compose -f docker-compose.local.yml up -d --build
```

The database lives in the `branik-data` volume, so it survives rebuilding the container.

### 4. Run Locally (Development)

Run from the project folder, so `appsettings.json` and the database are found there:

```bash
cd src/BranikBot
Discord__BotToken=<BOT_TOKEN> dotnet run
```

### 5. Run the Tests

```bash
dotnet test src/BranikBot.Tests/
```

---

## Project Structure

Three projects that only reference downwards, plus the tests:

```text
src/
├── BranikBot.Core/          # Pure logic, no package references
│   ├── Parsing/             # Extracting money amounts from text
│   ├── Replies/             # Calculation, reply formatting, cooldown
│   └── Pricing/             # Shared currency and price types
├── BranikBot.Persistence/   # SQLite: price history and trusted bots
├── BranikBot/               # The host
│   ├── Discord/             # Gateway wiring, message handling, slash commands
│   ├── Replies/             # Building a reply from a message
│   ├── Pricing/             # Price and exchange rate providers and samplers
│   ├── Configuration/       # Options bound from appsettings
│   ├── Extensions/          # Dependency injection registration
│   └── Logging/             # Serilog setup
└── BranikBot.Tests/         # Unit tests
```
