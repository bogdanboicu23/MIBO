FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MIBO.ActionService/MIBO.ActionService.csproj", "MIBO.ActionService/"]
RUN dotnet restore "MIBO.ActionService/MIBO.ActionService.csproj"

COPY . .

WORKDIR "/src/MIBO.ActionService"
RUN dotnet build "MIBO.ActionService.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/MIBO.ActionService"
RUN dotnet publish "MIBO.ActionService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MIBO.ActionService.dll"]
