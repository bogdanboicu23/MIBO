FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MIBO.VisualisationService/MIBO.VisualisationService.csproj", "MIBO.VisualisationService/"]
RUN dotnet restore "MIBO.VisualisationService/MIBO.VisualisationService.csproj"
COPY MIBO.VisualisationService/ MIBO.VisualisationService/
WORKDIR "/src/MIBO.VisualisationService"
RUN dotnet build "./MIBO.VisualisationService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
WORKDIR "/src/MIBO.VisualisationService"
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MIBO.VisualisationService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MIBO.VisualisationService.dll"]
