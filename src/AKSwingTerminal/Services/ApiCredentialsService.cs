using AKSwingTerminal.Data;
using AKSwingTerminal.Models;
using AKSwingTerminal.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AKSwingTerminal.Services
{
    public interface IApiCredentialsService
    {
        Task<List<ApiCredentialsDto>> GetAllCredentialsAsync(int userId);
        Task<ApiCredentialsDto?> GetActiveCredentialsAsync(int userId);
        Task<ApiCredentials> CreateCredentialsAsync(int userId, ApiCredentialsUpdateDto dto);
        Task<bool> UpdateCredentialsAsync(int userId, int credentialsId, ApiCredentialsUpdateDto dto);
        Task<bool> DeleteCredentialsAsync(int userId, int credentialsId);
        Task<bool> SetCredentialsActiveStatusAsync(int userId, int credentialsId, bool isActive);
    }

    public class ApiCredentialsService : IApiCredentialsService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ApiCredentialsService> _logger;

        public ApiCredentialsService(
            AppDbContext dbContext,
            ILogger<ApiCredentialsService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<ApiCredentialsDto>> GetAllCredentialsAsync(int userId)
        {
            return await _dbContext.ApiCredentials
                .Where(c => c.UserId == userId)
                .Select(c => new ApiCredentialsDto
                {
                    Id = c.Id,
                    AppId = c.AppId,
                    AppSecret = "••••••••", // Mask the secret for security
                    RedirectUrl = c.RedirectUrl,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ApiCredentialsDto?> GetActiveCredentialsAsync(int userId)
        {
            var credentials = await _dbContext.ApiCredentials
                .Where(c => c.UserId == userId && c.IsActive)
                .FirstOrDefaultAsync();

            if (credentials == null)
            {
                return null;
            }

            return new ApiCredentialsDto
            {
                Id = credentials.Id,
                AppId = credentials.AppId,
                AppSecret = "••••••••", // Mask the secret for security
                RedirectUrl = credentials.RedirectUrl,
                IsActive = credentials.IsActive,
                CreatedAt = credentials.CreatedAt
            };
        }

        public async Task<ApiCredentials> CreateCredentialsAsync(int userId, ApiCredentialsUpdateDto dto)
        {
            // For single-user application, we'll manage only one set of credentials
            // Deactivate all existing credentials
            var existingCredentials = await _dbContext.ApiCredentials
                .Where(c => c.UserId == userId)
                .ToListAsync();

            foreach (var cred in existingCredentials)
            {
                cred.IsActive = false;
                cred.UpdatedAt = DateTime.UtcNow;
            }

            // Create new credentials
            var newCredentials = new ApiCredentials
            {
                UserId = userId,
                AppId = dto.AppId,
                AppSecret = dto.AppSecret,
                RedirectUrl = dto.RedirectUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.ApiCredentials.Add(newCredentials);
            await _dbContext.SaveChangesAsync();

            return newCredentials;
        }

        public async Task<bool> UpdateCredentialsAsync(int userId, int credentialsId, ApiCredentialsUpdateDto dto)
        {
            var credentials = await _dbContext.ApiCredentials
                .FirstOrDefaultAsync(c => c.Id == credentialsId && c.UserId == userId);

            if (credentials == null)
            {
                return false;
            }

            credentials.AppId = dto.AppId;
            credentials.AppSecret = dto.AppSecret;
            credentials.RedirectUrl = dto.RedirectUrl;
            credentials.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCredentialsAsync(int userId, int credentialsId)
        {
            var credentials = await _dbContext.ApiCredentials
                .FirstOrDefaultAsync(c => c.Id == credentialsId && c.UserId == userId);

            if (credentials == null)
            {
                return false;
            }

            _dbContext.ApiCredentials.Remove(credentials);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetCredentialsActiveStatusAsync(int userId, int credentialsId, bool isActive)
        {
            // If setting to active, deactivate all other credentials first
            if (isActive)
            {
                var existingCredentials = await _dbContext.ApiCredentials
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                foreach (var cred in existingCredentials)
                {
                    cred.IsActive = false;
                    cred.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Update the specified credentials
            var credentials = await _dbContext.ApiCredentials
                .FirstOrDefaultAsync(c => c.Id == credentialsId && c.UserId == userId);

            if (credentials == null)
            {
                return false;
            }

            credentials.IsActive = isActive;
            credentials.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
