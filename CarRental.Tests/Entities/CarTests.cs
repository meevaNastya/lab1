using CarRental.Api.Entities;
using CarRental.Api.Enums;

namespace CarRental.Tests.Entities;

public class CarTests
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesCar()
    {
        var car = new Car(
            "Skoda",
            "Octavia",
            "TMBJG7NE5G0123456",
            CarCategory.Comfort,
            3500);

        Assert.Equal("Skoda", car.Brand);
        Assert.Equal("Octavia", car.Model);
        Assert.Equal(3500, car.PricePerDay);
    }

    [Fact]
    public void Constructor_WithInvalidVinLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new Car(
                "Skoda",
                "Octavia",
                "SHORT",
                CarCategory.Comfort,
                3500));
    }

    [Fact]
    public void Constructor_WithLongBrand_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new Car(
                new string('A', 51),
                "Octavia",
                "TMBJG7NE5G0123456",
                CarCategory.Comfort,
                3500));
    }

    [Fact]
    public void Constructor_WithUnknownCategory_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new Car(
                "Skoda",
                "Octavia",
                "TMBJG7NE5G0123456",
                (CarCategory)999,
                3500));
    }

    [Fact]
    public void Constructor_WithLowercaseVin_NormalizesVin()
    {
        var car = new Car(
            "Skoda",
            "Octavia",
            "tmbjg7ne5g0123456",
            CarCategory.Comfort,
            3500);

        Assert.Equal("TMBJG7NE5G0123456", car.Vin);
    }

    [Fact]
    public void ChangePricePerDay_WithPositivePrice_ChangesPrice()
    {
        Car car = CreateCar();

        car.ChangePricePerDay(4000);

        Assert.Equal(4000, car.PricePerDay);
    }

    [Fact]
    public void ChangePricePerDay_WithNonPositivePrice_ThrowsArgumentOutOfRangeException()
    {
        Car car = CreateCar();

        Assert.Throws<ArgumentOutOfRangeException>(() => car.ChangePricePerDay(0));
    }

    private static Car CreateCar()
    {
        return new Car(
            "Skoda",
            "Octavia",
            "TMBJG7NE5G0123456",
            CarCategory.Comfort,
            3500);
    }
}
