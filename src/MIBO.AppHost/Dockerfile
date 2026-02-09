FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MIBO.AppHost/MIBO.AppHost.csproj", "MIBO.AppHost/"]
COPY ["MIBO.ApiGateway/MIBO.ApiGateway.csproj", "MIBO.ApiGateway/"]
COPY ["MIBO.CalendarAgent/MIBO.CalendarAgent.csproj", "MIBO.CalendarAgent/"]
COPY ["MIBO.ConversationService/MIBO.ConversationService.csproj", "MIBO.ConversationService/"]
COPY ["MIBO.VisualisationService/MIBO.VisualisationService.csproj", "MIBO.VisualisationService/"]
RUN dotnet restore "MIBO.AppHost/MIBO.AppHost.csproj"
COPY . .
WORKDIR "/src/MIBO.AppHost"
RUN dotnet build "MIBO.AppHost.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
WORKDIR "/src"
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MIBO.AppHost/MIBO.AppHost.csproj" -c $BUILD_CONFIGURATION -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MIBO.AppHost.dll"]
