using CarRental.Api.Entities;
using CarRental.Api.Enums;

namespace CarRental.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_WithClientRoleAndDriverLicense_CreatesUser()
    {
        User user = CreateClient();

        Assert.Equal("client", user.Username);
        Assert.Contains(UserRole.Client, user.Roles);
    }

    [Fact]
    public void Constructor_WithClientRoleWithoutDriverLicense_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new User(
                "client",
                "password-hash",
                new DateOnly(2000, 1, 1),
                null,
                new[] { UserRole.Client }));
    }

    [Fact]
    public void Constructor_WithFutureBirthDate_ThrowsArgumentException()
    {
        DateOnly tomorrow = DateOnly.FromDateTime(DateTime.Today).AddDays(1);

        Assert.Throws<ArgumentException>(
            () => new User(
                "client",
                "password-hash",
                tomorrow,
                null,
                new[] { UserRole.Manager }));
    }

    [Fact]
    public void Constructor_WithFutureDriverLicenseDate_ThrowsArgumentException()
    {
        DateOnly tomorrow = DateOnly.FromDateTime(DateTime.Today).AddDays(1);

        Assert.Throws<ArgumentException>(
            () => new User(
                "client",
                "password-hash",
                new DateOnly(2000, 1, 1),
                tomorrow,
                new[] { UserRole.Client }));
    }

    [Fact]
    public void Constructor_WithLongUsername_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new User(
                new string('a', 51),
                "password-hash",
                new DateOnly(2000, 1, 1),
                null,
                new[] { UserRole.Manager }));
    }

    [Fact]
    public void AddRole_WithNewRole_AddsRole()
    {
        User user = CreateClient();

        user.AddRole(UserRole.Manager);

        Assert.Contains(UserRole.Manager, user.Roles);
        Assert.True(user.HasRole(UserRole.Manager));
    }

    [Fact]
    public void AddRole_WithExistingRole_DoesNotDuplicateRole()
    {
        User user = CreateClient();

        user.AddRole(UserRole.Client);

        Assert.Single(user.Roles);
    }

    [Fact]
    public void RemoveRole_WhenItIsLastRole_ThrowsInvalidOperationException()
    {
        User user = CreateClient();

        Assert.Throws<InvalidOperationException>(() => user.RemoveRole(UserRole.Client));
    }

    [Fact]
    public void RemoveRole_WithUnknownRole_ThrowsArgumentException()
    {
        User user = CreateClient();

        Assert.Throws<ArgumentException>(() => user.RemoveRole((UserRole)999));
    }

    [Fact]
    public void GetAgeOn_BeforeBirthday_ReturnsCompletedYears()
    {
        User user = CreateClient();

        int age = user.GetAgeOn(new DateOnly(2026, 5, 31));

        Assert.Equal(25, age);
    }

    [Fact]
    public void GetDrivingExperienceYearsOn_ReturnsCompletedYears()
    {
        User user = CreateClient();

        int experienceYears = user.GetDrivingExperienceYearsOn(new DateOnly(2026, 6, 1));

        Assert.Equal(6, experienceYears);
    }

    private static User CreateClient()
    {
        return new User(
            "client",
            "password-hash",
            new DateOnly(2000, 6, 1),
            new DateOnly(2020, 6, 1),
            new[] { UserRole.Client });
    }
}
