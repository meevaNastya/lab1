namespace CarRental.Api.Dtos.Cars;

public record CreateMaintenanceRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    string Description);
