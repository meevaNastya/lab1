using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CarRental.Api.Data;
using CarRental.Api.Dtos.Auth;
using CarRental.Api.Dtos.Users;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Exceptions;
using CarRental.Api.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CarRental.Api.Services;

public class AuthService
{
    private const int MinimumPasswordLength = 6;

    private readonly CarRentalDbContext _dbContext;
    private readonly UserService _userService;
    private readonly IPasswordHasher<string> _passwordHasher;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        CarRentalDbContext dbContext,
        UserService userService,
        IPasswordHasher<string> passwordHasher,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));

        ValidateJwtOptions();
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        ValidatePassword(request.Password);

        string passwordHash = _passwordHasher.HashPassword(
            request.Username,
            request.Password);

        User user = await _userService.CreateUserAsync(
            request.Username,
            passwordHash,
            request.BirthDate,
            request.DriverLicenseIssueDate,
            new[] { UserRole.Client });

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        User? user = await _dbContext.Users
            .Include(existingUser => existingUser.RoleAssignments)
            .SingleOrDefaultAsync(existingUser => existingUser.Username == request.Username);

        if (user is null)
            throw new InvalidCredentialsException();

        PasswordVerificationResult verificationResult = _passwordHasher.VerifyHashedPassword(
            user.Username,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
            throw new InvalidCredentialsException();

        return CreateAuthResponse(user);
    }

    public async Task<User> CreateUserByAdministratorAsync(CreateUserRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        ValidatePassword(request.Password);

        string passwordHash = _passwordHasher.HashPassword(
            request.Username,
            request.Password);

        return await _userService.CreateUserAsync(
            request.Username,
            passwordHash,
            request.BirthDate,
            request.DriverLicenseIssueDate,
            request.Roles);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.LifetimeMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
        };

        claims.AddRange(
            user.Roles.Select(
                role => new Claim(ClaimTypes.Role, role.ToString())));

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);
        string tokenValue = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new AuthResponse(tokenValue, expiresAt);
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is empty.", nameof(password));

        if (password.Length < MinimumPasswordLength)
        {
            throw new ArgumentException(
                $"Password must contain at least {MinimumPasswordLength} characters.",
                nameof(password));
        }
    }

    private void ValidateJwtOptions()
    {
        if (string.IsNullOrWhiteSpace(_jwtOptions.Issuer))
            throw new InvalidOperationException("JWT issuer is not configured.");

        if (string.IsNullOrWhiteSpace(_jwtOptions.Audience))
            throw new InvalidOperationException("JWT audience is not configured.");

        if (_jwtOptions.Key.Length < 32)
            throw new InvalidOperationException("JWT key must contain at least 32 characters.");

        if (_jwtOptions.LifetimeMinutes <= 0)
            throw new InvalidOperationException("JWT lifetime must be positive.");
    }
}
