using CarRental.Api.Enums;

namespace CarRental.Api.Dtos.Rentals;

public record RentalRequestResponse(
    int Id,
    int ClientId,
    string ClientUsername,
    int CarId,
    string Car,
    DateOnly StartDate,
    DateOnly EndDate,
    RentalRequestStatus Status);
