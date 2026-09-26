using CarRental.Api.Data;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Exceptions;
using CarRental.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task CreateUserAsync_WithValidArguments_SavesUser()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var userService = new UserService(dbContext);

        User user = await userService.CreateUserAsync(
            "client",
            "password-hash",
            new DateOnly(2000, 1, 1),
            new DateOnly(2020, 1, 1),
            new[] { UserRole.Client });

        Assert.True(user.Id > 0);
        Assert.Equal("client", user.Username);
        Assert.Contains(UserRole.Client, user.Roles);
        Assert.Equal(1, await dbContext.Users.CountAsync());
    }

    [Fact]
    public async Task CreateUserAsync_WhenUsernameAlreadyExists_ThrowsUsernameAlreadyExistsException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var userService = new UserService(dbContext);
        await CreateClientAsync(userService);

        await Assert.ThrowsAsync<UsernameAlreadyExistsException>(
            () => CreateClientAsync(userService));
    }

    [Fact]
    public async Task FindUserAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var userService = new UserService(dbContext);

        User? user = await userService.FindUserAsync(1);

        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserAsync_WhenUserDoesNotExist_ThrowsUserNotFoundException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var userService = new UserService(dbContext);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => userService.GetUserAsync(1));
    }

    [Fact]
    public async Task GetUsersAsync_WhenUsersDoNotExist_ReturnsEmptyCollection()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var userService = new UserService(dbContext);

        IReadOnlyCollection<User> users = await userService.GetUsersAsync();

        Assert.Empty(users);
    }

    [Fact]
    public async Task AddUserRoleAsync_WithNewRole_AddsRole()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var userService = new UserService(dbContext);
        User user = await CreateClientAsync(userService);

        await userService.AddUserRoleAsync(user.Id, UserRole.Manager);
        dbContext.ChangeTracker.Clear();

        User savedUser = await userService.GetUserAsync(user.Id);

        Assert.Contains(UserRole.Manager, savedUser.Roles);
    }

    [Fact]
    public async Task AddUserRoleAsync_WithExistingRole_DoesNotDuplicateRole()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var userService = new UserService(dbContext);
        User user = await CreateClientAsync(userService);

        await userService.AddUserRoleAsync(user.Id, UserRole.Client);

        Assert.Single(user.Roles);
    }

    [Fact]
    public async Task RemoveUserRoleAsync_WhenUserHasSeveralRoles_RemovesRole()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var userService = new UserService(dbContext);
        User user = await userService.CreateUserAsync(
            "manager",
            "password-hash",
            new DateOnly(2000, 1, 1),
            new DateOnly(2020, 1, 1),
            new[] { UserRole.Client, UserRole.Manager });

        await userService.RemoveUserRoleAsync(user.Id, UserRole.Manager);
        dbContext.ChangeTracker.Clear();

        User savedUser = await userService.GetUserAsync(user.Id);

        Assert.DoesNotContain(UserRole.Manager, savedUser.Roles);
        Assert.Contains(UserRole.Client, savedUser.Roles);
    }

    [Fact]
    public async Task RemoveUserRoleAsync_WhenRoleIsLast_ThrowsInvalidOperationException()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        var userService = new UserService(dbContext);
        User user = await CreateClientAsync(userService);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => userService.RemoveUserRoleAsync(user.Id, UserRole.Client));
    }

    private static CarRentalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarRentalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarRentalDbContext(options);
    }

    private static Task<User> CreateClientAsync(UserService userService)
    {
        return userService.CreateUserAsync(
            "client",
            "password-hash",
            new DateOnly(2000, 1, 1),
            new DateOnly(2020, 1, 1),
            new[] { UserRole.Client });
    }
}
