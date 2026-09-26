using CarRental.Api.Enums;

namespace CarRental.Api.Entities;

public class UserRoleAssignment
{
    private UserRoleAssignment()
    {
        User = null!;
    }

    public UserRoleAssignment(User user, UserRole role)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        if (!Enum.IsDefined(role) || role == UserRole.Undefined)
            throw new ArgumentException("User role is undefined.", nameof(role));

        User = user;
        UserId = user.Id;
        Role = role;
    }

    public int UserId { get; private set; }

    public UserRole Role { get; private set; }

    public User User { get; private set; }
}
