using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Exceptions;

namespace CarRental.Tests.Entities;

public class RentalRequestTests
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesPendingRequest()
    {
        RentalRequest request = CreateRequest();

        Assert.Equal(RentalRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Approve_WhenRequestIsPending_ChangesStatusToApproved()
    {
        RentalRequest request = CreateRequest();

        request.Approve();

        Assert.Equal(RentalRequestStatus.Approved, request.Status);
    }

    [Fact]
    public void Reject_WhenRequestIsPending_ChangesStatusToRejected()
    {
        RentalRequest request = CreateRequest();

        request.Reject();

        Assert.Equal(RentalRequestStatus.Rejected, request.Status);
    }

    [Fact]
    public void Complete_WhenRequestIsApproved_ChangesStatusToCompleted()
    {
        RentalRequest request = CreateRequest();
        request.Approve();

        request.Complete();

        Assert.Equal(RentalRequestStatus.Completed, request.Status);
    }

    [Fact]
    public void Approve_WhenRequestIsAlreadyRejected_ThrowsInvalidRentalRequestStatusException()
    {
        RentalRequest request = CreateRequest();
        request.Reject();

        Assert.Throws<InvalidRentalRequestStatusException>(request.Approve);
    }

    [Fact]
    public void Constructor_WhenEndDateIsEarlierThanStartDate_ThrowsArgumentException()
    {
        User client = TestData.CreateClient();
        Car car = TestData.CreateCar();

        Assert.Throws<ArgumentException>(
            () => new RentalRequest(
                client,
                car,
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 9)));
    }

    private static RentalRequest CreateRequest()
    {
        return new RentalRequest(
            TestData.CreateClient(),
            TestData.CreateCar(),
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));
    }

    [Fact]
    public void Constructor_WhenEndDateIsMissing_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new RentalRequest(
                TestData.CreateClient(),
                TestData.CreateCar(),
                new DateOnly(2026, 7, 10),
                default));
    }
}
