namespace CarRental.Api.Dtos.Auth;

public record LoginRequest(
    string Username,
    string Password);
