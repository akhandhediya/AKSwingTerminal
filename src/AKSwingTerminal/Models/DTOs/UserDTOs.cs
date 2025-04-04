using System.ComponentModel.DataAnnotations;

namespace AKSwingTerminal.Models.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool HasActiveApiCredentials { get; set; }
        public bool HasValidToken { get; set; }
        public DateTime? TokenExpiresAt { get; set; }
    }

    public class ApiCredentialsDto
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string AppId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string AppSecret { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        [Url]
        public string RedirectUrl { get; set; } = string.Empty;
        
        public bool IsActive { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }

    public class ApiCredentialsUpdateDto
    {
        [Required]
        [MaxLength(50)]
        public string AppId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string AppSecret { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        [Url]
        public string RedirectUrl { get; set; } = string.Empty;
    }
}
