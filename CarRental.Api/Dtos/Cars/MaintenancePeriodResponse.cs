namespace CarRental.Api.Dtos.Cars;

public record MaintenancePeriodResponse(
    int Id,
    int CarId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Description);
