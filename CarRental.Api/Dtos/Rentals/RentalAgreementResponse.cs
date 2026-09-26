namespace CarRental.Api.Dtos.Rentals;

public record RentalAgreementResponse(
    int Id,
    int RentalRequestId,
    int ClientId,
    int CarId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal PricePerDay,
    decimal RentalCost,
    decimal Penalty,
    decimal TotalCost,
    DateOnly? ActualReturnDate,
    bool HasDamage,
    bool IsCompleted);
