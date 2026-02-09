FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
WORKDIR /src/MIBO.AppHost
RUN dotnet restore
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish || cp -r /app/build/* /app/publish/ 2>/dev/null || mkdir -p /app/publish && cp -r /app/build/* /app/publish/

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "MIBO.AppHost.dll"]
