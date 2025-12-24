using PaL.X.Shared.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PaL.X.Data
{
    public static class SeedData
    {
        public static async Task Initialize(AppDbContext context)
        {
            // Vérifier si des utilisateurs existent déjà
            if (context.Users.Any())
            {
                return; // La base contient déjà des données
            }

            // Créer un utilisateur admin
            var (adminHash, adminSalt) = PasswordHasher.HashPassword("Admin123!");
            var admin = new User
            {
                Username = "admin",
                Email = "admin@palx.local",
                PasswordHash = adminHash,
                Salt = adminSalt,
                IsAdmin = true,
                CreatedAt = DateTime.UtcNow
            };

            // Créer un utilisateur normal pour les tests
            var (userHash, userSalt) = PasswordHasher.HashPassword("User123!");
            var user = new User
            {
                Username = "user",
                Email = "user@palx.local",
                PasswordHash = userHash,
                Salt = userSalt,
                IsAdmin = false,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(admin, user);
            await context.SaveChangesAsync();
        }
    }
}