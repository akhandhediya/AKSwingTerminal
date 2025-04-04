using System.ComponentModel.DataAnnotations;

namespace AKSwingTerminal.Models
{
    public class ApiCredentials
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string AppId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string AppSecret { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string RedirectUrl { get; set; } = string.Empty;
        
        [Required]
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}
