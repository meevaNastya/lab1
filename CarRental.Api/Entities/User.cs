using CarRental.Api.Enums;

namespace CarRental.Api.Entities;

public class User
{
    private const int MaximumUsernameLength = 50;

    private readonly List<UserRoleAssignment> _roleAssignments;

    private User()
    {
        _roleAssignments = new List<UserRoleAssignment>();

        Username = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(
        string username,
        string passwordHash,
        DateOnly birthDate,
        DateOnly? driverLicenseIssueDate,
        IEnumerable<UserRole> roles)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is empty.", nameof(username));

        if (username.Length > MaximumUsernameLength)
        {
            throw new ArgumentException(
                $"Username cannot contain more than {MaximumUsernameLength} characters.",
                nameof(username));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is empty.", nameof(passwordHash));

        if (roles is null)
            throw new ArgumentNullException(nameof(roles));

        List<UserRole> userRoles = roles
            .Distinct()
            .ToList();

        if (userRoles.Count == 0)
            throw new ArgumentException("User must have at least one role.", nameof(roles));

        if (userRoles.Any(role => !Enum.IsDefined(role) || role == UserRole.Undefined))
            throw new ArgumentException("User role is undefined.", nameof(roles));

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        if (birthDate == default || birthDate > today)
            throw new ArgumentException("Birth date cannot be in the future.", nameof(birthDate));

        if (driverLicenseIssueDate < birthDate)
        {
            throw new ArgumentException(
                "Driver license issue date cannot be earlier than birth date.",
                nameof(driverLicenseIssueDate));
        }

        if (driverLicenseIssueDate > today)
        {
            throw new ArgumentException(
                "Driver license issue date cannot be in the future.",
                nameof(driverLicenseIssueDate));
        }

        if (userRoles.Contains(UserRole.Client) && driverLicenseIssueDate is null)
        {
            throw new ArgumentException(
                "Client must have a driver license issue date.",
                nameof(driverLicenseIssueDate));
        }

        Username = username.Trim();
        PasswordHash = passwordHash;
        BirthDate = birthDate;
        DriverLicenseIssueDate = driverLicenseIssueDate;
        _roleAssignments = userRoles
            .Select(role => new UserRoleAssignment(this, role))
            .ToList();
    }

    public int Id { get; private set; }

    public string Username { get; private set; }

    public string PasswordHash { get; private set; }

    public DateOnly BirthDate { get; private set; }

    public DateOnly? DriverLicenseIssueDate { get; private set; }

    public IReadOnlyCollection<UserRoleAssignment> RoleAssignments => _roleAssignments;

    public IReadOnlyCollection<UserRole> Roles => _roleAssignments
        .Select(assignment => assignment.Role)
        .ToList();

    public void AddRole(UserRole role)
    {
        if (!Enum.IsDefined(role) || role == UserRole.Undefined)
            throw new ArgumentException("User role is undefined.", nameof(role));

        if (role == UserRole.Client && DriverLicenseIssueDate is null)
        {
            throw new InvalidOperationException(
                "User without a driver license issue date cannot become a client.");
        }

        if (HasRole(role))
            return;

        _roleAssignments.Add(new UserRoleAssignment(this, role));
    }

    public void RemoveRole(UserRole role)
    {
        if (!Enum.IsDefined(role) || role == UserRole.Undefined)
            throw new ArgumentException("User role is undefined.", nameof(role));

        UserRoleAssignment? roleAssignment = _roleAssignments
            .SingleOrDefault(assignment => assignment.Role == role);

        if (roleAssignment is null)
            return;

        if (_roleAssignments.Count == 1)
            throw new InvalidOperationException("User must have at least one role.");

        _roleAssignments.Remove(roleAssignment);
    }

    public bool HasRole(UserRole role)
    {
        return _roleAssignments.Any(assignment => assignment.Role == role);
    }

    public int GetAgeOn(DateOnly date)
    {
        return CalculateFullYears(BirthDate, date);
    }

    public int GetDrivingExperienceYearsOn(DateOnly date)
    {
        if (DriverLicenseIssueDate is null)
            throw new InvalidOperationException("User does not have a driver license.");

        return CalculateFullYears(DriverLicenseIssueDate.Value, date);
    }

    public void ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is empty.", nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    private static int CalculateFullYears(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException(
                "End date cannot be earlier than start date.",
                nameof(endDate));
        }

        int fullYears = endDate.Year - startDate.Year;

        if (startDate.AddYears(fullYears) > endDate)
            fullYears--;

        return fullYears;
    }
}
