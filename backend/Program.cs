using System.Security.Claims;
using System.Text;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// --- MySQL ---
var connStr = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection est manquante (docker-compose, .env ou user-secrets).");
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36))));

// --- Services ---
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- Swagger avec bouton Authorize ---
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Colle uniquement le token (sans le mot Bearer)."
    });

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
    });
});

// --- JWT ---
var jwtKey = config["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key est manquante (docker-compose, .env ou user-secrets).");
var jwtIssuer = config["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer est manquante.");
var jwtAudience = config["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience est manquante.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // À chaque requête : le compte existe-t-il toujours et est-il actif ?
        opt.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var db = ctx.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var idText = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                var active = int.TryParse(idText, out var id)
                    && await db.Users.AsNoTracking().AnyAsync(u => u.Id == id && u.IsActive);

                if (!active) ctx.Fail("Compte désactivé ou supprimé.");
            }
        };
    });
builder.Services.AddAuthorization();

//  CORS : les adresses du frontend viennent de la configuration 
var frontendUrls = config.GetSection("App:FrontendUrls").Get<string[]>()
    ?? new[] { config["App:FrontendUrl"] ?? "http://localhost:3000" };

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins(frontendUrls.Select(u => u.TrimEnd('/')).ToArray())
     .AllowAnyHeader()
     .AllowAnyMethod()));

var app = builder.Build();

//  Migrations + premier admin 
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var adminEmail = config["SeedAdmin:Email"];
    var adminPassword = config["SeedAdmin:Password"];

    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
    {
        app.Logger.LogWarning("SeedAdmin non configuré : aucun admin créé.");
    }
    else if (!db.Users.Any(u => u.Email == adminEmail))
    {
        db.Users.Add(new User
        {
            Email = adminEmail,
            FullName = config["SeedAdmin:FullName"] ?? "Administrateur",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role = Roles.Admin,
            MustChangePassword = false
        });
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();   // en production uniquement
}

app.UseCors();
app.UseAuthentication();   // "qui es-tu ?"
app.UseAuthorization();    // "as-tu le droit ?"
app.MapControllers();
app.Run();