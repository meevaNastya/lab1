using CarRental.Api.Enums;

namespace CarRental.Api.Dtos.Users;

public record CreateUserRequest(
    string Username,
    string Password,
    DateOnly BirthDate,
    DateOnly? DriverLicenseIssueDate,
    IReadOnlyCollection<UserRole> Roles);
