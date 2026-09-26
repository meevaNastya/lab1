namespace CarRental.Api.Dtos.Rentals;

public record CreateRentalRequest(
    int CarId,
    DateOnly StartDate,
    DateOnly EndDate);
