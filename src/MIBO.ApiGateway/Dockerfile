FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MIBO.ApiGateway/MIBO.ApiGateway.csproj", "MIBO.ApiGateway/"]
RUN dotnet restore "MIBO.ApiGateway/MIBO.ApiGateway.csproj"
COPY . .
WORKDIR "/src/MIBO.ApiGateway"
RUN dotnet build "./MIBO.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MIBO.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MIBO.ApiGateway.dll"]
