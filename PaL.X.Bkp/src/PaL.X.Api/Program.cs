using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaL.X.Api.Services;
using PaL.X.API.Services;
using PaL.X.Data;
using Npgsql;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(defaultConnection));

// Ajouter le ServiceManager comme singleton
builder.Services.AddSingleton<ServiceManager>();

// Ajouter le service de nettoyage des messages
// En Development, si la DB n'est pas configurée (ex: pas de mot de passe), éviter de spammer des erreurs.
var hasDbPassword = false;
try
{
    if (!string.IsNullOrWhiteSpace(defaultConnection))
    {
        var csb = new NpgsqlConnectionStringBuilder(defaultConnection);
        hasDbPassword = !string.IsNullOrWhiteSpace(csb.Password);
    }
}
catch
{
    hasDbPassword = false;
}

if (!builder.Environment.IsDevelopment() || hasDbPassword)
{
    builder.Services.AddHostedService<MessageCleanupService>();
}
else
{
    Console.WriteLine("[WARN] ConnectionStrings:DefaultConnection seems incomplete (no Password/Pwd). MessageCleanupService is disabled in Development.");
}

// Ajouter le service de géolocalisation
builder.Services.AddHttpClient<IGeoLocationService, GeoLocationService>();

// Ajouter SignalR
builder.Services.AddSignalR();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    if (builder.Environment.IsDevelopment())
    {
        // Dev fallback to avoid hard-stop when appsettings are incomplete.
        // For real deployments, configure Jwt:Key (or env var JWT__KEY).
        jwtKey = "DEV_ONLY__CHANGE_ME__PaL.X__JwtKey__32+chars_minimum";
        Console.WriteLine("[WARN] Jwt:Key is missing. Using a DEVELOPMENT fallback key. Configure Jwt:Key or env var JWT__KEY.");
    }
    else
    {
        throw new InvalidOperationException("JWT Key is not configured");
    }
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireClaim("isAdmin", "true"));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Ensure HTTPS redirection knows the HTTPS port (avoids: "Failed to determine the https port for redirect")
var httpsPortValue = builder.Configuration["ASPNETCORE_HTTPS_PORT"];
if (!int.TryParse(httpsPortValue, out var httpsPort))
{
    httpsPort = builder.Environment.IsDevelopment() ? 5001 : 443;
}

builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = httpsPort;
});

// Add CORS
var allowedOrigins = builder.Environment.IsDevelopment() 
    ? new[] { "http://localhost:5000", "https://localhost:5001" }
    : new[] { "https://votre-domaine.com" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins",
        builder =>
        {
            builder.WithOrigins(allowedOrigins)
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowedOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<PaL.X.Api.Hubs.PaLHub>("/hubs/pal");

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Appliquer les migrations automatiquement
        dbContext.Database.Migrate();

        // Nettoyer les sessions fantômes (sessions actives des exécutions précédentes)
        var orphanedSessions = await dbContext.Sessions
            .Where(s => s.IsActive)
            .ToListAsync();

        if (orphanedSessions.Any())
        {
            foreach (var session in orphanedSessions)
            {
                session.IsActive = false;
                session.DisconnectedAt = DateTime.UtcNow;
                session.DisplayedStatus = PaL.X.Shared.Enums.UserStatus.Offline;
                session.RealStatus = PaL.X.Shared.Enums.UserStatus.Offline;
            }
            await dbContext.SaveChangesAsync();
            Console.WriteLine($"Nettoyage: {orphanedSessions.Count} sessions fantômes désactivées.");
        }

        // Initialiser les données de test (Admin et User)
        // await SeedData.Initialize(dbContext);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Database initialization failed: {ex.GetType().Name}: {ex.Message}");
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }

        Console.WriteLine("[WARN] Continuing startup because environment is Development. Configure ConnectionStrings:DefaultConnection to enable DB features.");
    }
}

app.Run();
