using FlutterPlatform.Application.Commands.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlutterPlatform.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>Login and obtain JWT tokens</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(req.Email, req.Password), ct);
        if (!result.Success) return Unauthorized(new { error = result.Error });
        return Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresAt = result.ExpiresAt
        });
    }

    /// <summary>Register a new user</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterCommand(req.Email, req.Username, req.Password), ct);
        if (!result.Success) return BadRequest(new { error = result.Error });
        return Created($"/api/v1/users/{result.UserId}", new { userId = result.UserId });
    }
}

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Username, string Password);
