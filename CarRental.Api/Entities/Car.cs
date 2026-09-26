using CarRental.Api.Enums;

namespace CarRental.Api.Entities;

public class Car
{
    private const int VinLength = 17;
    private const int MaximumBrandLength = 50;
    private const int MaximumModelLength = 50;

    private Car()
    {
        Brand = string.Empty;
        Model = string.Empty;
        Vin = string.Empty;
    }

    public Car(
        string brand,
        string model,
        string vin,
        CarCategory category,
        decimal pricePerDay)
    {
        if (string.IsNullOrWhiteSpace(brand))
            throw new ArgumentException("Car brand is empty.", nameof(brand));

        if (brand.Length > MaximumBrandLength)
        {
            throw new ArgumentException(
                $"Car brand cannot contain more than {MaximumBrandLength} characters.",
                nameof(brand));
        }

        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Car model is empty.", nameof(model));

        if (model.Length > MaximumModelLength)
        {
            throw new ArgumentException(
                $"Car model cannot contain more than {MaximumModelLength} characters.",
                nameof(model));
        }

        if (string.IsNullOrWhiteSpace(vin))
            throw new ArgumentException("Car VIN is empty.", nameof(vin));

        if (vin.Length != VinLength)
        {
            throw new ArgumentException(
                $"Car VIN must contain {VinLength} characters.",
                nameof(vin));
        }

        if (!Enum.IsDefined(category) || category == CarCategory.Undefined)
            throw new ArgumentException("Car category is undefined.", nameof(category));

        if (pricePerDay <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pricePerDay),
                "Price per day must be positive.");
        }

        Brand = brand.Trim();
        Model = model.Trim();
        Vin = vin.ToUpperInvariant();
        Category = category;
        PricePerDay = pricePerDay;
    }

    public int Id { get; private set; }

    public string Brand { get; private set; }

    public string Model { get; private set; }

    public string Vin { get; private set; }

    public CarCategory Category { get; private set; }

    public decimal PricePerDay { get; private set; }

    public void ChangePricePerDay(decimal pricePerDay)
    {
        if (pricePerDay <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pricePerDay),
                "Price per day must be positive.");
        }

        PricePerDay = pricePerDay;
    }
}
