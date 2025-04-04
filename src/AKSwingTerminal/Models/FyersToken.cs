using System.ComponentModel.DataAnnotations;

namespace AKSwingTerminal.Models
{
    public class FyersToken
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string AccessToken { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? RefreshToken { get; set; }
        
        [Required]
        public DateTime ExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}
