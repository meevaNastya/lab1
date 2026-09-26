namespace CarRental.Api.Exceptions;

public class CarNotFoundException : Exception
{
    public CarNotFoundException(int carId)
        : base($"Car with ID {carId} was not found.")
    {
    }
}
