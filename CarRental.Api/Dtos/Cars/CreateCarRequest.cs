using CarRental.Api.Enums;

namespace CarRental.Api.Dtos.Cars;

public record CreateCarRequest(
    string Brand,
    string Model,
    string Vin,
    CarCategory Category,
    decimal PricePerDay);
