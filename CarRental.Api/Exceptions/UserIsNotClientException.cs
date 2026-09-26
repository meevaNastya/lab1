namespace CarRental.Api.Exceptions;

public class UserIsNotClientException : Exception
{
    public UserIsNotClientException(int userId)
        : base($"User with ID {userId} does not have the client role.")
    {
    }
}
