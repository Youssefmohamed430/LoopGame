using LoopGame.Application.Dtos.AuthServiceDtos;
using LoopGame.Application.IServices.SystemAndUtilityServices;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LoopGame.Controllers;

/// <summary>
/// Authentication endpoints: register, login, token refresh, password reset, logout.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService _auth) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserToReturnDto>> Register([FromBody] RegisterDto request)
    {
        var result = await _auth.RegisterAsync(request);
        return result.IsFailure ? result.Error.ToActionResult() : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserToReturnDto>> Login([FromBody] LoginDto request)
    {
        var result = await _auth.LoginAsync(request);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _auth.ForgotPasswordAsync(request);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(new { message = "If the email exists, a reset code has been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _auth.ResetPasswordAsync(request);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(new { message = "Password reset successfully." });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<UserToReturnDto>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _auth.RefreshTokenAsync(request);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] RefreshTokenRequest? request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _auth.LogoutAsync(userId, request?.RefreshToken);
        return NoContent();
    }
}
