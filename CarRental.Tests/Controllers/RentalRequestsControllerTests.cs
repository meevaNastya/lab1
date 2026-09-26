using System.Reflection;
using System.Security.Claims;
using CarRental.Api.Controllers;
using CarRental.Api.Data;
using CarRental.Api.Dtos.Rentals;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Tests.Controllers;

public class RentalRequestsControllerTests
{
    [Fact]
    public void CreateRentalRequest_HasClientAuthorization()
    {
        MethodInfo method = GetMethod(nameof(RentalRequestsController.CreateRentalRequest));
        AuthorizeAttribute authorizeAttribute = method
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();

        Assert.Equal(UserRole.Client.ToString(), authorizeAttribute.Roles);
    }

    [Fact]
    public void ApproveRequest_HasManagerAndAdministratorAuthorization()
    {
        MethodInfo method = GetMethod(nameof(RentalRequestsController.ApproveRequest));
        AuthorizeAttribute authorizeAttribute = method
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();

        Assert.Equal("Manager,Administrator", authorizeAttribute.Roles);
    }

    [Fact]
    public async Task CreateRentalRequest_UsesClientIdFromClaims()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddClientAsync(dbContext);
        Car car = await AddCarAsync(dbContext);
        RentalRequestsController controller = CreateController(dbContext, client.Id);
        var request = new CreateRentalRequest(
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));

        ActionResult<RentalRequestResponse> actionResult =
            await controller.CreateRentalRequest(request);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<RentalRequestResponse>(okResult.Value);
        Assert.Equal(client.Id, response.ClientId);
        Assert.Equal(RentalRequestStatus.Pending, response.Status);
    }

    [Fact]
    public async Task CreateRentalRequest_WithoutUserIdClaim_ReturnsUnauthorized()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        Car car = await AddCarAsync(dbContext);
        RentalRequestsController controller = CreateController(dbContext, null);

        ActionResult<RentalRequestResponse> actionResult =
            await controller.CreateRentalRequest(
                new CreateRentalRequest(
                    car.Id,
                    new DateOnly(2026, 7, 10),
                    new DateOnly(2026, 7, 12)));

        Assert.IsType<UnauthorizedResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetMyRequests_ReturnsOnlyCurrentUserRequests()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User firstClient = await AddClientAsync(dbContext, "first-client");
        User secondClient = await AddClientAsync(dbContext, "second-client");
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);
        await rentalService.CreateRentalRequestAsync(
            firstClient.Id,
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));
        await rentalService.CreateRentalRequestAsync(
            secondClient.Id,
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));
        RentalRequestsController controller = CreateController(dbContext, firstClient.Id);

        ActionResult<IReadOnlyCollection<RentalRequestResponse>> actionResult =
            await controller.GetMyRequests();

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsAssignableFrom<IReadOnlyCollection<RentalRequestResponse>>(
            okResult.Value);
        Assert.Single(response);
        Assert.Equal(firstClient.Id, response.Single().ClientId);
    }

    [Fact]
    public async Task ApproveRequest_ForPendingRequest_ReturnsAgreement()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddClientAsync(dbContext);
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);
        RentalRequest request = await rentalService.CreateRentalRequestAsync(
            client.Id,
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));
        RentalRequestsController controller = CreateController(dbContext, null);

        ActionResult<RentalAgreementResponse> actionResult =
            await controller.ApproveRequest(request.Id);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<RentalAgreementResponse>(okResult.Value);
        Assert.Equal(request.Id, response.RentalRequestId);
        Assert.Equal(9000, response.RentalCost);
    }

    [Fact]
    public async Task ApproveRequest_WithNonPositiveId_ReturnsBadRequest()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        RentalRequestsController controller = CreateController(dbContext, null);

        ActionResult<RentalAgreementResponse> actionResult =
            await controller.ApproveRequest(0);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetRequests_ReturnsAllRequests()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddClientAsync(dbContext);
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);
        await rentalService.CreateRentalRequestAsync(
            client.Id,
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));
        RentalRequestsController controller = CreateController(dbContext, null);

        ActionResult<IReadOnlyCollection<RentalRequestResponse>> actionResult =
            await controller.GetRequests();

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsAssignableFrom<IReadOnlyCollection<RentalRequestResponse>>(
            okResult.Value);
        Assert.Single(response);
    }

    [Fact]
    public async Task RejectRequest_ForPendingRequest_ReturnsNoContent()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddClientAsync(dbContext);
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);
        RentalRequest request = await rentalService.CreateRentalRequestAsync(
            client.Id,
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));
        RentalRequestsController controller = CreateController(dbContext, null);

        IActionResult result = await controller.RejectRequest(request.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(RentalRequestStatus.Rejected, request.Status);
    }

    [Fact]
    public async Task RejectRequest_WhenRequestDoesNotExist_ReturnsNotFound()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        RentalRequestsController controller = CreateController(dbContext, null);

        IActionResult result = await controller.RejectRequest(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CompleteRental_ForApprovedRequest_ReturnsCompletedAgreement()
    {
        await using CarRentalDbContext dbContext = CreateDbContext();
        User client = await AddClientAsync(dbContext);
        Car car = await AddCarAsync(dbContext);
        var rentalService = new RentalService(dbContext);
        RentalRequest request = await rentalService.CreateRentalRequestAsync(
            client.Id,
            car.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12));
        await rentalService.ApproveRentalRequestAsync(request.Id);
        RentalRequestsController controller = CreateController(dbContext, null);

        ActionResult<RentalAgreementResponse> actionResult =
            await controller.CompleteRental(
                request.Id,
                new CompleteRentalRequest(
                    new DateOnly(2026, 7, 14),
                    true));

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<RentalAgreementResponse>(okResult.Value);
        Assert.True(response.IsCompleted);
        Assert.Equal(16500, response.TotalCost);
    }

    private static MethodInfo GetMethod(string methodName)
    {
        return typeof(RentalRequestsController)
            .GetMethod(methodName)
            ?? throw new InvalidOperationException($"{methodName} method was not found.");
    }

    private static CarRentalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarRentalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarRentalDbContext(options);
    }

    private static RentalRequestsController CreateController(
        CarRentalDbContext dbContext,
        int? userId)
    {
        var controller = new RentalRequestsController(new RentalService(dbContext));
        var claims = new List<Claim>();

        if (userId.HasValue)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")),
            },
        };

        return controller;
    }

    private static async Task<User> AddClientAsync(
        CarRentalDbContext dbContext,
        string username = "client")
    {
        var client = new User(
            username,
            "password-hash",
            new DateOnly(1995, 1, 1),
            new DateOnly(2015, 1, 1),
            new[] { UserRole.Client });

        dbContext.Users.Add(client);
        await dbContext.SaveChangesAsync();

        return client;
    }

    private static async Task<Car> AddCarAsync(CarRentalDbContext dbContext)
    {
        var car = new Car(
            "Toyota",
            "Corolla",
            "JTDBR32E720123456",
            CarCategory.Comfort,
            3000);

        dbContext.Cars.Add(car);
        await dbContext.SaveChangesAsync();

        return car;
    }
}
