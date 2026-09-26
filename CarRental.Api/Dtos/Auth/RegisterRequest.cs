namespace CarRental.Api.Dtos.Auth;

public record RegisterRequest(
    string Username,
    string Password,
    DateOnly BirthDate,
    DateOnly DriverLicenseIssueDate);
