FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MIBO.CalendarAgent/MIBO.CalendarAgent.csproj", "MIBO.CalendarAgent/"]
RUN dotnet restore "MIBO.CalendarAgent/MIBO.CalendarAgent.csproj"
COPY MIBO.CalendarAgent/ MIBO.CalendarAgent/
WORKDIR "/src/MIBO.CalendarAgent"
RUN dotnet build "./MIBO.CalendarAgent.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
WORKDIR "/src/MIBO.CalendarAgent"
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MIBO.CalendarAgent.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MIBO.CalendarAgent.dll"]
