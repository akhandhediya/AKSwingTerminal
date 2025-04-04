using AKSwingTerminal.Models;
using AKSwingTerminal.Models.DTOs;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AKSwingTerminal.Services
{
    public interface IFyersAuthService
    {
        Task<AuthResponse> GenerateAuthUrlAsync(string appId, string redirectUri);
        Task<TokenResponse> GenerateTokenAsync(TokenRequest request);
        Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<bool> ValidateTokenAsync(string accessToken);
        Task<FyersProfileData?> GetUserProfileAsync(string accessToken);
    }

    public class FyersAuthService : IFyersAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<FyersAuthService> _logger;

        public FyersAuthService(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<FyersAuthService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<AuthResponse> GenerateAuthUrlAsync(string appId, string redirectUri)
        {
            try
            {
                // Construct the Fyers authentication URL
                var baseUrl = _configuration["FyersApi:BaseUrl"] ?? "https://api.fyers.in/api/v3";
                var appType = _configuration["FyersApi:AppType"] ?? "100"; // Default to web application

                // Create a state parameter for security (can be enhanced with CSRF protection)
                var state = Guid.NewGuid().ToString();

                // Construct the auth URL
                var authUrl = $"https://api.fyers.in/api/v3/generate-authcode?" +
                              $"client_id={appId}&" +
                              $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                              $"response_type=code&" +
                              $"state={state}";

                return new AuthResponse { AuthUrl = authUrl };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating auth URL");
                throw new ApplicationException("Failed to generate authentication URL", ex);
            }
        }

        public async Task<TokenResponse> GenerateTokenAsync(TokenRequest request)
        {
            try
            {
                var baseUrl = _configuration["FyersApi:BaseUrl"] ?? "https://api.fyers.in/api/v3";
                var tokenUrl = $"{baseUrl}/validate-authcode";

                // Prepare the request payload
                var payload = new
                {
                    grant_type = "authorization_code",
                    appIdHash = GenerateAppIdHash(request.AppId, request.AppSecret),
                    code = request.AuthCode
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                // Send the request
                var response = await _httpClient.PostAsync(tokenUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Token generation failed: {Response}", responseContent);
                    return new TokenResponse
                    {
                        Success = false,
                        Error = $"Failed to generate token: {response.StatusCode} - {responseContent}"
                    };
                }

                // Parse the response
                var fyersResponse = JsonSerializer.Deserialize<FyersApiResponse<TokenResponseData>>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (fyersResponse?.S != "ok" || fyersResponse.Data == null)
                {
                    return new TokenResponse
                    {
                        Success = false,
                        Error = fyersResponse?.Message ?? "Unknown error"
                    };
                }

                // Calculate token expiry (typically 24 hours from now)
                var expiresAt = DateTime.UtcNow.AddHours(24);

                return new TokenResponse
                {
                    Success = true,
                    AccessToken = fyersResponse.Data.AccessToken,
                    RefreshToken = fyersResponse.Data.RefreshToken,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating token");
                return new TokenResponse
                {
                    Success = false,
                    Error = $"Exception: {ex.Message}"
                };
            }
        }

        public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            try
            {
                var baseUrl = _configuration["FyersApi:BaseUrl"] ?? "https://api.fyers.in/api/v3";
                var refreshUrl = $"{baseUrl}/validate-refresh-token";

                // Prepare the request payload
                var payload = new
                {
                    grant_type = "refresh_token",
                    appIdHash = GenerateAppIdHash(request.AppId, request.AppSecret),
                    refresh_token = request.RefreshToken
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                // Send the request
                var response = await _httpClient.PostAsync(refreshUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Token refresh failed: {Response}", responseContent);
                    return new TokenResponse
                    {
                        Success = false,
                        Error = $"Failed to refresh token: {response.StatusCode} - {responseContent}"
                    };
                }

                // Parse the response
                var fyersResponse = JsonSerializer.Deserialize<FyersApiResponse<TokenResponseData>>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (fyersResponse?.S != "ok" || fyersResponse.Data == null)
                {
                    return new TokenResponse
                    {
                        Success = false,
                        Error = fyersResponse?.Message ?? "Unknown error"
                    };
                }

                // Calculate token expiry (typically 24 hours from now)
                var expiresAt = DateTime.UtcNow.AddHours(24);

                return new TokenResponse
                {
                    Success = true,
                    AccessToken = fyersResponse.Data.AccessToken,
                    RefreshToken = fyersResponse.Data.RefreshToken,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return new TokenResponse
                {
                    Success = false,
                    Error = $"Exception: {ex.Message}"
                };
            }
        }

        public async Task<bool> ValidateTokenAsync(string accessToken)
        {
            try
            {
                // A simple way to validate the token is to make a profile request
                var profile = await GetUserProfileAsync(accessToken);
                return profile != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<FyersProfileData?> GetUserProfileAsync(string accessToken)
        {
            try
            {
                var baseUrl = _configuration["FyersApi:BaseUrl"] ?? "https://api.fyers.in/api/v3";
                var profileUrl = $"{baseUrl}/profile";

                // Set up the request with the access token
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Send the request
                var response = await _httpClient.GetAsync(profileUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Profile request failed: {Response}", responseContent);
                    return null;
                }

                // Parse the response
                var fyersResponse = JsonSerializer.Deserialize<FyersApiResponse<FyersProfileData>>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (fyersResponse?.S != "ok" || fyersResponse.Data == null)
                {
                    _logger.LogError("Invalid profile response: {Response}", responseContent);
                    return null;
                }

                return fyersResponse.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return null;
            }
            finally
            {
                // Clear the authorization header
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        private string GenerateAppIdHash(string appId, string appSecret)
        {
            // This is a simplified implementation - in production, follow Fyers documentation exactly
            return $"{appId}:{appSecret}";
        }
    }

    // Helper class for token response deserialization
    internal class TokenResponseData
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}
