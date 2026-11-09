
using Backend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// 1. CONFIGURATION DE MYSQL
// ========================================

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "La chaîne de connexion MySQL est introuvable."
    );

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

// ========================================
// 2. CONFIGURATION DES SERVICES
// ========================================

builder.Services.AddControllers();

// Configuration Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========================================
// 3. CONSTRUCTION DE L'APPLICATION
// ========================================

var app = builder.Build();

// ========================================
// 4. CONFIGURATION DU PIPELINE HTTP
// ========================================

if (app.Environment.IsDevelopment())
{
    // Activer Swagger
    app.UseSwagger();

    // Activer l'interface Swagger UI
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Activer les contrôleurs
app.MapControllers();

// ========================================
// 5. DÉMARRAGE DU SERVEUR
// ========================================

app.Run();
