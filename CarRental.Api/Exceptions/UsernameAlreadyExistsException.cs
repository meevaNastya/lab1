namespace CarRental.Api.Exceptions;

public class UsernameAlreadyExistsException : Exception
{
    public UsernameAlreadyExistsException(string username)
        : base($"User with username '{username}' already exists.")
    {
    }
}
