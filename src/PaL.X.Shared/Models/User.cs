using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PaL.X.Shared.Enums;

namespace PaL.X.Shared.Models;

[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsAdmin { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastLogin { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Offline;
}
