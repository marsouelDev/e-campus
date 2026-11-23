namespace Frontend.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Responsable = "Responsable";
    public const string Enseignant = "Enseignant";
    public const string Etudiant = "Etudiant";
    public const string Parent = "Parent";

    public static readonly string[] All = [Admin, Responsable, Enseignant, Etudiant, Parent];

    public static string Label(string role) => role switch
    {
        Admin => "Administrateur",
        Responsable => "Responsable pédagogique",
        Enseignant => "Enseignant",
        Etudiant => "Étudiant",
        Parent => "Parent",
        _ => role,
    };
}