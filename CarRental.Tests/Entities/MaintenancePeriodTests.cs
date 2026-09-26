using CarRental.Api.Entities;

namespace CarRental.Tests.Entities;

public class MaintenancePeriodTests
{
    [Fact]
    public void Overlaps_WhenPeriodsIntersect_ReturnsTrue()
    {
        MaintenancePeriod maintenancePeriod = CreateMaintenancePeriod();

        bool overlaps = maintenancePeriod.Overlaps(
            new DateOnly(2026, 7, 12),
            new DateOnly(2026, 7, 20));

        Assert.True(overlaps);
    }

    [Fact]
    public void Overlaps_WhenPeriodsDoNotIntersect_ReturnsFalse()
    {
        MaintenancePeriod maintenancePeriod = CreateMaintenancePeriod();

        bool overlaps = maintenancePeriod.Overlaps(
            new DateOnly(2026, 7, 16),
            new DateOnly(2026, 7, 20));

        Assert.False(overlaps);
    }

    [Fact]
    public void Constructor_WhenDescriptionIsEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new MaintenancePeriod(
                TestData.CreateCar(),
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 15),
                string.Empty));
    }

    [Fact]
    public void Constructor_WhenDescriptionIsTooLong_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new MaintenancePeriod(
                TestData.CreateCar(),
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 15),
                new string('A', 501)));
    }

    private static MaintenancePeriod CreateMaintenancePeriod()
    {
        return new MaintenancePeriod(
            TestData.CreateCar(),
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 15),
            "Scheduled maintenance");
    }
}
