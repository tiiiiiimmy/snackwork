using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.DTOs.Auth;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Services;

public class AuthService(SnackSpotDbContext db, IConfiguration config) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var emailNormalized = request.Email.ToLowerInvariant();

        var emailExists = await db.Users.AnyAsync(u => u.Email == emailNormalized);
        if (emailExists)
            throw new InvalidOperationException("Email is already registered.");

        var usernameExists = await db.Users.AnyAsync(u => u.Username == request.Username);
        if (usernameExists)
            throw new InvalidOperationException("Username is already taken.");

        var user = new User
        {
            Username = request.Username,
            Email = emailNormalized,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return await BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var emailNormalized = request.Email.ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailNormalized && !u.IsDeleted);
        if (user is null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return await BuildAuthResponse(user);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        var token = await db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshToken && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow);

        if (token is null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        token.IsRevoked = true;

        return await BuildAuthResponse(token.User);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
        if (token is not null)
        {
            token.IsRevoked = true;
            await db.SaveChangesAsync();
        }
    }

    private async Task<AuthResponse> BuildAuthResponse(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = CreateRefreshToken(user);

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Level = user.Level,
                ExperiencePoints = user.ExperiencePoints
            }
        };
    }

    private string GenerateAccessToken(User user)
    {
        var secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var issuer = config["Jwt:Issuer"];
        var audience = config["Jwt:Audience"];
        var expiryMinutes = int.Parse(config["Jwt:AccessTokenExpiryMinutes"]!);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("username", user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken CreateRefreshToken(User user)
    {
        return new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(config["Jwt:RefreshTokenExpiryDays"]!)),
            UserId = user.Id
        };
    }
}
