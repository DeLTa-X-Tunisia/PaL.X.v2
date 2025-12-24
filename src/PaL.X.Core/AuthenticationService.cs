using Microsoft.EntityFrameworkCore;
using PaL.X.Data;
using PaL.X.Shared.Models;
using BCrypt.Net;

namespace PaL.X.Core;

public class AuthenticationService
{
    private readonly PalContext _context;

    public AuthenticationService(PalContext context)
    {
        _context = context;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null) return null;

        // Check if password is hashed (starts with $2)
        bool isValid = false;
        if (user.PasswordHash.StartsWith("$2"))
        {
            isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        else
        {
            // Legacy plain text check (and upgrade if needed, but for now just check)
            isValid = user.PasswordHash == password;
            if (isValid)
            {
                // Upgrade to hash
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            }
        }

        if (isValid)
        {
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return user;
        }

        return null;
    }

    public async Task<UserProfile?> GetProfileAsync(int userId)
    {
        return await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task SaveProfileAsync(UserProfile profile)
    {
        var existing = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == profile.UserId);
        if (existing == null)
        {
            _context.UserProfiles.Add(profile);
        }
        else
        {
            // Update fields
            existing.Email = profile.Email;
            existing.FirstName = profile.FirstName;
            existing.LastName = profile.LastName;
            existing.Gender = profile.Gender;
            existing.BirthDate = profile.BirthDate;
            existing.Country = profile.Country;
            existing.EmailVisibility = profile.EmailVisibility;
            existing.NameVisibility = profile.NameVisibility;
            existing.GenderVisibility = profile.GenderVisibility;
            existing.BirthDateVisibility = profile.BirthDateVisibility;
            existing.CountryVisibility = profile.CountryVisibility;
            existing.PhoneNumber = profile.PhoneNumber;
            existing.PhoneNumberVisibility = profile.PhoneNumberVisibility;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<User> RegisterAsync(string username, string password)
    {
        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            throw new Exception("Nom d'utilisateur déjà pris.");
        }

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<List<User>> GetFriendsAsync(int userId)
    {
        // For now, return all other users
        return await _context.Users
            .Where(u => u.Id != userId)
            .ToListAsync();
    }
}
