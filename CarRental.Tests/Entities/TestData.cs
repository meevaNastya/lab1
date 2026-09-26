using CarRental.Api.Entities;
using CarRental.Api.Enums;

namespace CarRental.Tests.Entities;

public static class TestData
{
    public static User CreateClient()
    {
        return new User(
            "client",
            "password-hash",
            new DateOnly(2000, 1, 1),
            new DateOnly(2020, 1, 1),
            new[] { UserRole.Client });
    }

    public static Car CreateCar()
    {
        return new Car(
            "Toyota",
            "Corolla",
            "JTDBR32E720123456",
            CarCategory.Comfort,
            3000);
    }
}
