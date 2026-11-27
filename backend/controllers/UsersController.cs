using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Responsable}")]
public class UsersController(
    ApplicationDbContext db,
    IConfiguration config,
    EmailService mailer) : ControllerBase
{
    // Un Responsable ne peut pas gérer les Admin ni les autres Responsables
    private bool CanManage(string targetRole) =>
        !Roles.AdminOnly.Contains(targetRole) || User.IsInRole(Roles.Admin);

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // MODIFIÉ : erreur claire si la clé manque (au lieu d'une NullReferenceException)
    private string DefaultPassword =>
        config["DefaultUserPassword"] is { Length: > 0 } pwd
            ? pwd
            : throw new InvalidOperationException(
                "DefaultUserPassword est manquante (docker-compose, .env ou user-secrets).");

    private string DefaultPasswordHash() => BCrypt.Net.BCrypt.HashPassword(DefaultPassword);

    private async Task<bool> AllAreParents(List<int> ids) =>
        ids.Count == 0 ||
        await db.Users.CountAsync(u => ids.Contains(u.Id) && u.Role == Roles.Parent) == ids.Count;

    private static string NewMatricule(string prefix, int userId) =>
        $"{prefix}{DateTime.UtcNow.Year}{userId:D4}";

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();


    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll([FromQuery] string? role)
    {
        var query = db.Users.WithProfiles();
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        var users = await query.OrderBy(u => u.FullName).ToListAsync();
        return Ok(users.Select(UserResponse.From));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetOne(int id)
    {
        var user = await db.Users.WithProfiles().FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? NotFound() : Ok(UserResponse.From(user));
    }


    [HttpPost("create")]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest req)
    {
        if (!Roles.All.Contains(req.Role))
            return BadRequest(new { message = "Rôle invalide." });

        if (!CanManage(req.Role))
            return Forbid();

        var email = req.Email.Trim();
        if (await db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "Cet email existe déjà." });

        var parentIds = req.ParentIds?.Distinct().ToList() ?? [];
        if (req.Role == Roles.Etudiant && !await AllAreParents(parentIds))
            return BadRequest(new { message = "Un des parents sélectionnés est invalide." });

        await using var tx = await db.Database.BeginTransactionAsync();

        var user = new User
        {
            Email = email,
            FullName = req.FullName.Trim(),
            Phone = Clean(req.Phone),
            Role = req.Role,
            PasswordHash = DefaultPasswordHash(),
            MustChangePassword = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();   // pour obtenir user.Id

        switch (req.Role)
        {
            case Roles.Etudiant:
                db.StudentProfiles.Add(new StudentProfile
                {
                    UserId = user.Id,
                    Matricule = NewMatricule("ETU", user.Id),
                    BirthDate = req.BirthDate,
                    ClassName = Clean(req.ClassName)
                });
                foreach (var pid in parentIds)
                    db.ParentStudents.Add(new ParentStudent { ParentUserId = pid, StudentUserId = user.Id });
                break;

            case Roles.Enseignant:
                db.TeacherProfiles.Add(new TeacherProfile
                {
                    UserId = user.Id,
                    Matricule = NewMatricule("ENS", user.Id),
                    Speciality = Clean(req.Speciality)
                });
                break;

            case Roles.Responsable:
                db.ManagerProfiles.Add(new AcademicManagerProfile
                {
                    UserId = user.Id,
                    Department = Clean(req.Department)
                });
                break;

            case Roles.Parent:
                db.ParentProfiles.Add(new ParentProfile
                {
                    UserId = user.Id,
                    Profession = Clean(req.Profession),
                    Address = Clean(req.Address)
                });
                break;
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        // Email après validation : s'il échoue, le compte existe quand même
        var sent = await mailer.SendWelcomeAsync(user.Email, user.FullName, DefaultPassword);
        Response.Headers["X-Email-Sent"] = sent ? "true" : "false";

        var created = await db.Users.WithProfiles().FirstAsync(u => u.Id == user.Id);
        return CreatedAtAction(nameof(GetOne), new { id = user.Id }, UserResponse.From(created));
    }


    [HttpPut("update/{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest req)
    {
        var user = await db.Users.WithProfiles().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        if (!CanManage(user.Role)) return Forbid();

        var isActive = req.IsActive ?? user.IsActive;
        if (id == CurrentUserId && !isActive)
            return BadRequest(new { message = "Tu ne peux pas désactiver ton propre compte." });

        // Validation des parents AVANT toute modification
        List<int>? wantedParents = null;
        if (user.Role == Roles.Etudiant && req.ParentIds is not null)
        {
            wantedParents = req.ParentIds.Distinct().ToList();
            if (!await AllAreParents(wantedParents))
                return BadRequest(new { message = "Un des parents sélectionnés est invalide." });
        }

        user.FullName = req.FullName.Trim();
        user.Phone = Clean(req.Phone);
        user.IsActive = isActive;

        switch (user.Role)
        {
            case Roles.Etudiant:
                var student = user.StudentProfile!;
                student.BirthDate = req.BirthDate;
                student.ClassName = Clean(req.ClassName);

                if (wantedParents is not null)
                {
                    db.ParentStudents.RemoveRange(
                        student.Parents.Where(p => !wantedParents.Contains(p.ParentUserId)));
                    foreach (var pid in wantedParents.Where(w => student.Parents.All(p => p.ParentUserId != w)))
                        db.ParentStudents.Add(new ParentStudent { ParentUserId = pid, StudentUserId = id });
                }
                break;

            case Roles.Enseignant:
                user.TeacherProfile!.Speciality = Clean(req.Speciality);
                break;

            case Roles.Responsable:
                user.ManagerProfile!.Department = Clean(req.Department);
                break;

            case Roles.Parent:
                user.ParentProfile!.Profession = Clean(req.Profession);
                user.ParentProfile.Address = Clean(req.Address);
                break;
        }

        await db.SaveChangesAsync();

        var updated = await db.Users.WithProfiles().FirstAsync(u => u.Id == id);
        return Ok(UserResponse.From(updated));
    }


    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (id == CurrentUserId)
            return BadRequest(new { message = "Tu ne peux pas supprimer ton propre compte." });

        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (!CanManage(user.Role)) return Forbid();

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return NoContent();
    }


    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (!CanManage(user.Role)) return Forbid();

        user.PasswordHash = DefaultPasswordHash();
        user.MustChangePassword = true;
        await db.SaveChangesAsync();

        //  le résultat de l'envoi est renvoyé (avant : NoContent, l'échec était invisible)
        var sent = await mailer.SendResetAsync(user.Email, user.FullName, DefaultPassword);
        return Ok(new { emailSent = sent });
    }
}