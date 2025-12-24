using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaL.X.Data;
using PaL.X.Shared.Models;

namespace PaL.X.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly PalContext _context;
    private readonly IWebHostEnvironment _env;

    public UsersController(PalContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
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
        if (profile == null || string.IsNullOrEmpty(profile.ProfilePictureUrl))
            return NotFound();

        return Ok(new { url = profile.ProfilePictureUrl });
    }
}
