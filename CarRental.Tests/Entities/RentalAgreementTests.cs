using CarRental.Api.Entities;

namespace CarRental.Tests.Entities;

public class RentalAgreementTests
{
    [Fact]
    public void Constructor_CalculatesCostIncludingBothBoundaryDates()
    {
        RentalAgreement agreement = CreateAgreement();

        Assert.Equal(3000, agreement.RentalCost);
        Assert.Equal(3000, agreement.TotalCost);
    }

    [Fact]
    public void Overlaps_WhenPeriodsShareBoundaryDate_ReturnsTrue()
    {
        RentalAgreement agreement = CreateAgreement();

        bool overlaps = agreement.Overlaps(
            new DateOnly(2026, 7, 12),
            new DateOnly(2026, 7, 15));

        Assert.True(overlaps);
    }

    [Fact]
    public void Overlaps_WhenPeriodsDoNotIntersect_ReturnsFalse()
    {
        RentalAgreement agreement = CreateAgreement();

        bool overlaps = agreement.Overlaps(
            new DateOnly(2026, 7, 13),
            new DateOnly(2026, 7, 15));

        Assert.False(overlaps);
    }

    [Fact]
    public void Complete_WithLateReturnAndDamage_CalculatesPenalty()
    {
        RentalAgreement agreement = CreateAgreement();

        agreement.Complete(
            new DateOnly(2026, 7, 14),
            true,
            0.5m);

        Assert.Equal(2500, agreement.Penalty);
        Assert.Equal(5500, agreement.TotalCost);
        Assert.True(agreement.HasDamage);
        Assert.True(agreement.IsCompleted);
    }

    [Fact]
    public void Complete_OnEndDateWithoutDamage_DoesNotAddPenalty()
    {
        RentalAgreement agreement = CreateAgreement();

        agreement.Complete(
            new DateOnly(2026, 7, 12),
            false,
            0.5m);

        Assert.Equal(0, agreement.Penalty);
        Assert.Equal(agreement.RentalCost, agreement.TotalCost);
    }

    [Fact]
    public void Complete_WithDamageOnly_AddsDamagePenalty()
    {
        RentalAgreement agreement = CreateAgreement();

        agreement.Complete(
            new DateOnly(2026, 7, 12),
            true,
            0.5m);

        Assert.Equal(500, agreement.Penalty);
        Assert.Equal(3500, agreement.TotalCost);
    }

    [Fact]
    public void Complete_BeforeRentalStart_ThrowsArgumentException()
    {
        RentalAgreement agreement = CreateAgreement();

        Assert.Throws<ArgumentException>(
            () => agreement.Complete(
                new DateOnly(2026, 7, 9),
                false,
                0.5m));
    }

    [Fact]
    public void Complete_WithNegativeDamageCoefficient_ThrowsArgumentOutOfRangeException()
    {
        RentalAgreement agreement = CreateAgreement();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => agreement.Complete(
                new DateOnly(2026, 7, 12),
                false,
                -0.1m));
    }

    [Fact]
    public void Complete_WhenAgreementIsAlreadyCompleted_ThrowsInvalidOperationException()
    {
        RentalAgreement agreement = CreateAgreement();
        agreement.Complete(new DateOnly(2026, 7, 12), false, 0);

        Assert.Throws<InvalidOperationException>(
            () => agreement.Complete(new DateOnly(2026, 7, 12), false, 0));
    }

    private static RentalAgreement CreateAgreement()
    {
        var request = new RentalRequest(
            TestData.CreateClient(),
            TestData.CreateCar(),
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));

        return new RentalAgreement(request, 1000);
    }
}
