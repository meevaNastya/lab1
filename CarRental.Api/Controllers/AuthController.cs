using CarRental.Api.Dtos.Auth;
using CarRental.Api.Exceptions;
using CarRental.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        try
        {
            AuthResponse response = await _authService.RegisterAsync(request);

            return Ok(response);
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

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        try
        {
            AuthResponse response = await _authService.LoginAsync(request);

            return Ok(response);
        }
        catch (InvalidCredentialsException exception)
        {
            return Unauthorized(exception.Message);
        }
    }
}
