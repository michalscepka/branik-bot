FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["BranikBot/BranikBot.csproj", "BranikBot/"]
RUN dotnet restore "BranikBot/BranikBot.csproj"
COPY . .
WORKDIR "/src/BranikBot"
RUN dotnet build "BranikBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BranikBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "BranikBot.dll"]
