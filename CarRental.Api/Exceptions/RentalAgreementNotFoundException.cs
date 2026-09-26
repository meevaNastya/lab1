namespace CarRental.Api.Exceptions;

public class RentalAgreementNotFoundException : Exception
{
    public RentalAgreementNotFoundException(int identifier)
        : base($"Rental agreement for identifier {identifier} was not found.")
    {
    }
}
