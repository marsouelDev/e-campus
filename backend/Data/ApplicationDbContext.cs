using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();
    public DbSet<AcademicManagerProfile> ManagerProfiles => Set<AcademicManagerProfile>();
    public DbSet<ParentProfile> ParentProfiles => Set<ParentProfile>();
    public DbSet<ParentStudent> ParentStudents => Set<ParentStudent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();

            // Supprimer un utilisateur selon  son profil
            e.HasOne(u => u.StudentProfile).WithOne(p => p.User)
                .HasForeignKey<StudentProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(u => u.TeacherProfile).WithOne(p => p.User)
                .HasForeignKey<TeacherProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(u => u.ManagerProfile).WithOne(p => p.User)
                .HasForeignKey<AcademicManagerProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(u => u.ParentProfile).WithOne(p => p.User)
                .HasForeignKey<ParentProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<StudentProfile>().HasIndex(s => s.Matricule).IsUnique();
        b.Entity<TeacherProfile>().HasIndex(t => t.Matricule).IsUnique();

        b.Entity<ParentStudent>(e =>
        {
            e.HasKey(x => new { x.ParentUserId, x.StudentUserId });
            e.HasOne(x => x.Parent).WithMany(p => p.Children)
                .HasForeignKey(x => x.ParentUserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Student).WithMany(s => s.Parents)
                .HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}