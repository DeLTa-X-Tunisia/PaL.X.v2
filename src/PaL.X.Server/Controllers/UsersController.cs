using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using PaL.X.Data;
using PaL.X.Shared.Models;
using PaL.X.Shared.Enums;
using PaL.X.Server.Hubs;

namespace PaL.X.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly PalContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IHubContext<ChatHub> _hubContext;

    public UsersController(PalContext context, IWebHostEnvironment env, IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _env = env;
        _hubContext = hubContext;
    }

    [HttpPut("{username}/status")]
    public async Task<IActionResult> UpdateStatus(string username, [FromBody] UserStatus status)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound();

        user.Status = status;
        
        // Update active sessions for this user
        var activeSessions = await _context.Sessions
            .Where(s => s.UserId == user.Id && s.IsActive)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.DisplayedStatus = status;
            session.LastActivity = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Broadcast to all clients
        await _hubContext.Clients.All.SendAsync("UserStatusChanged", user.Id, status);

        return Ok(new { status = user.Status });
    }

    [HttpGet("{username}/status")]
    public async Task<IActionResult> GetStatus(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound();

        return Ok(new { status = user.Status });
    }

    [HttpPost("{username}/avatar")]
    public async Task<IActionResult> UploadAvatar(string username, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return NotFound("User not found.");

        var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == user.Id);
        if (userProfile == null)
        {
            // Create profile if not exists
            userProfile = new UserProfile 
            { 
                UserId = user.Id, 
                User = user,
                Email = "",
                FirstName = "",
                LastName = "",
                BirthDate = DateTime.MinValue
            };
            _context.UserProfiles.Add(userProfile);
        }

        // Ensure directory exists
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "avatars");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // Generate unique filename
        var fileName = $"{username}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Update DB
        // We store the relative URL
        // Assuming the server is hosted at root, the URL is /avatars/filename
        // If the client needs the full URL, they can prepend the server address.
        var fileUrl = $"/avatars/{fileName}";
        userProfile.ProfilePictureUrl = fileUrl;
        
        await _context.SaveChangesAsync();

        return Ok(new { url = fileUrl });
    }
    
    [HttpGet("{username}/avatar")]
    public async Task<IActionResult> GetAvatar(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound();
        
        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile == null || string.IsNullOrEmpty(profile.ProfilePictureUrl)) return NotFound();
        
        return Ok(new { url = profile.ProfilePictureUrl });
    }

    [HttpGet("{username}/profile")]
    public async Task<IActionResult> GetProfile(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound();

        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        
        // If profile is null, return 204 No Content or a default empty profile?
        // Returning null with Ok(null) results in "null" string which is valid JSON but might be confusing.
        // Returning NotFound() might be wrong if user exists.
        // Let's return an empty object or null, but ensure client handles it.
        // Actually, returning 204 No Content is standard for "resource exists but is empty/null".
        if (profile == null) return NoContent();

        return Ok(profile);
    }

    [HttpPut("{username}/profile")]
    public async Task<IActionResult> UpdateProfile(string username, [FromBody] UserProfile profileData)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound();

        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile == null)
        {
            profile = new UserProfile { UserId = user.Id, User = user };
            _context.UserProfiles.Add(profile);
        }

        // Update fields
        profile.FirstName = profileData.FirstName;
        profile.LastName = profileData.LastName;
        profile.Email = profileData.Email;
        profile.Gender = profileData.Gender;
        profile.BirthDate = profileData.BirthDate;
        profile.Country = profileData.Country;
        profile.PhoneNumber = profileData.PhoneNumber;
        
        // Visibilities
        profile.EmailVisibility = profileData.EmailVisibility;
        profile.NameVisibility = profileData.NameVisibility;
        profile.GenderVisibility = profileData.GenderVisibility;
        profile.BirthDateVisibility = profileData.BirthDateVisibility;
        profile.CountryVisibility = profileData.CountryVisibility;
        profile.PhoneNumberVisibility = profileData.PhoneNumberVisibility;

        await _context.SaveChangesAsync();
        return Ok(profile);
    }

    [HttpGet("{username}/friends")]
    public async Task<IActionResult> GetFriends(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound();

        // For now, return all other users as "friends" for testing
        var friends = await _context.Users
            .Where(u => u.Id != user.Id)
            .Select(u => new { id = u.Id, username = u.Username, status = u.Status })
            .ToListAsync();

        return Ok(friends);
    }
}
