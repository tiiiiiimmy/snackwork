using Microsoft.AspNetCore.Mvc;
using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Auth;
using SnackSpot.Api.Services;

namespace SnackSpot.Api.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshTokenCookie = "refreshToken";

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Registration successful."));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Login successful."));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var token = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrEmpty(token))
            return Unauthorized(ApiResponse.Fail("Refresh token not found."));

        var result = await authService.RefreshAsync(token);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies[RefreshTokenCookie];
        if (!string.IsNullOrEmpty(token))
            await authService.LogoutAsync(token);

        Response.Cookies.Delete(RefreshTokenCookie);
        return Ok(ApiResponse.Ok("Logged out successfully."));
    }

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append(RefreshTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }
}
