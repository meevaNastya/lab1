using CarRental.Api.Controllers;
using CarRental.Api.Data;
using CarRental.Api.Dtos.Auth;
using CarRental.Api.Options;
using CarRental.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarRental.Tests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_WithValidRequest_ReturnsToken()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        AuthController controller = CreateController(dbContext);
        RegisterRequest request = CreateRegisterRequest();

        ActionResult<AuthResponse> actionResult = await controller.Register(request);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task Register_WhenUsernameAlreadyExists_ReturnsConflict()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        AuthController controller = CreateController(dbContext);
        RegisterRequest request = CreateRegisterRequest();
        await controller.Register(request);

        ActionResult<AuthResponse> actionResult = await controller.Register(request);

        Assert.IsType<ConflictObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        AuthController controller = CreateController(dbContext);
        var request = new RegisterRequest(
            "client",
            "123",
            new DateOnly(2000, 1, 1),
            new DateOnly(2020, 1, 1));

        ActionResult<AuthResponse> actionResult = await controller.Register(request);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsToken()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        AuthController controller = CreateController(dbContext);
        await controller.Register(CreateRegisterRequest());

        ActionResult<AuthResponse> actionResult = await controller.Login(
            new LoginRequest("client", "secret123"));

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.IsType<AuthResponse>(okResult.Value);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        AuthController controller = CreateController(dbContext);
        await controller.Register(CreateRegisterRequest());

        ActionResult<AuthResponse> actionResult = await controller.Login(
            new LoginRequest("client", "wrong-password"));

        Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
    }

    private static CarRentalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarRentalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarRentalDbContext(options);
    }

    private static AuthController CreateController(CarRentalDbContext dbContext)
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
        var authService = new AuthService(
            dbContext,
            userService,
            passwordHasher,
            jwtOptions);

        return new AuthController(authService);
    }

    private static RegisterRequest CreateRegisterRequest()
    {
        return new RegisterRequest(
            "client",
            "secret123",
            new DateOnly(2000, 1, 1),
            new DateOnly(2020, 1, 1));
    }
}
