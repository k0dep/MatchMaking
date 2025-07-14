FROM mcr.microsoft.com/dotnet/aspnet:9.0.7-alpine3.22-amd64 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0.302-alpine3.21-amd64 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY MatchMaking.Service/MatchMaking.Service.csproj     MatchMaking.Service/
COPY MatchMaking.Contracts/MatchMaking.Contracts.csproj MatchMaking.Contracts/
COPY MatchMaking.Kafka/MatchMaking.Kafka.csproj         MatchMaking.Kafka/

RUN dotnet restore "MatchMaking.Service/MatchMaking.Service.csproj"

COPY . .
WORKDIR /src/MatchMaking.Service
RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MatchMaking.Service.dll"]