using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

public record ChildResponse(
    int Id, string FullName, string Email, string? Matricule,
    string? ClassName, DateOnly? BirthDate, bool IsActive);

[ApiController]
[Route("api/parent")]
[Authorize(Roles = Roles.Parent)]
public class ParentController(ApplicationDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("children")]
    public async Task<ActionResult<IEnumerable<ChildResponse>>> MyChildren()
    {
        var children = await db.ParentStudents
            .AsNoTracking()
            .Where(ps => ps.ParentUserId == CurrentUserId)
            .Select(ps => new ChildResponse(
                ps.Student.User.Id,
                ps.Student.User.FullName,
                ps.Student.User.Email,
                ps.Student.Matricule,
                ps.Student.ClassName,
                ps.Student.BirthDate,
                ps.Student.User.IsActive))
            .OrderBy(c => c.FullName)
            .ToListAsync();

        return Ok(children);
    }

    [HttpGet("children/{id:int}")]
    public async Task<ActionResult<ChildResponse>> MyChild(int id)
    {
        var child = await db.ParentStudents
            .AsNoTracking()
            .Where(ps => ps.ParentUserId == CurrentUserId && ps.StudentUserId == id) //meme si parent devinne l'id on vas l'affiche page 404 car il ne fait pas partir de la liste 
            .Select(ps => new ChildResponse(
                ps.Student.User.Id,
                ps.Student.User.FullName,
                ps.Student.User.Email,
                ps.Student.Matricule,
                ps.Student.ClassName,
                ps.Student.BirthDate,
                ps.Student.User.IsActive))
            .FirstOrDefaultAsync();

        return child is null ? NotFound() : Ok(child);
    }
}