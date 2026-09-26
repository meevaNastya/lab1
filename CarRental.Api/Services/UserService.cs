using CarRental.Api.Data;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Api.Services;

public class UserService
{
    private readonly CarRentalDbContext _dbContext;

    public UserService(CarRentalDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<User> CreateUserAsync(
        string username,
        string passwordHash,
        DateOnly birthDate,
        DateOnly? driverLicenseIssueDate,
        IEnumerable<UserRole> roles)
    {
        string normalizedUsername = username?.Trim()
            ?? throw new ArgumentNullException(nameof(username));
        bool usernameExists = await _dbContext.Users
            .AnyAsync(user => user.Username == normalizedUsername);

        if (usernameExists)
            throw new UsernameAlreadyExistsException(normalizedUsername);

        var user = new User(
            normalizedUsername,
            passwordHash,
            birthDate,
            driverLicenseIssueDate,
            roles);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<User?> FindUserAsync(int userId)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId), "User ID must be positive.");

        return await _dbContext.Users
            .Include(user => user.RoleAssignments)
            .SingleOrDefaultAsync(user => user.Id == userId);
    }

    public async Task<User> GetUserAsync(int userId)
    {
        User? user = await FindUserAsync(userId);

        if (user is null)
            throw new UserNotFoundException(userId);

        return user;
    }

    public async Task<IReadOnlyCollection<User>> GetUsersAsync()
    {
        return await _dbContext.Users
            .Include(user => user.RoleAssignments)
            .OrderBy(user => user.Id)
            .ToListAsync();
    }

    public async Task AddUserRoleAsync(int userId, UserRole role)
    {
        User user = await GetUserAsync(userId);

        user.AddRole(role);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveUserRoleAsync(int userId, UserRole role)
    {
        User user = await GetUserAsync(userId);

        user.RemoveRole(role);
        await _dbContext.SaveChangesAsync();
    }
}
