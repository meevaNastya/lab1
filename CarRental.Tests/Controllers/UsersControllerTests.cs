using CarRental.Api.Controllers;
using CarRental.Api.Data;
using CarRental.Api.Dtos.Users;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Options;
using CarRental.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarRental.Tests.Controllers;

public class UsersControllerTests
{
    [Fact]
    public void UsersController_HasAdministratorAuthorization()
    {
        AuthorizeAttribute authorizeAttribute = typeof(UsersController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(UserRole.Administrator.ToString(), authorizeAttribute.Roles);
    }

    [Fact]
    public async Task CreateUser_WithManagerRole_CreatesManager()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        UsersController controller = CreateController(dbContext);
        var request = new CreateUserRequest(
            "manager",
            "secret123",
            new DateOnly(1995, 1, 1),
            null,
            new[] { UserRole.Manager });

        ActionResult<UserResponse> actionResult = await controller.CreateUser(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var response = Assert.IsType<UserResponse>(createdResult.Value);
        User savedUser = await dbContext.Users
            .Include(user => user.RoleAssignments)
            .SingleAsync();

        Assert.Equal("manager", response.Username);
        Assert.Contains(UserRole.Manager, savedUser.Roles);
        Assert.NotEqual(request.Password, savedUser.PasswordHash);
    }

    [Fact]
    public async Task AddRole_WithExistingUser_AddsRole()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        UsersController controller = CreateController(dbContext);
        User user = await AddClientAsync(dbContext);

        IActionResult result = await controller.AddRole(
            user.Id,
            new ChangeUserRoleRequest(UserRole.Manager));

        Assert.IsType<NoContentResult>(result);
        Assert.Contains(UserRole.Manager, user.Roles);
    }

    [Fact]
    public async Task GetUser_WithNonPositiveId_ReturnsBadRequest()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        UsersController controller = CreateController(dbContext);

        ActionResult<UserResponse> actionResult = await controller.GetUser(0);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsers()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        UsersController controller = CreateController(dbContext);
        await AddClientAsync(dbContext);

        ActionResult<IReadOnlyCollection<UserResponse>> actionResult =
            await controller.GetUsers();

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsAssignableFrom<IReadOnlyCollection<UserResponse>>(
            okResult.Value);
        Assert.Single(response);
    }

    [Fact]
    public async Task RemoveRole_WhenUserHasSeveralRoles_ReturnsNoContent()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        UsersController controller = CreateController(dbContext);
        User user = await AddClientAsync(dbContext);
        user.AddRole(UserRole.Manager);
        await dbContext.SaveChangesAsync();

        IActionResult result = await controller.RemoveRole(
            user.Id,
            UserRole.Manager);

        Assert.IsType<NoContentResult>(result);
        Assert.DoesNotContain(UserRole.Manager, user.Roles);
    }

    private static CarRentalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarRentalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarRentalDbContext(options);
    }

    private static UsersController CreateController(CarRentalDbContext dbContext)
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

        return new UsersController(userService, authService);
    }

    private static async Task<User> AddClientAsync(CarRentalDbContext dbContext)
    {
        var user = new User(
            "client",
            "password-hash",
            new DateOnly(1995, 1, 1),
            new DateOnly(2015, 1, 1),
            new[] { UserRole.Client });

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }
}
