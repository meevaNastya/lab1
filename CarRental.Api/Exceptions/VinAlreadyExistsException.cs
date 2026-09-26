namespace CarRental.Api.Exceptions;

public class VinAlreadyExistsException : Exception
{
    public VinAlreadyExistsException(string vin)
        : base($"Car with VIN '{vin}' already exists.")
    {
    }
}
