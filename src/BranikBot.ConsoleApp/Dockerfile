FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["BranikBot.ConsoleApp/BranikBot.ConsoleApp.csproj", "BranikBot.ConsoleApp/"]
RUN dotnet restore "BranikBot.ConsoleApp/BranikBot.ConsoleApp.csproj"
COPY . .
WORKDIR "/src/BranikBot.ConsoleApp"
RUN dotnet build "BranikBot.ConsoleApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BranikBot.ConsoleApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "BranikBot.ConsoleApp.dll"]
