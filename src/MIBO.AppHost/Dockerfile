FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
WORKDIR /src/MIBO.AppHost
RUN dotnet restore
RUN dotnet build -c Release
RUN dotnet publish -c Release -o /app/publish --self-contained false
RUN ls -la /app/publish || echo "Publish directory not created"

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MIBO.AppHost.dll"]
