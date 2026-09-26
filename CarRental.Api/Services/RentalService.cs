using CarRental.Api.Data;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Api.Services;

public class RentalService
{
    private const int StandardMinimumAge = 21;
    private const int StandardMinimumExperienceYears = 2;
    private const int PremiumMinimumAge = 23;
    private const int PremiumMinimumExperienceYears = 3;
    private const int SportMinimumAge = 25;
    private const int SportMinimumExperienceYears = 5;
    private const decimal DamagePenaltyCoefficient = 0.5m;

    private readonly CarRentalDbContext _dbContext;

    public RentalService(CarRentalDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<RentalRequest> CreateRentalRequestAsync(
        int clientId,
        int carId,
        DateOnly startDate,
        DateOnly endDate)
    {
        ValidatePeriod(startDate, endDate);

        User client = await GetClientAsync(clientId);
        Car car = await GetCarAsync(carId);

        ValidateDriverRequirements(client, car.Category, startDate);
        await EnsureCarIsAvailableAsync(carId, startDate, endDate);

        var rentalRequest = new RentalRequest(
            client,
            car,
            startDate,
            endDate);

        _dbContext.RentalRequests.Add(rentalRequest);

        if (client.HasRole(UserRole.Manager)
            || client.HasRole(UserRole.Administrator))
        {
            rentalRequest.Approve();

            var agreement = new RentalAgreement(
                rentalRequest,
                car.PricePerDay);

            _dbContext.RentalAgreements.Add(agreement);
        }

        await _dbContext.SaveChangesAsync();

        return rentalRequest;
    }

    public async Task<RentalRequest> GetRentalRequestAsync(int rentalRequestId)
    {
        if (rentalRequestId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rentalRequestId),
                "Rental request ID must be positive.");
        }

        RentalRequest? rentalRequest = await _dbContext.RentalRequests
            .Include(request => request.Client)
            .ThenInclude(client => client.RoleAssignments)
            .Include(request => request.Car)
            .SingleOrDefaultAsync(request => request.Id == rentalRequestId);

        if (rentalRequest is null)
            throw new RentalRequestNotFoundException(rentalRequestId);

        return rentalRequest;
    }

    public async Task<IReadOnlyCollection<RentalRequest>> GetClientRentalRequestsAsync(
        int clientId)
    {
        await GetClientAsync(clientId);

        return await _dbContext.RentalRequests
            .Include(request => request.Client)
            .Include(request => request.Car)
            .Where(request => request.ClientId == clientId)
            .OrderBy(request => request.Id)
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<RentalRequest>> GetRentalRequestsAsync()
    {
        return await _dbContext.RentalRequests
            .Include(request => request.Client)
            .Include(request => request.Car)
            .OrderBy(request => request.Id)
            .ToListAsync();
    }

    public async Task<RentalAgreement> ApproveRentalRequestAsync(int rentalRequestId)
    {
        RentalRequest rentalRequest = await GetRentalRequestAsync(rentalRequestId);

        await EnsureCarIsAvailableAsync(
            rentalRequest.CarId,
            rentalRequest.StartDate,
            rentalRequest.EndDate);

        rentalRequest.Approve();

        var agreement = new RentalAgreement(
            rentalRequest,
            rentalRequest.Car.PricePerDay);

        _dbContext.RentalAgreements.Add(agreement);
        await _dbContext.SaveChangesAsync();

        return agreement;
    }

    public async Task RejectRentalRequestAsync(int rentalRequestId)
    {
        RentalRequest rentalRequest = await GetRentalRequestAsync(rentalRequestId);

        rentalRequest.Reject();
        await _dbContext.SaveChangesAsync();
    }

    public async Task<RentalAgreement> GetRentalAgreementAsync(int rentalAgreementId)
    {
        if (rentalAgreementId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rentalAgreementId),
                "Rental agreement ID must be positive.");
        }

        RentalAgreement? agreement = await _dbContext.RentalAgreements
            .Include(rentalAgreement => rentalAgreement.RentalRequest)
            .Include(rentalAgreement => rentalAgreement.Client)
            .Include(rentalAgreement => rentalAgreement.Car)
            .SingleOrDefaultAsync(rentalAgreement => rentalAgreement.Id == rentalAgreementId);

        if (agreement is null)
            throw new RentalAgreementNotFoundException(rentalAgreementId);

        return agreement;
    }

    public async Task<RentalAgreement> CompleteRentalAsync(
        int rentalRequestId,
        DateOnly actualReturnDate,
        bool hasDamage)
    {
        if (rentalRequestId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rentalRequestId),
                "Rental request ID must be positive.");
        }

        RentalAgreement? agreement = await _dbContext.RentalAgreements
            .Include(rentalAgreement => rentalAgreement.RentalRequest)
            .Include(rentalAgreement => rentalAgreement.Client)
            .Include(rentalAgreement => rentalAgreement.Car)
            .SingleOrDefaultAsync(
                rentalAgreement => rentalAgreement.RentalRequestId == rentalRequestId);

        if (agreement is null)
            throw new RentalAgreementNotFoundException(rentalRequestId);

        agreement.Complete(
            actualReturnDate,
            hasDamage,
            DamagePenaltyCoefficient);
        agreement.RentalRequest.Complete();

        await _dbContext.SaveChangesAsync();

        return agreement;
    }

    private async Task<User> GetClientAsync(int clientId)
    {
        if (clientId <= 0)
            throw new ArgumentOutOfRangeException(nameof(clientId), "Client ID must be positive.");

        User? client = await _dbContext.Users
            .Include(user => user.RoleAssignments)
            .SingleOrDefaultAsync(user => user.Id == clientId);

        if (client is null)
            throw new UserNotFoundException(clientId);

        if (!client.HasRole(UserRole.Client))
            throw new UserIsNotClientException(clientId);

        return client;
    }

    private async Task<Car> GetCarAsync(int carId)
    {
        if (carId <= 0)
            throw new ArgumentOutOfRangeException(nameof(carId), "Car ID must be positive.");

        Car? car = await _dbContext.Cars
            .SingleOrDefaultAsync(existingCar => existingCar.Id == carId);

        if (car is null)
            throw new CarNotFoundException(carId);

        return car;
    }

    private async Task EnsureCarIsAvailableAsync(
        int carId,
        DateOnly startDate,
        DateOnly endDate)
    {
        bool hasMaintenance = await _dbContext.MaintenancePeriods
            .AnyAsync(
                period =>
                    period.CarId == carId
                    && period.StartDate <= endDate
                    && period.EndDate >= startDate);

        bool hasAgreement = await _dbContext.RentalAgreements
            .AnyAsync(
                agreement =>
                    agreement.CarId == carId
                    && agreement.StartDate <= endDate
                    && ((!agreement.IsCompleted && agreement.EndDate >= startDate)
                        || (agreement.IsCompleted
                            && agreement.ActualReturnDate >= startDate)));

        if (hasMaintenance || hasAgreement)
            throw new CarIsNotAvailableException(carId, startDate, endDate);
    }

    private static void ValidateDriverRequirements(
        User client,
        CarCategory category,
        DateOnly rentalStartDate)
    {
        int minimumAge = GetMinimumAge(category);
        int minimumExperienceYears = GetMinimumExperienceYears(category);
        int clientAge = client.GetAgeOn(rentalStartDate);
        int drivingExperienceYears = client.GetDrivingExperienceYearsOn(rentalStartDate);

        if (clientAge < minimumAge || drivingExperienceYears < minimumExperienceYears)
        {
            throw new DriverRequirementsNotMetException(
                category,
                minimumAge,
                minimumExperienceYears);
        }
    }

    private static int GetMinimumAge(CarCategory category)
    {
        return category switch
        {
            CarCategory.Economy => StandardMinimumAge,
            CarCategory.Comfort => StandardMinimumAge,
            CarCategory.Premium => PremiumMinimumAge,
            CarCategory.Sport => SportMinimumAge,
            _ => throw new ArgumentOutOfRangeException(nameof(category)),
        };
    }

    private static int GetMinimumExperienceYears(CarCategory category)
    {
        return category switch
        {
            CarCategory.Economy => StandardMinimumExperienceYears,
            CarCategory.Comfort => StandardMinimumExperienceYears,
            CarCategory.Premium => PremiumMinimumExperienceYears,
            CarCategory.Sport => SportMinimumExperienceYears,
            _ => throw new ArgumentOutOfRangeException(nameof(category)),
        };
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
