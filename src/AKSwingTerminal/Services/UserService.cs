using AKSwingTerminal.Data;
using AKSwingTerminal.Models;
using AKSwingTerminal.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AKSwingTerminal.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetDefaultUserAsync();
        Task<UserProfileDto> GetUserProfileAsync(int userId);
        Task<bool> HasActiveApiCredentialsAsync(int userId);
        Task<bool> HasValidTokenAsync(int userId);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            AppDbContext dbContext,
            ITokenService tokenService,
            ILogger<UserService> logger)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _dbContext.Users.FindAsync(userId);
        }

        public async Task<User?> GetDefaultUserAsync()
        {
            // For single-user application, we'll always return the first user
            // This could be enhanced for multi-user scenarios
            return await _dbContext.Users.FirstOrDefaultAsync();
        }

        public async Task<UserProfileDto> GetUserProfileAsync(int userId)
        {
            var user = await _dbContext.Users
                .Include(u => u.ApiCredentials)
                .Include(u => u.FyersTokens)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new ArgumentException("User not found", nameof(userId));
            }

            var hasActiveCredentials = user.ApiCredentials.Any(c => c.IsActive);
            
            var latestToken = user.FyersTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefault();
            
            var hasValidToken = latestToken != null && 
                                latestToken.ExpiresAt > DateTime.UtcNow &&
                                await _tokenService.ValidateTokenAsync(latestToken.AccessToken);

            return new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                HasActiveApiCredentials = hasActiveCredentials,
                HasValidToken = hasValidToken,
                TokenExpiresAt = latestToken?.ExpiresAt
            };
        }

        public async Task<bool> HasActiveApiCredentialsAsync(int userId)
        {
            return await _dbContext.ApiCredentials
                .AnyAsync(c => c.UserId == userId && c.IsActive);
        }

        public async Task<bool> HasValidTokenAsync(int userId)
        {
            var latestToken = await _dbContext.FyersTokens
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestToken == null || latestToken.ExpiresAt <= DateTime.UtcNow)
            {
                return false;
            }

            return await _tokenService.ValidateTokenAsync(latestToken.AccessToken);
        }
    }
}
