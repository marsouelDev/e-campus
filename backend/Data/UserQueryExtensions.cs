using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public static class UserQueryExtensions
{
    // Charge l'utilisateur avec son profil (nécessaire pour UserResponse.From)
    public static IQueryable<User> WithProfiles(this IQueryable<User> query) => query
        .Include(u => u.StudentProfile).ThenInclude(s => s!.Parents)
        .Include(u => u.TeacherProfile)
        .Include(u => u.ManagerProfile)
        .Include(u => u.ParentProfile).ThenInclude(p => p!.Children)
        .AsSplitQuery();
}