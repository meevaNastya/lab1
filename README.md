# Car Rental

Backend-система аренды автомобилей на ASP.NET Core Web API.

## Требования

- .NET 8 SDK
- PostgreSQL

## Запуск

1. Скопировать `CarRental.Api/appsettings.Development.example.json`
   в `CarRental.Api/appsettings.Development.json` и указать свои данные.
2. Применить миграцию:

```powershell
dotnet ef database update --project CarRental.Api
```

3. Запустить приложение:

```powershell
dotnet run --project CarRental.Api
```

Swagger будет доступен по адресу `http://localhost:5106/swagger`.

Данные начального администратора задаются в локальном
`appsettings.Development.json`.

## Тесты

```powershell
dotnet test
```
