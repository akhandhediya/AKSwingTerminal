using System.ComponentModel.DataAnnotations;

namespace AKSwingTerminal.Models.DTOs
{
    public class AuthRequest
    {
        [Required]
        public string AppId { get; set; } = string.Empty;
        
        [Required]
        public string RedirectUri { get; set; } = string.Empty;
        
        [Required]
        public string AppType { get; set; } = "100"; // Default to web application
    }
    
    public class AuthResponse
    {
        public string AuthUrl { get; set; } = string.Empty;
    }
    
    public class TokenRequest
    {
        [Required]
        public string AppId { get; set; } = string.Empty;
        
        [Required]
        public string AppSecret { get; set; } = string.Empty;
        
        [Required]
        public string AuthCode { get; set; } = string.Empty;
    }
    
    public class TokenResponse
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? Error { get; set; }
    }
    
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
        
        [Required]
        public string AppId { get; set; } = string.Empty;
        
        [Required]
        public string AppSecret { get; set; } = string.Empty;
    }
}
