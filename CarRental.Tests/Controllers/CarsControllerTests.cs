using System.Reflection;
using CarRental.Api.Controllers;
using CarRental.Api.Data;
using CarRental.Api.Dtos.Cars;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Tests.Controllers;

public class CarsControllerTests
{
    [Fact]
    public void CreateCar_HasManagerAndAdministratorAuthorization()
    {
        MethodInfo method = typeof(CarsController)
            .GetMethod(nameof(CarsController.CreateCar))
            ?? throw new InvalidOperationException("CreateCar method was not found.");
        AuthorizeAttribute authorizeAttribute = method
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();

        Assert.Equal("Manager,Administrator", authorizeAttribute.Roles);
    }

    [Fact]
    public void GetAvailableCars_AllowsAnonymousAccess()
    {
        MethodInfo method = typeof(CarsController)
            .GetMethod(nameof(CarsController.GetAvailableCars))
            ?? throw new InvalidOperationException("GetAvailableCars method was not found.");

        Assert.Single(method.GetCustomAttributes<AllowAnonymousAttribute>());
    }

    [Fact]
    public async Task CreateCar_WithValidRequest_ReturnsCreatedCar()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        CarsController controller = CreateController(dbContext);
        var request = new CreateCarRequest(
            "Skoda",
            "Octavia",
            "TMBJG7NE5G0123456",
            CarCategory.Comfort,
            3500);

        ActionResult<CarResponse> actionResult = await controller.CreateCar(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var response = Assert.IsType<CarResponse>(createdResult.Value);
        Assert.Equal("Skoda", response.Brand);
        Assert.Equal(1, await dbContext.Cars.CountAsync());
    }

    [Fact]
    public async Task CreateCar_WhenVinAlreadyExists_ReturnsConflict()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        CarsController controller = CreateController(dbContext);
        CreateCarRequest request = CreateCarRequest();
        await controller.CreateCar(request);

        ActionResult<CarResponse> actionResult = await controller.CreateCar(request);

        Assert.IsType<ConflictObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetCar_WithNonPositiveId_ReturnsBadRequest()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        CarsController controller = CreateController(dbContext);

        ActionResult<CarResponse> actionResult = await controller.GetCar(0);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetCar_WhenCarDoesNotExist_ReturnsNotFound()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        CarsController controller = CreateController(dbContext);

        ActionResult<CarResponse> actionResult = await controller.GetCar(1);

        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetCars_ReturnsAllCars()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        CarsController controller = CreateController(dbContext);
        await AddCarAsync(
            dbContext,
            "TMBJG7NE5G0123456",
            "Skoda",
            "Octavia");
        await AddCarAsync(
            dbContext,
            "JTDBR32E720123456",
            "Toyota",
            "Corolla");

        ActionResult<IReadOnlyCollection<CarResponse>> actionResult =
            await controller.GetCars();

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsAssignableFrom<IReadOnlyCollection<CarResponse>>(
            okResult.Value);
        Assert.Equal(2, response.Count);
    }

    [Fact]
    public async Task ChangePrice_WithValidPrice_ChangesPrice()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        CarsController controller = CreateController(dbContext);
        Car car = await AddCarAsync(
            dbContext,
            "TMBJG7NE5G0123456",
            "Skoda",
            "Octavia");

        IActionResult result = await controller.ChangePrice(
            car.Id,
            new ChangeCarPriceRequest(4200));

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(4200, car.PricePerDay);
    }

    [Fact]
    public async Task GetAvailableCars_WithInvalidMaximumPrice_ReturnsBadRequest()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        CarsController controller = CreateController(dbContext);

        ActionResult<IReadOnlyCollection<CarResponse>> actionResult =
            await controller.GetAvailableCars(
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 12),
                maxPricePerDay: 0);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetAvailableCars_ReturnsOnlyCarsAvailableForSelectedDates()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        CarsController controller = CreateController(dbContext);
        Car firstCar = await AddCarAsync(
            dbContext,
            "TMBJG7NE5G0123456",
            "Skoda",
            "Octavia");
        Car secondCar = await AddCarAsync(
            dbContext,
            "JTDBR32E720123456",
            "Toyota",
            "Corolla");
        dbContext.MaintenancePeriods.Add(
            new MaintenancePeriod(
                firstCar,
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 12),
                "Scheduled maintenance"));
        await dbContext.SaveChangesAsync();

        ActionResult<IReadOnlyCollection<CarResponse>> actionResult =
            await controller.GetAvailableCars(
                new DateOnly(2026, 7, 11),
                new DateOnly(2026, 7, 15));

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsAssignableFrom<IReadOnlyCollection<CarResponse>>(
            okResult.Value);
        Assert.Single(response);
        Assert.Equal(secondCar.Id, response.Single().Id);
    }

    [Fact]
    public async Task GetCarStatus_WhenCarHasMaintenance_ReturnsUnderMaintenance()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        CarsController controller = CreateController(dbContext);
        Car car = await AddCarAsync(
            dbContext,
            "TMBJG7NE5G0123456",
            "Skoda",
            "Octavia");
        dbContext.MaintenancePeriods.Add(
            new MaintenancePeriod(
                car,
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 12),
                "Scheduled maintenance"));
        await dbContext.SaveChangesAsync();

        ActionResult<CarStatusResponse> actionResult = await controller.GetCarStatus(
            car.Id,
            new DateOnly(2026, 7, 11),
            new DateOnly(2026, 7, 11));

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<CarStatusResponse>(okResult.Value);
        Assert.Equal(CarStatus.UnderMaintenance, response.Status);
    }

    [Fact]
    public async Task AddMaintenance_WhenDatesOverlapExistingMaintenance_ReturnsConflict()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        CarsController controller = CreateController(dbContext);
        Car car = await AddCarAsync(
            dbContext,
            "TMBJG7NE5G0123456",
            "Skoda",
            "Octavia");
        await controller.AddMaintenance(
            car.Id,
            new CreateMaintenanceRequest(
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 12),
                "Scheduled maintenance"));

        ActionResult<MaintenancePeriodResponse> actionResult =
            await controller.AddMaintenance(
                car.Id,
                new CreateMaintenanceRequest(
                    new DateOnly(2026, 7, 12),
                    new DateOnly(2026, 7, 15),
                    "Additional maintenance"));

        Assert.IsType<ConflictObjectResult>(actionResult.Result);
    }

    private static CarRentalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarRentalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarRentalDbContext(options);
    }

    private static CarsController CreateController(CarRentalDbContext dbContext)
    {
        return new CarsController(new CarService(dbContext));
    }

    private static CreateCarRequest CreateCarRequest()
    {
        return new CreateCarRequest(
            "Skoda",
            "Octavia",
            "TMBJG7NE5G0123456",
            CarCategory.Comfort,
            3500);
    }

    private static async Task<Car> AddCarAsync(
        CarRentalDbContext dbContext,
        string vin,
        string brand,
        string model)
    {
        var car = new Car(
            brand,
            model,
            vin,
            CarCategory.Comfort,
            3500);

        dbContext.Cars.Add(car);
        await dbContext.SaveChangesAsync();

        return car;
    }
}
