# Use the official ASP.NET Core runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["BranikBot.ConsoleApp/BranikBot.ConsoleApp.csproj", "BranikBot.ConsoleApp/"]
RUN dotnet restore "BranikBot.ConsoleApp/BranikBot.ConsoleApp.csproj"

# Copy the entire source code
COPY . .

# Build the application
WORKDIR "/src/BranikBot.ConsoleApp"
RUN dotnet build "BranikBot.ConsoleApp.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "BranikBot.ConsoleApp.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "BranikBot.ConsoleApp.dll"]
