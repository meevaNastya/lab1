using CarRental.Api.Data;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Api.Services;

public class CarService
{
    private readonly CarRentalDbContext _dbContext;

    public CarService(CarRentalDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Car> CreateCarAsync(
        string brand,
        string model,
        string vin,
        CarCategory category,
        decimal pricePerDay)
    {
        string normalizedVin = vin?.ToUpperInvariant()
            ?? throw new ArgumentNullException(nameof(vin));
        bool vinExists = await _dbContext.Cars
            .AnyAsync(car => car.Vin == normalizedVin);

        if (vinExists)
            throw new VinAlreadyExistsException(normalizedVin);

        var car = new Car(
            brand,
            model,
            normalizedVin,
            category,
            pricePerDay);

        _dbContext.Cars.Add(car);
        await _dbContext.SaveChangesAsync();

        return car;
    }

    public async Task<Car?> FindCarAsync(int carId)
    {
        if (carId <= 0)
            throw new ArgumentOutOfRangeException(nameof(carId), "Car ID must be positive.");

        return await _dbContext.Cars
            .SingleOrDefaultAsync(car => car.Id == carId);
    }

    public async Task<Car> GetCarAsync(int carId)
    {
        Car? car = await FindCarAsync(carId);

        if (car is null)
            throw new CarNotFoundException(carId);

        return car;
    }

    public async Task<IReadOnlyCollection<Car>> GetCarsAsync()
    {
        return await _dbContext.Cars
            .OrderBy(car => car.Id)
            .ToListAsync();
    }

    public async Task ChangeCarPricePerDayAsync(int carId, decimal pricePerDay)
    {
        Car car = await GetCarAsync(carId);

        car.ChangePricePerDay(pricePerDay);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<MaintenancePeriod> AddMaintenancePeriodAsync(
        int carId,
        DateOnly startDate,
        DateOnly endDate,
        string description)
    {
        ValidatePeriod(startDate, endDate);

        Car car = await GetCarAsync(carId);
        bool hasAgreement = await HasAgreementAsync(carId, startDate, endDate);
        bool hasMaintenance = await HasMaintenanceAsync(carId, startDate, endDate);

        if (hasAgreement || hasMaintenance)
            throw new CarIsNotAvailableException(carId, startDate, endDate);

        var maintenancePeriod = new MaintenancePeriod(
            car,
            startDate,
            endDate,
            description);

        _dbContext.MaintenancePeriods.Add(maintenancePeriod);
        await _dbContext.SaveChangesAsync();

        return maintenancePeriod;
    }

    public async Task<IReadOnlyCollection<Car>> GetAvailableCarsAsync(
        DateOnly startDate,
        DateOnly endDate,
        CarCategory? category = null,
        string? brand = null,
        decimal? maxPricePerDay = null)
    {
        ValidatePeriod(startDate, endDate);

        if (category.HasValue
            && (!Enum.IsDefined(category.Value)
                || category.Value == CarCategory.Undefined))
        {
            throw new ArgumentException("Car category is undefined.", nameof(category));
        }

        if (brand is not null && brand.Length > 50)
            throw new ArgumentException("Car brand cannot contain more than 50 characters.", nameof(brand));

        if (maxPricePerDay <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxPricePerDay),
                "Maximum price per day must be positive.");
        }

        IQueryable<Car> query = _dbContext.Cars
            .Where(car => !_dbContext.MaintenancePeriods.Any(
                period =>
                    period.CarId == car.Id
                    && period.StartDate <= endDate
                    && period.EndDate >= startDate))
            .Where(car => !_dbContext.RentalAgreements.Any(
                agreement =>
                    agreement.CarId == car.Id
                    && agreement.StartDate <= endDate
                    && ((!agreement.IsCompleted && agreement.EndDate >= startDate)
                        || (agreement.IsCompleted
                            && agreement.ActualReturnDate >= startDate))));

        if (category.HasValue)
            query = query.Where(car => car.Category == category.Value);

        if (!string.IsNullOrWhiteSpace(brand))
        {
            string normalizedBrand = brand.Trim().ToLower();
            query = query.Where(car => car.Brand.ToLower() == normalizedBrand);
        }

        if (maxPricePerDay.HasValue)
            query = query.Where(car => car.PricePerDay <= maxPricePerDay.Value);

        return await query
            .OrderBy(car => car.Id)
            .ToListAsync();
    }

    public async Task<CarStatus> GetCarStatusAsync(
        int carId,
        DateOnly startDate,
        DateOnly endDate)
    {
        ValidatePeriod(startDate, endDate);
        await GetCarAsync(carId);

        if (await HasMaintenanceAsync(carId, startDate, endDate))
            return CarStatus.UnderMaintenance;

        if (await HasAgreementAsync(carId, startDate, endDate))
            return CarStatus.Rented;

        return CarStatus.Available;
    }

    private async Task<bool> HasAgreementAsync(
        int carId,
        DateOnly startDate,
        DateOnly endDate)
    {
        return await _dbContext.RentalAgreements
            .AnyAsync(
                agreement =>
                    agreement.CarId == carId
                    && agreement.StartDate <= endDate
                    && ((!agreement.IsCompleted && agreement.EndDate >= startDate)
                        || (agreement.IsCompleted
                            && agreement.ActualReturnDate >= startDate)));
    }

    private async Task<bool> HasMaintenanceAsync(
        int carId,
        DateOnly startDate,
        DateOnly endDate)
    {
        return await _dbContext.MaintenancePeriods
            .AnyAsync(
                period =>
                    period.CarId == carId
                    && period.StartDate <= endDate
                    && period.EndDate >= startDate);
    }

    private static void ValidatePeriod(DateOnly startDate, DateOnly endDate)
    {
        if (startDate == default)
            throw new ArgumentException("Start date is required.", nameof(startDate));

        if (endDate == default)
            throw new ArgumentException("End date is required.", nameof(endDate));

        if (endDate < startDate)
        {
            throw new ArgumentException(
                "End date cannot be earlier than start date.",
                nameof(endDate));
        }
    }
}
