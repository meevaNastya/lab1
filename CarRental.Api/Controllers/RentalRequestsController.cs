using System.Security.Claims;
using CarRental.Api.Dtos.Rentals;
using CarRental.Api.Entities;
using CarRental.Api.Exceptions;
using CarRental.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Controllers;

[ApiController]
[Route("api/rental-requests")]
[Authorize]
public class RentalRequestsController : ControllerBase
{
    private const string ManagementRoles = "Manager,Administrator";

    private readonly RentalService _rentalService;

    public RentalRequestsController(RentalService rentalService)
    {
        _rentalService = rentalService ?? throw new ArgumentNullException(nameof(rentalService));
    }

    [Authorize(Roles = "Client")]
    [HttpPost]
    public async Task<ActionResult<RentalRequestResponse>> CreateRentalRequest(
        CreateRentalRequest request)
    {
        if (!TryGetCurrentUserId(out int clientId))
            return Unauthorized();

        try
        {
            RentalRequest rentalRequest = await _rentalService.CreateRentalRequestAsync(
                clientId,
                request.CarId,
                request.StartDate,
                request.EndDate);

            return Ok(CreateResponse(rentalRequest));
        }
        catch (CarNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (CarIsNotAvailableException exception)
        {
            return Conflict(exception.Message);
        }
        catch (DriverRequirementsNotMetException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "Client")]
    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyCollection<RentalRequestResponse>>> GetMyRequests()
    {
        if (!TryGetCurrentUserId(out int clientId))
            return Unauthorized();

        try
        {
            IReadOnlyCollection<RentalRequest> requests =
                await _rentalService.GetClientRentalRequestsAsync(clientId);
            List<RentalRequestResponse> response = requests
                .Select(CreateResponse)
                .ToList();

            return Ok(response);
        }
        catch (UserNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
    }

    [Authorize(Roles = ManagementRoles)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RentalRequestResponse>>> GetRequests()
    {
        IReadOnlyCollection<RentalRequest> requests =
            await _rentalService.GetRentalRequestsAsync();
        List<RentalRequestResponse> response = requests
            .Select(CreateResponse)
            .ToList();

        return Ok(response);
    }

    [Authorize(Roles = ManagementRoles)]
    [HttpPost("{rentalRequestId:int}/approve")]
    public async Task<ActionResult<RentalAgreementResponse>> ApproveRequest(
        int rentalRequestId)
    {
        try
        {
            RentalAgreement agreement =
                await _rentalService.ApproveRentalRequestAsync(rentalRequestId);

            return Ok(CreateResponse(agreement));
        }
        catch (RentalRequestNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (CarIsNotAvailableException exception)
        {
            return Conflict(exception.Message);
        }
        catch (InvalidRentalRequestStatusException exception)
        {
            return Conflict(exception.Message);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = ManagementRoles)]
    [HttpPost("{rentalRequestId:int}/reject")]
    public async Task<IActionResult> RejectRequest(int rentalRequestId)
    {
        try
        {
            await _rentalService.RejectRentalRequestAsync(rentalRequestId);

            return NoContent();
        }
        catch (RentalRequestNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (InvalidRentalRequestStatusException exception)
        {
            return Conflict(exception.Message);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = ManagementRoles)]
    [HttpPost("{rentalRequestId:int}/complete")]
    public async Task<ActionResult<RentalAgreementResponse>> CompleteRental(
        int rentalRequestId,
        CompleteRentalRequest request)
    {
        try
        {
            RentalAgreement agreement = await _rentalService.CompleteRentalAsync(
                rentalRequestId,
                request.ActualReturnDate,
                request.HasDamage);

            return Ok(CreateResponse(agreement));
        }
        catch (RentalAgreementNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (InvalidRentalRequestStatusException exception)
        {
            return Conflict(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userIdValue, out userId) && userId > 0;
    }

    private static RentalRequestResponse CreateResponse(RentalRequest request)
    {
        return new RentalRequestResponse(
            request.Id,
            request.ClientId,
            request.Client.Username,
            request.CarId,
            $"{request.Car.Brand} {request.Car.Model}",
            request.StartDate,
            request.EndDate,
            request.Status);
    }

    private static RentalAgreementResponse CreateResponse(RentalAgreement agreement)
    {
        return new RentalAgreementResponse(
            agreement.Id,
            agreement.RentalRequestId,
            agreement.ClientId,
            agreement.CarId,
            agreement.StartDate,
            agreement.EndDate,
            agreement.PricePerDay,
            agreement.RentalCost,
            agreement.Penalty,
            agreement.TotalCost,
            agreement.ActualReturnDate,
            agreement.HasDamage,
            agreement.IsCompleted);
    }
}
