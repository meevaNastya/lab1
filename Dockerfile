# === Stage 1: build & publish ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CarRental.Api/ CarRental.Api/
WORKDIR /src/CarRental.Api
RUN dotnet publish -c Release -o /app/publish

# === Stage 2: runtime ===
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CarRental.Api.dll"]
