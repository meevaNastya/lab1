using CarRental.Api.Enums;

namespace CarRental.Api.Dtos.Users;

public record UserResponse(
    int Id,
    string Username,
    DateOnly BirthDate,
    DateOnly? DriverLicenseIssueDate,
    IReadOnlyCollection<UserRole> Roles);
