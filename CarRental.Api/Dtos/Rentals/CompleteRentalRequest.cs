namespace CarRental.Api.Dtos.Rentals;

public record CompleteRentalRequest(
    DateOnly ActualReturnDate,
    bool HasDamage);
