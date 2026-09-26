using CarRental.Api.Enums;

namespace CarRental.Api.Dtos.Cars;

public record CarStatusResponse(
    int CarId,
    DateOnly StartDate,
    DateOnly EndDate,
    CarStatus Status);
