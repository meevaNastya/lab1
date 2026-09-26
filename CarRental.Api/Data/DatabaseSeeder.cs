using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarRental.Api.Data;

public class DatabaseSeeder
{
    private readonly CarRentalDbContext _dbContext;
    private readonly IPasswordHasher<string> _passwordHasher;
    private readonly InitialAdministratorOptions _administratorOptions;

    public DatabaseSeeder(
        CarRentalDbContext dbContext,
        IPasswordHasher<string> passwordHasher,
        IOptions<InitialAdministratorOptions> administratorOptions)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _administratorOptions = administratorOptions?.Value
            ?? throw new ArgumentNullException(nameof(administratorOptions));
    }

    public async Task SeedAsync()
    {
        await SeedAdministratorAsync();
        await SeedCarsAsync();
    }

    private async Task SeedAdministratorAsync()
    {
        bool administratorExists = await _dbContext.UserRoleAssignments
            .AnyAsync(assignment => assignment.Role == UserRole.Administrator);

        if (administratorExists)
            return;

        ValidateOptions();

        string passwordHash = _passwordHasher.HashPassword(
            _administratorOptions.Username,
            _administratorOptions.Password);
        var administrator = new User(
            _administratorOptions.Username,
            passwordHash,
            _administratorOptions.BirthDate,
            null,
            new[] { UserRole.Administrator });

        _dbContext.Users.Add(administrator);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedCarsAsync()
    {
        if (await _dbContext.Cars.AnyAsync())
            return;

        var cars = new List<Car>
        {
            new(
                "Skoda",
                "Rapid",
                "SEEDCAR0000000001",
                CarCategory.Economy,
                2800),
            new(
                "Volkswagen",
                "Polo",
                "SEEDCAR0000000002",
                CarCategory.Economy,
                3000),
            new(
                "Kia",
                "Rio",
                "SEEDCAR0000000003",
                CarCategory.Economy,
                2900),
            new(
                "Skoda",
                "Octavia",
                "SEEDCAR0000000004",
                CarCategory.Comfort,
                3500),
            new(
                "Toyota",
                "Corolla",
                "SEEDCAR0000000005",
                CarCategory.Comfort,
                3800),
            new(
                "Hyundai",
                "Solaris",
                "SEEDCAR0000000006",
                CarCategory.Comfort,
                3200),
            new(
                "Toyota",
                "Camry",
                "SEEDCAR0000000007",
                CarCategory.Premium,
                6500),
            new(
                "BMW",
                "5 Series",
                "SEEDCAR0000000008",
                CarCategory.Premium,
                9000),
            new(
                "Mercedes-AMG",
                "C 63",
                "SEEDCAR0000000009",
                CarCategory.Sport,
                14000),
            new(
                "Porsche",
                "911",
                "SEEDCAR0000000010",
                CarCategory.Sport,
                18000),
        };

        _dbContext.Cars.AddRange(cars);
        await _dbContext.SaveChangesAsync();
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_administratorOptions.Username))
        {
            throw new InvalidOperationException(
                "Initial administrator username is not configured.");
        }

        if (_administratorOptions.Password.Length < 6)
        {
            throw new InvalidOperationException(
                "Initial administrator password must contain at least 6 characters.");
        }

        if (_administratorOptions.BirthDate == default)
        {
            throw new InvalidOperationException(
                "Initial administrator birth date is not configured.");
        }
    }
}
