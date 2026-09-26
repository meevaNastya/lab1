using CarRental.Api.Enums;
using CarRental.Api.Exceptions;

namespace CarRental.Api.Entities;

public class RentalRequest
{
    private RentalRequest()
    {
        Client = null!;
        Car = null!;
    }

    public RentalRequest(
        User client,
        Car car,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (client is null)
            throw new ArgumentNullException(nameof(client));

        if (car is null)
            throw new ArgumentNullException(nameof(car));

        if (startDate == default)
            throw new ArgumentException("Rental start date is required.", nameof(startDate));

        if (endDate == default)
            throw new ArgumentException("Rental end date is required.", nameof(endDate));

        if (endDate < startDate)
        {
            throw new ArgumentException(
                "Rental end date cannot be earlier than start date.",
                nameof(endDate));
        }

        Client = client;
        ClientId = client.Id;
        Car = car;
        CarId = car.Id;
        StartDate = startDate;
        EndDate = endDate;
        Status = RentalRequestStatus.Pending;
    }

    public int Id { get; private set; }

    public int ClientId { get; private set; }

    public User Client { get; private set; }

    public int CarId { get; private set; }

    public Car Car { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public RentalRequestStatus Status { get; private set; }

    public void Approve()
    {
        EnsureStatus(RentalRequestStatus.Pending);
        Status = RentalRequestStatus.Approved;
    }

    public void Reject()
    {
        EnsureStatus(RentalRequestStatus.Pending);
        Status = RentalRequestStatus.Rejected;
    }

    public void Complete()
    {
        EnsureStatus(RentalRequestStatus.Approved);
        Status = RentalRequestStatus.Completed;
    }

    private void EnsureStatus(RentalRequestStatus requiredStatus)
    {
        if (Status != requiredStatus)
            throw new InvalidRentalRequestStatusException(Status, requiredStatus);
    }
}
