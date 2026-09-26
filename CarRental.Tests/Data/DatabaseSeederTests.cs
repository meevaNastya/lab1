using CarRental.Api.Data;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarRental.Tests.Data;

public class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_WhenAdministratorDoesNotExist_CreatesAdministrator()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        DatabaseSeeder databaseSeeder = CreateDatabaseSeeder(dbContext);

        await databaseSeeder.SeedAsync();

        User administrator = await dbContext.Users
            .Include(user => user.RoleAssignments)
            .SingleAsync();

        Assert.Equal("admin", administrator.Username);
        Assert.Contains(UserRole.Administrator, administrator.Roles);
        Assert.NotEqual("test-password", administrator.PasswordHash);
    }

    [Fact]
    public async Task SeedAsync_WhenCarsDoNotExist_CreatesTenCars()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        DatabaseSeeder databaseSeeder = CreateDatabaseSeeder(dbContext);

        await databaseSeeder.SeedAsync();

        List<Car> cars = await dbContext.Cars.ToListAsync();

        Assert.Equal(10, cars.Count);
        Assert.Contains(cars, car => car.Category == CarCategory.Economy);
        Assert.Contains(cars, car => car.Category == CarCategory.Comfort);
        Assert.Contains(cars, car => car.Category == CarCategory.Premium);
        Assert.Contains(cars, car => car.Category == CarCategory.Sport);
    }

    [Fact]
    public async Task SeedAsync_WhenDataExists_DoesNotCreateDuplicates()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        DatabaseSeeder databaseSeeder = CreateDatabaseSeeder(dbContext);
        await databaseSeeder.SeedAsync();

        await databaseSeeder.SeedAsync();

        Assert.Equal(1, await dbContext.Users.CountAsync());
        Assert.Equal(10, await dbContext.Cars.CountAsync());
    }

    private static CarRentalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarRentalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarRentalDbContext(options);
    }

    private static DatabaseSeeder CreateDatabaseSeeder(CarRentalDbContext dbContext)
    {
        var passwordHasher = new PasswordHasher<string>();
        IOptions<InitialAdministratorOptions> administratorOptions = Options.Create(
            new InitialAdministratorOptions
            {
                Username = "admin",
                Password = "test-password",
                BirthDate = new DateOnly(1990, 1, 1),
            });

        return new DatabaseSeeder(
            dbContext,
            passwordHasher,
            administratorOptions);
    }
}
