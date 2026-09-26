using CarRental.Api.Data;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Exceptions;
using CarRental.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Tests.Services;

public class CarServiceTests
{
    [Fact]
    public async Task CreateCarAsync_WithValidArguments_SavesCar()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);

        Car car = await CreateCarAsync(carService);

        Assert.True(car.Id > 0);
        Assert.Equal("Toyota", car.Brand);
        Assert.Equal(1, await dbContext.Cars.CountAsync());
    }

    [Fact]
    public async Task CreateCarAsync_WhenVinAlreadyExists_ThrowsVinAlreadyExistsException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);
        await CreateCarAsync(carService);

        await Assert.ThrowsAsync<VinAlreadyExistsException>(
            () => CreateCarAsync(carService));
    }

    [Fact]
    public async Task GetCarAsync_WhenCarDoesNotExist_ThrowsCarNotFoundException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);

        await Assert.ThrowsAsync<CarNotFoundException>(
            () => carService.GetCarAsync(1));
    }

    [Fact]
    public async Task ChangeCarPricePerDayAsync_WithPositivePrice_ChangesPrice()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);
        Car car = await CreateCarAsync(carService);

        await carService.ChangeCarPricePerDayAsync(car.Id, 4500);
        dbContext.ChangeTracker.Clear();

        Car savedCar = await carService.GetCarAsync(car.Id);

        Assert.Equal(4500, savedCar.PricePerDay);
    }

    [Fact]
    public async Task AddMaintenancePeriodAsync_WhenCarIsAvailable_SavesPeriod()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);
        Car car = await CreateCarAsync(carService);

        MaintenancePeriod period = await carService.AddMaintenancePeriodAsync(
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12),
            "Scheduled maintenance");

        Assert.True(period.Id > 0);
        Assert.Equal(1, await dbContext.MaintenancePeriods.CountAsync());
    }

    [Fact]
    public async Task AddMaintenancePeriodAsync_WhenAgreementOverlaps_ThrowsCarIsNotAvailableException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);
        Car car = await CreateCarAsync(carService);
        await AddAgreementAsync(dbContext, car);

        await Assert.ThrowsAsync<CarIsNotAvailableException>(
            () => carService.AddMaintenancePeriodAsync(
                car.Id,
                new DateOnly(2026, 7, 12),
                new DateOnly(2026, 7, 15),
                "Scheduled maintenance"));
    }

    [Fact]
    public async Task AddMaintenancePeriodAsync_WhenMaintenanceOverlaps_ThrowsCarIsNotAvailableException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);
        Car car = await CreateCarAsync(carService);
        await carService.AddMaintenancePeriodAsync(
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12),
            "Scheduled maintenance");

        await Assert.ThrowsAsync<CarIsNotAvailableException>(
            () => carService.AddMaintenancePeriodAsync(
                car.Id,
                new DateOnly(2026, 7, 12),
                new DateOnly(2026, 7, 15),
                "Additional maintenance"));
    }

    [Fact]
    public async Task GetAvailableCarsAsync_WhenCarHasMaintenance_DoesNotReturnCar()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);
        Car car = await CreateCarAsync(carService);
        await carService.AddMaintenancePeriodAsync(
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12),
            "Scheduled maintenance");

        IReadOnlyCollection<Car> cars = await carService.GetAvailableCarsAsync(
            new DateOnly(2026, 7, 12),
            new DateOnly(2026, 7, 15));

        Assert.DoesNotContain(cars, availableCar => availableCar.Id == car.Id);
    }

    [Fact]
    public async Task GetAvailableCarsAsync_WhenCarHasAgreement_DoesNotReturnCar()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);
        Car car = await CreateCarAsync(carService);
        await AddAgreementAsync(dbContext, car);

        IReadOnlyCollection<Car> cars = await carService.GetAvailableCarsAsync(
            new DateOnly(2026, 7, 12),
            new DateOnly(2026, 7, 15));

        Assert.DoesNotContain(cars, availableCar => availableCar.Id == car.Id);
    }

    [Fact]
    public async Task GetAvailableCarsAsync_WhenCarHasOnlyPendingRequest_ReturnsCar()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);
        Car car = await CreateCarAsync(carService);
        User client = await AddClientAsync(dbContext);
        var request = new RentalRequest(
            client,
            car,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));
        dbContext.RentalRequests.Add(request);
        await dbContext.SaveChangesAsync();

        IReadOnlyCollection<Car> cars = await carService.GetAvailableCarsAsync(
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));

        Assert.Contains(cars, availableCar => availableCar.Id == car.Id);
    }

    [Fact]
    public async Task GetAvailableCarsAsync_AppliesCatalogFilters()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);
        await CreateCarAsync(carService);
        Car expectedCar = await carService.CreateCarAsync(
            "Skoda",
            "Rapid",
            "TMBJG7NE5G0654321",
            CarCategory.Economy,
            2500);

        IReadOnlyCollection<Car> cars = await carService.GetAvailableCarsAsync(
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12),
            CarCategory.Economy,
            "skoda",
            2600);

        Assert.Single(cars);
        Assert.Equal(expectedCar.Id, cars.Single().Id);
    }

    [Fact]
    public async Task GetAvailableCarsAsync_WhenAgreementIsCompleted_BlocksUntilActualReturnDate()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);
        Car car = await CreateCarAsync(carService);
        RentalAgreement agreement = await AddAgreementAsync(dbContext, car);
        agreement.Complete(new DateOnly(2026, 7, 14), false, 0.5m);
        agreement.RentalRequest.Complete();
        await dbContext.SaveChangesAsync();

        IReadOnlyCollection<Car> beforeReturn = await carService.GetAvailableCarsAsync(
            new DateOnly(2026, 7, 13),
            new DateOnly(2026, 7, 13));
        IReadOnlyCollection<Car> afterReturn = await carService.GetAvailableCarsAsync(
            new DateOnly(2026, 7, 15),
            new DateOnly(2026, 7, 15));

        Assert.DoesNotContain(beforeReturn, availableCar => availableCar.Id == car.Id);
        Assert.Contains(afterReturn, availableCar => availableCar.Id == car.Id);
    }

    [Fact]
    public async Task GetCarStatusAsync_WhenCarHasMaintenance_ReturnsUnderMaintenance()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);
        Car car = await CreateCarAsync(carService);
        await carService.AddMaintenancePeriodAsync(
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12),
            "Scheduled maintenance");

        CarStatus status = await carService.GetCarStatusAsync(
            car.Id,
            new DateOnly(2026, 7, 11),
            new DateOnly(2026, 7, 11));

        Assert.Equal(CarStatus.UnderMaintenance, status);
    }

    [Fact]
    public async Task GetCarStatusAsync_WhenCarHasAgreement_ReturnsRented()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);
        Car car = await CreateCarAsync(carService);
        await AddAgreementAsync(dbContext, car);

        CarStatus status = await carService.GetCarStatusAsync(
            car.Id,
            new DateOnly(2026, 7, 11),
            new DateOnly(2026, 7, 11));

        Assert.Equal(CarStatus.Rented, status);
    }

    [Fact]
    public async Task GetCarStatusAsync_WhenCarHasNoBlockingPeriods_ReturnsAvailable()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var carService = new CarService(dbContext);
        Car car = await CreateCarAsync(carService);

        CarStatus status = await carService.GetCarStatusAsync(
            car.Id,
            new DateOnly(2026, 7, 11),
            new DateOnly(2026, 7, 11));

        Assert.Equal(CarStatus.Available, status);
    }

    private static CarRentalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarRentalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarRentalDbContext(options);
    }

    private static Task<Car> CreateCarAsync(CarService carService)
    {
        return carService.CreateCarAsync(
            "Toyota",
            "Corolla",
            "JTDBR32E720123456",
            CarCategory.Comfort,
            3000);
    }

    private static async Task<User> AddClientAsync(CarRentalDbContext dbContext)
    {
        var client = new User(
            "client",
            "password-hash",
            new DateOnly(2000, 1, 1),
            new DateOnly(2020, 1, 1),
            new[] { UserRole.Client });

        dbContext.Users.Add(client);
        await dbContext.SaveChangesAsync();

        return client;
    }

    private static async Task<RentalAgreement> AddAgreementAsync(
        CarRentalDbContext dbContext,
        Car car)
    {
        User client = await AddClientAsync(dbContext);
        var request = new RentalRequest(
            client,
            car,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));
        request.Approve();

        dbContext.RentalRequests.Add(request);
        await dbContext.SaveChangesAsync();

        var agreement = new RentalAgreement(request, car.PricePerDay);
        dbContext.RentalAgreements.Add(agreement);
        await dbContext.SaveChangesAsync();

        return agreement;
    }
}
