# Use the official ASP.NET Core runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared build properties and central package management
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]

# Copy csproj files
COPY ["BranikBot/BranikBot.csproj", "BranikBot/"]
COPY ["BranikBot.Core/BranikBot.Core.csproj", "BranikBot.Core/"]
COPY ["BranikBot.Persistence/BranikBot.Persistence.csproj", "BranikBot.Persistence/"]

# Restore dependencies
RUN dotnet restore "BranikBot/BranikBot.csproj"

# Copy the entire source code
COPY . .

# Build the application. VERSION comes from the git tag in CI.
ARG VERSION=0.0.0-dev
WORKDIR "/src/BranikBot"
RUN dotnet build "BranikBot.csproj" -c Release -o /app/build /p:Version=$VERSION

# Publish stage
FROM build AS publish
ARG VERSION=0.0.0-dev
RUN dotnet publish "BranikBot.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false /p:Version=$VERSION

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "BranikBot.dll"]
