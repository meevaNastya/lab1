using System.IdentityModel.Tokens.Jwt;
using CarRental.Api.Data;
using CarRental.Api.Dtos.Auth;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Exceptions;
using CarRental.Api.Options;
using CarRental.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarRental.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesClientAndReturnsToken()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        AuthService authService = CreateAuthService(dbContext);
        var request = new RegisterRequest(
            "client",
            "secret123",
            new DateOnly(2000, 1, 1),
            new DateOnly(2020, 1, 1));

        AuthResponse response = await authService.RegisterAsync(request);
        User user = await dbContext.Users
            .Include(existingUser => existingUser.RoleAssignments)
            .SingleAsync();

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.NotEqual(request.Password, user.PasswordHash);
        Assert.Single(user.Roles);
        Assert.Contains(UserRole.Client, user.Roles);
    }

    [Fact]
    public async Task RegisterAsync_WhenUsernameAlreadyExists_ThrowsUsernameAlreadyExistsException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        AuthService authService = CreateAuthService(dbContext);
        var request = new RegisterRequest(
            "client",
            "secret123",
            new DateOnly(2000, 1, 1),
            new DateOnly(2020, 1, 1));
        await authService.RegisterAsync(request);

        await Assert.ThrowsAsync<UsernameAlreadyExistsException>(
            () => authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WhenPasswordIsTooShort_ThrowsArgumentException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        AuthService authService = CreateAuthService(dbContext);
        var request = new RegisterRequest(
            "client",
            "123",
            new DateOnly(2000, 1, 1),
            new DateOnly(2020, 1, 1));

        await Assert.ThrowsAsync<ArgumentException>(
            () => authService.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_ReturnsTokenWithClientRole()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        AuthService authService = CreateAuthService(dbContext);
        await RegisterClientAsync(authService);

        AuthResponse response = await authService.LoginAsync(
            new LoginRequest("client", "secret123"));
        JwtSecurityToken token = new JwtSecurityTokenHandler()
            .ReadJwtToken(response.Token);

        Assert.Contains(token.Claims, claim => claim.Value == UserRole.Client.ToString());
        Assert.True(response.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ThrowsInvalidCredentialsException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        AuthService authService = CreateAuthService(dbContext);
        await RegisterClientAsync(authService);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => authService.LoginAsync(
                new LoginRequest("client", "wrong-password")));
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ThrowsInvalidCredentialsException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        AuthService authService = CreateAuthService(dbContext);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => authService.LoginAsync(
                new LoginRequest("unknown", "secret123")));
    }

    private static CarRentalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarRentalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarRentalDbContext(options);
    }

    private static AuthService CreateAuthService(CarRentalDbContext dbContext)
    {
        var userService = new UserService(dbContext);
        var passwordHasher = new PasswordHasher<string>();
        IOptions<JwtOptions> jwtOptions = Options.Create(
            new JwtOptions
            {
                Issuer = "CarRental.Tests",
                Audience = "CarRental.Tests.Client",
                Key = "CarRentalTestsJwtKey-WithAtLeast32Characters",
                LifetimeMinutes = 60,
            });

        return new AuthService(
            dbContext,
            userService,
            passwordHasher,
            jwtOptions);
    }

    private static Task<AuthResponse> RegisterClientAsync(AuthService authService)
    {
        return authService.RegisterAsync(
            new RegisterRequest(
                "client",
                "secret123",
                new DateOnly(2000, 1, 1),
                new DateOnly(2020, 1, 1)));
    }
}
