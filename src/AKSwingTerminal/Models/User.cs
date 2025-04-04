using System.ComponentModel.DataAnnotations;

namespace AKSwingTerminal.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<ApiCredentials> ApiCredentials { get; set; } = new List<ApiCredentials>();
        
        public virtual ICollection<FyersToken> FyersTokens { get; set; } = new List<FyersToken>();
    }
}
