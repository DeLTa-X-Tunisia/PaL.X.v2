using System;
using System.Security.Cryptography;
using System.Text;

namespace PaL.X.Data
{
    public static class PasswordHasher
    {
        public static (string Hash, string Salt) HashPassword(string password)
        {
            // Génération d'un salt aléatoire
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);

            // Hashage avec SHA-256
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                string hash = Convert.ToBase64String(hashBytes);
                
                return (hash, salt);
            }
        }

        public static bool VerifyPassword(string password, string hash, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                string computedHash = Convert.ToBase64String(hashBytes);
                
                return computedHash == hash;
            }
        }
    }
}