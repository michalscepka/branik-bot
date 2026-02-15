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

-   **Price Detection**: Automatically parses messages for various currency formats (CZK, EUR).
-   **Market Comparison**: Fetches current market prices and calculates how much Braník 2l you can buy for the detected amount.
-   **Cooldown System**: Prevents spam by enforcing a cooldown period per channel.
-   **Dockerized**: Ready-to-use `docker-compose` setup for both production and local development.

---

## Prerequisites

Before you begin, ensure you have the following installed:

-   [Docker Desktop](https://www.docker.com/products/docker-desktop)
-   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (if running locally without Docker)

---

## Getting Started

Follow these steps to set up the bot:

### 1. Clone the Repository

```bash
git clone https://github.com/michalscepka/branik-bot.git
cd branik-bot
```

### 2. Configure Environment

Update `appsettings.json` to set the following Environment Variable:

| Variable | Description |
| :--- | :--- |
| `BotToken` | Your bot token from the [Developer Portal](https://discord.com/developers/applications). |

### 3. Run with Docker

You can start the bot using Docker Compose:

```bash
docker compose -f docker-compose.local.yml up -d --build
```

### 4. Run Locally (Development)

Navigate to the project directory and run:

```bash
dotnet run --project src/BranikBot.ConsoleApp/BranikBot.ConsoleApp.csproj
```

---

## Project Structure

The project follows Clean Architecture principles:

```text
src/
├── BranikBot.Domain/         # Core entities, enums, and business logic
├── BranikBot.Application/    # Use cases, provider interfaces, and resources
├── BranikBot.Infrastructure/ # External integrations (Discord, Caching, HTTP Clients)
├── BranikBot.ConsoleApp/     # Entry point and host configuration
└── BranikBot.Tests/          # Unit and Integration tests
```
