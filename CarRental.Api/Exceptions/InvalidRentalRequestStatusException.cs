using CarRental.Api.Enums;

namespace CarRental.Api.Exceptions;

public class InvalidRentalRequestStatusException : Exception
{
    public InvalidRentalRequestStatusException(
        RentalRequestStatus currentStatus,
        RentalRequestStatus requiredStatus)
        : base(
            $"Rental request status is {currentStatus}, but {requiredStatus} is required.")
    {
    }
}
