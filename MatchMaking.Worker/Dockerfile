FROM mcr.microsoft.com/dotnet/runtime:9.0.7-alpine3.22-amd64 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0.302-alpine3.21-amd64 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MatchMaking.Worker/MatchMaking.Worker.csproj", "MatchMaking.Worker/"]
RUN dotnet restore "MatchMaking.Worker/MatchMaking.Worker.csproj"
COPY . .
WORKDIR "/src/MatchMaking.Worker"
RUN dotnet build "./MatchMaking.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MatchMaking.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MatchMaking.Worker.dll"]
