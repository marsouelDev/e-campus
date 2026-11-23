using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    ApplicationDbContext db,
    TokenService tokens,
    IConfiguration config) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var email = req.Email.Trim();
        var user = await db.Users.WithProfiles().FirstOrDefaultAsync(u => u.Email == email);

        // Même message dans tous les cas d'échec
        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Email ou mot de passe incorrect." });

        // Première connexion ou après réinitialisation ,signalé une seule fois
        var firstLogin = user.MustChangePassword;
        if (firstLogin)
        {
            user.MustChangePassword = false;
            await db.SaveChangesAsync();
        }

        return Ok(new
        {
            token = tokens.CreateToken(user),
            mustChangePassword = firstLogin,
            user = UserResponse.From(user)
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await db.Users.WithProfiles().FirstOrDefaultAsync(u => u.Id == CurrentUserId);
        return user is null ? NotFound() : Ok(UserResponse.From(user));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null || !user.IsActive) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Mot de passe actuel incorrect." });

        if (req.NewPassword == req.CurrentPassword)
            return BadRequest(new { message = "Le nouveau mot de passe doit être différent de l'ancien." });

        if (req.NewPassword == config["DefaultUserPassword"])
            return BadRequest(new { message = "Choisis un mot de passe différent de celui par défaut." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        user.MustChangePassword = false;
        await db.SaveChangesAsync();

        return Ok(new { token = tokens.CreateToken(user), role = user.Role });
    }
}