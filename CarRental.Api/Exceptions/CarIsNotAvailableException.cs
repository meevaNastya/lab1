namespace CarRental.Api.Exceptions;

public class CarIsNotAvailableException : Exception
{
    public CarIsNotAvailableException(
        int carId,
        DateOnly startDate,
        DateOnly endDate)
        : base($"Car with ID {carId} is not available from {startDate} to {endDate}.")
    {
    }
}
