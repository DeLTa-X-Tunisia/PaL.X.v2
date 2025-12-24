using System;
using System.ComponentModel.DataAnnotations;

namespace PaL.X.Api.Models
{
    public class BlockUserRequest
    {
    [MaxLength(2000)]
    public string? Reason { get; set; }

        public bool IsPermanent { get; set; }

        [Range(1, 3650, ErrorMessage = "La durée doit être positive.")]
        public int? DurationDays { get; set; }

        public DateTime? BlockedUntil { get; set; }
    }
}
