using CarRental.Api.Dtos.Users;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Exceptions;
using CarRental.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly AuthService _authService;

    public UsersController(
        UserService userService,
        AuthService authService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> GetUsers()
    {
        IReadOnlyCollection<User> users = await _userService.GetUsersAsync();
        List<UserResponse> response = users
            .Select(CreateResponse)
            .ToList();

        return Ok(response);
    }

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<UserResponse>> GetUser(int userId)
    {
        try
        {
            User user = await _userService.GetUserAsync(userId);

            return Ok(CreateResponse(user));
        }
        catch (UserNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser(CreateUserRequest request)
    {
        try
        {
            User user = await _authService.CreateUserByAdministratorAsync(request);
            UserResponse response = CreateResponse(user);

            return CreatedAtAction(
                nameof(GetUser),
                new { userId = user.Id },
                response);
        }
        catch (UsernameAlreadyExistsException exception)
        {
            return Conflict(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("{userId:int}/roles")]
    public async Task<IActionResult> AddRole(
        int userId,
        ChangeUserRoleRequest request)
    {
        try
        {
            await _userService.AddUserRoleAsync(userId, request.Role);

            return NoContent();
        }
        catch (UserNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{userId:int}/roles/{role}")]
    public async Task<IActionResult> RemoveRole(
        int userId,
        UserRole role)
    {
        try
        {
            await _userService.RemoveUserRoleAsync(userId, role);

            return NoContent();
        }
        catch (UserNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    private static UserResponse CreateResponse(User user)
    {
        return new UserResponse(
            user.Id,
            user.Username,
            user.BirthDate,
            user.DriverLicenseIssueDate,
            user.Roles);
    }
}
