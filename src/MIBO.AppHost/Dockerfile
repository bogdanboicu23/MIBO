FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
WORKDIR /src/MIBO.AppHost
RUN dotnet restore
RUN dotnet build -c Release -o /app/output

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=build /app/output .
ENTRYPOINT ["dotnet", "MIBO.AppHost.dll"]
