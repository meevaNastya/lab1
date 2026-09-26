using CarRental.Api.Data;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Exceptions;
using CarRental.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Tests.Services;

public class RentalServiceTests
{
    [Fact]
    public async Task CreateRentalRequestAsync_ForClient_CreatesPendingRequest()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddUserAsync(dbContext, new[] { UserRole.Client });
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);

        RentalRequest request = await rentalService.CreateRentalRequestAsync(
            client.Id,
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));

        Assert.Equal(RentalRequestStatus.Pending, request.Status);
        Assert.Equal(0, await dbContext.RentalAgreements.CountAsync());
    }

    [Fact]
    public async Task CreateRentalRequestAsync_ForClientAndManager_CreatesApprovedRequestAndAgreement()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddUserAsync(
            dbContext,
            new[] { UserRole.Client, UserRole.Manager });
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);

        RentalRequest request = await rentalService.CreateRentalRequestAsync(
            client.Id,
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));

        Assert.Equal(RentalRequestStatus.Approved, request.Status);
        Assert.Equal(1, await dbContext.RentalAgreements.CountAsync());
    }

    [Fact]
    public async Task CreateRentalRequestAsync_ForClientAndAdministrator_CreatesApprovedRequestAndAgreement()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddUserAsync(
            dbContext,
            new[] { UserRole.Client, UserRole.Administrator });
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);

        RentalRequest request = await rentalService.CreateRentalRequestAsync(
            client.Id,
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));

        Assert.Equal(RentalRequestStatus.Approved, request.Status);
        Assert.Equal(1, await dbContext.RentalAgreements.CountAsync());
    }

    [Fact]
    public async Task CreateRentalRequestAsync_ForUserWithoutClientRole_ThrowsUserIsNotClientException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User user = await AddUserAsync(dbContext, new[] { UserRole.Manager });
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);

        await Assert.ThrowsAsync<UserIsNotClientException>(
            () => rentalService.CreateRentalRequestAsync(
                user.Id,
                car.Id,
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 12)));
    }

    [Fact]
    public async Task CreateRentalRequestAsync_WhenClientIsTooYoung_ThrowsDriverRequirementsNotMetException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddUserAsync(
            dbContext,
            new[] { UserRole.Client },
            new DateOnly(2007, 1, 1),
            new DateOnly(2025, 1, 1));
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);

        await Assert.ThrowsAsync<DriverRequirementsNotMetException>(
            () => rentalService.CreateRentalRequestAsync(
                client.Id,
                car.Id,
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 12)));
    }

    [Fact]
    public async Task CreateRentalRequestAsync_ForSportCarWithInsufficientExperience_ThrowsDriverRequirementsNotMetException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddUserAsync(
            dbContext,
            new[] { UserRole.Client },
            new DateOnly(1995, 1, 1),
            new DateOnly(2023, 1, 1));
        Car car = await AddCarAsync(dbContext, CarCategory.Sport);
        var rentalService = new RentalService(dbContext);

        await Assert.ThrowsAsync<DriverRequirementsNotMetException>(
            () => rentalService.CreateRentalRequestAsync(
                client.Id,
                car.Id,
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 12)));
    }

    [Fact]
    public async Task CreateRentalRequestAsync_WhenMaintenanceOverlaps_ThrowsCarIsNotAvailableException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddUserAsync(dbContext, new[] { UserRole.Client });
        Car car = await AddCarAsync(dbContext);
        dbContext.MaintenancePeriods.Add(
            new MaintenancePeriod(
                car,
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 12),
                "Scheduled maintenance"));
        await dbContext.SaveChangesAsync();
        var rentalService = new RentalService(dbContext);

        await Assert.ThrowsAsync<CarIsNotAvailableException>(
            () => rentalService.CreateRentalRequestAsync(
                client.Id,
                car.Id,
                new DateOnly(2026, 7, 12),
                new DateOnly(2026, 7, 15)));
    }

    [Fact]
    public async Task CreateRentalRequestAsync_WhenEndDateIsMissing_ThrowsArgumentException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddUserAsync(dbContext, new[] { UserRole.Client });
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => rentalService.CreateRentalRequestAsync(
                client.Id,
                car.Id,
                new DateOnly(2026, 7, 10),
                default));
    }

    [Fact]
    public async Task ApproveRentalRequestAsync_ForPendingRequest_CreatesAgreement()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddUserAsync(dbContext, new[] { UserRole.Client });
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);
        RentalRequest request = await CreateRequestAsync(rentalService, client, car);

        RentalAgreement agreement = await rentalService.ApproveRentalRequestAsync(request.Id);

        Assert.Equal(RentalRequestStatus.Approved, request.Status);
        Assert.Equal(9000, agreement.RentalCost);
        Assert.Equal(car.PricePerDay, agreement.PricePerDay);
    }

    [Fact]
    public async Task ApproveRentalRequestAsync_WhenAnotherAgreementOverlaps_ThrowsCarIsNotAvailableException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User firstClient = await AddUserAsync(
            dbContext,
            new[] { UserRole.Client },
            username: "first-client");
        User secondClient = await AddUserAsync(
            dbContext,
            new[] { UserRole.Client },
            username: "second-client");
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);
        RentalRequest firstRequest = await CreateRequestAsync(rentalService, firstClient, car);
        RentalRequest secondRequest = await CreateRequestAsync(rentalService, secondClient, car);
        await rentalService.ApproveRentalRequestAsync(firstRequest.Id);

        await Assert.ThrowsAsync<CarIsNotAvailableException>(
            () => rentalService.ApproveRentalRequestAsync(secondRequest.Id));

        Assert.Equal(RentalRequestStatus.Pending, secondRequest.Status);
    }

    [Fact]
    public async Task RejectRentalRequestAsync_ForPendingRequest_ChangesStatusToRejected()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddUserAsync(dbContext, new[] { UserRole.Client });
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);
        RentalRequest request = await CreateRequestAsync(rentalService, client, car);

        await rentalService.RejectRentalRequestAsync(request.Id);

        Assert.Equal(RentalRequestStatus.Rejected, request.Status);
    }

    [Fact]
    public async Task CompleteRentalAsync_CompletesAgreementAndRequest()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddUserAsync(dbContext, new[] { UserRole.Client });
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);
        RentalRequest request = await CreateRequestAsync(rentalService, client, car);
        RentalAgreement agreement = await rentalService.ApproveRentalRequestAsync(request.Id);

        RentalAgreement completedAgreement = await rentalService.CompleteRentalAsync(
            request.Id,
            new DateOnly(2026, 7, 14),
            true);

        Assert.True(completedAgreement.IsCompleted);
        Assert.Equal(7500, completedAgreement.Penalty);
        Assert.Equal(16500, completedAgreement.TotalCost);
        Assert.Equal(RentalRequestStatus.Completed, request.Status);
    }

    [Fact]
    public async Task GetClientRentalRequestsAsync_ReturnsOnlySelectedClientRequests()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User firstClient = await AddUserAsync(
            dbContext,
            new[] { UserRole.Client },
            username: "first-client");
        User secondClient = await AddUserAsync(
            dbContext,
            new[] { UserRole.Client },
            username: "second-client");
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);
        await CreateRequestAsync(rentalService, firstClient, car);
        await CreateRequestAsync(rentalService, secondClient, car);

        IReadOnlyCollection<RentalRequest> requests =
            await rentalService.GetClientRentalRequestsAsync(firstClient.Id);

        Assert.Single(requests);
        Assert.Equal(firstClient.Id, requests.Single().ClientId);
    }

    private static CarRentalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarRentalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarRentalDbContext(options);
    }

    private static async Task<User> AddUserAsync(
        CarRentalDbContext dbContext,
        IEnumerable<UserRole> roles,
        DateOnly? birthDate = null,
        DateOnly? driverLicenseIssueDate = null,
        string username = "client")
    {
        var user = new User(
            username,
            "password-hash",
            birthDate ?? new DateOnly(1995, 1, 1),
            driverLicenseIssueDate ?? new DateOnly(2015, 1, 1),
            roles);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }

    private static async Task<Car> AddCarAsync(
        CarRentalDbContext dbContext,
        CarCategory category = CarCategory.Comfort)
    {
        var car = new Car(
            "Toyota",
            "Corolla",
            "JTDBR32E720123456",
            category,
            3000);

        dbContext.Cars.Add(car);
        await dbContext.SaveChangesAsync();

        return car;
    }

    private static Task<RentalRequest> CreateRequestAsync(
        RentalService rentalService,
        User client,
        Car car)
    {
        return rentalService.CreateRentalRequestAsync(
            client.Id,
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));
    }
}
