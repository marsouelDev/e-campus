namespace Backend.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Responsable = "ResponsableAcademique";
    public const string Enseignant = "Enseignant";
    public const string Parent = "Parent";
    public const string Etudiant = "Etudiant";

    public static readonly string[] All = [Admin, Responsable, Enseignant, Parent, Etudiant];

    // Rôles que seul un Admin peut créer, modifier ou supprimer
    public static readonly string[] AdminOnly = [Admin, Responsable];
}