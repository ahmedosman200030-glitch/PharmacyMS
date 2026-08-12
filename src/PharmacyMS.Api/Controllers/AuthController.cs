using Microsoft.AspNetCore.Mvc;
using PharmacyMS.Application.Interfaces.Services;

namespace PharmacyMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.LoginAsync(request.Username, request.Password);
        if (user == null) return Unauthorized(new { message = "Invalid username or password" });
        return Ok(user);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _authService.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword);
        if (!result) return BadRequest(new { message = "Current password is incorrect" });
        return Ok(new { message = "Password changed successfully" });
    }
}

public record LoginRequest(string Username, string Password);
public record ChangePasswordRequest(int UserId, string CurrentPassword, string NewPassword);
