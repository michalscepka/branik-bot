# BranikBot

BranikBot is a Discord bot designed to detect prices in server chat messages and convert them against the current price of Braník 2l petlahev.

## Features

-   **Price Detection**: Automatically parses messages for various currency formats (EUR, CZK).
-   **Market Comparison**: Fetches current market prices and calculates how much Braníčků you can buy for the detected amount.
-   **Cooldown System**: Prevents spam by enforcing a cooldown period per channel.
-   **Dockerized**: Ready-to-use `docker-compose` setup for both production and local development.
-   **Logging**: Structured logging with Serilog.

## Prerequisites

Before you begin, ensure you have the following installed:

-   [Docker Desktop](https://www.docker.com/products/docker-desktop)
-   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (if running locally without Docker)

## Getting Started

Follow these steps to set up the bot:

### 1. Clone the Repository

```bash
git clone <your-repo-url>
cd branik-bot
```

### 2. Configure Environment

The application uses `appsettings.json` and environment variables. You may need to set up your Discord Bot Token and other configurations.

Check `src/BranikBot.ConsoleApp/appsettings.json` for structure. You can override these with environment variables or user secrets.

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

## Project Structure

```text
src/
├── BranikBot.ConsoleApp/       # Entry point, host configuration
├── BranikBot.Infrastructure/   # Core logic, services, parsing helpers
├── BranikBot.Tests/            # Unit tests
└── Logging/                    # Custom logging configuration
```

## Development

### Running Tests

To execute the test suite:

```bash
dotnet test
```
