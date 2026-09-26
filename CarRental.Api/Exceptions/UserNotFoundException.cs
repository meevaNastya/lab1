namespace CarRental.Api.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(int userId)
        : base($"User with ID {userId} was not found.")
    {
    }
}
