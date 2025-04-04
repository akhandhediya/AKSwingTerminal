using AKSwingTerminal.Data;
using AKSwingTerminal.Models;
using AKSwingTerminal.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AKSwingTerminal.Services
{
    public interface ITokenService
    {
        Task<FyersToken?> GetLatestTokenAsync();
        Task<FyersToken> SaveTokenAsync(TokenResponse tokenResponse, int userId);
        Task<bool> DeleteTokenAsync(int tokenId);
        Task<bool> DeleteAllTokensAsync(int userId);
        Task<string> GenerateJwtTokenAsync(int userId);
        Task<bool> ValidateTokenAsync(string accessToken);
        Task<bool> IsTokenExpiredAsync();
        Task<bool> RefreshTokenIfNeededAsync();
    }

    public class TokenService : ITokenService
    {
        private readonly AppDbContext _dbContext;
        private readonly IFyersAuthService _fyersAuthService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            AppDbContext dbContext,
            IFyersAuthService fyersAuthService,
            IConfiguration configuration,
            ILogger<TokenService> logger)
        {
            _dbContext = dbContext;
            _fyersAuthService = fyersAuthService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<FyersToken?> GetLatestTokenAsync()
        {
            return await _dbContext.FyersTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<FyersToken> SaveTokenAsync(TokenResponse tokenResponse, int userId)
        {
            var token = new FyersToken
            {
                UserId = userId,
                AccessToken = tokenResponse.AccessToken!,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresAt = tokenResponse.ExpiresAt!.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.FyersTokens.Add(token);
            await _dbContext.SaveChangesAsync();

            return token;
        }

        public async Task<bool> DeleteTokenAsync(int tokenId)
        {
            var token = await _dbContext.FyersTokens.FindAsync(tokenId);
            if (token == null)
            {
                return false;
            }

            _dbContext.FyersTokens.Remove(token);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAllTokensAsync(int userId)
        {
            var tokens = await _dbContext.FyersTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (!tokens.Any())
            {
                return false;
            }

            _dbContext.FyersTokens.RemoveRange(tokens);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<string> GenerateJwtTokenAsync(int userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found", nameof(userId));
            }

            var secretKey = _configuration["JwtSettings:SecretKey"] ?? 
                throw new InvalidOperationException("JWT Secret Key is not configured");
            
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.Name)
            };

            var expiryInMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "60");
            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> ValidateTokenAsync(string accessToken)
        {
            return await _fyersAuthService.ValidateTokenAsync(accessToken);
        }

        public async Task<bool> IsTokenExpiredAsync()
        {
            var token = await GetLatestTokenAsync();
            if (token == null)
            {
                return true;
            }

            // Add a buffer of 5 minutes to ensure we refresh before actual expiry
            return token.ExpiresAt.AddMinutes(-5) <= DateTime.UtcNow;
        }

        public async Task<bool> RefreshTokenIfNeededAsync()
        {
            try
            {
                // Check if token is expired or about to expire
                if (!await IsTokenExpiredAsync())
                {
                    return true; // Token is still valid
                }

                var token = await GetLatestTokenAsync();
                if (token == null || string.IsNullOrEmpty(token.RefreshToken))
                {
                    _logger.LogWarning("No valid token or refresh token found for refresh");
                    return false;
                }

                // Get API credentials
                var credentials = await _dbContext.ApiCredentials
                    .Where(c => c.IsActive && c.UserId == token.UserId)
                    .FirstOrDefaultAsync();

                if (credentials == null)
                {
                    _logger.LogWarning("No active API credentials found for token refresh");
                    return false;
                }

                // Attempt to refresh the token
                var refreshRequest = new RefreshTokenRequest
                {
                    RefreshToken = token.RefreshToken,
                    AppId = credentials.AppId,
                    AppSecret = credentials.AppSecret
                };

                var response = await _fyersAuthService.RefreshTokenAsync(refreshRequest);
                if (!response.Success)
                {
                    _logger.LogError("Token refresh failed: {Error}", response.Error);
                    return false;
                }

                // Save the new token
                await SaveTokenAsync(response, token.UserId);
                
                // Delete the old token
                await DeleteTokenAsync(token.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return false;
            }
        }
    }
}
