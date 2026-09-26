namespace CarRental.Api.Exceptions;

public class RentalRequestNotFoundException : Exception
{
    public RentalRequestNotFoundException(int rentalRequestId)
        : base($"Rental request with ID {rentalRequestId} was not found.")
    {
    }
}
