# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MIBO.ConversationService/MIBO.ConversationService.csproj", "MIBO.ConversationService/"]

COPY ["MIBO.Cache.Redis/MIBO.Cache.Redis.csproj", "MIBO.Cache.Redis/"]
COPY ["MIBO.Storage.Mongo/MIBO.Storage.Mongo.csproj", "MIBO.Storage.Mongo/"]

RUN dotnet restore "MIBO.ConversationService/MIBO.ConversationService.csproj"

COPY . .

WORKDIR "/src/MIBO.ConversationService"
RUN dotnet build "MIBO.ConversationService.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/MIBO.ConversationService"
RUN dotnet publish "MIBO.ConversationService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MIBO.ConversationService.dll"]