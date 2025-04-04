using AKSwingTerminal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AKSwingTerminal.Controllers
{
    public class AuthController : Controller
    {
        private readonly IFyersAuthService _fyersAuthService;
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IFyersAuthService fyersAuthService,
            ITokenService tokenService,
            IUserService userService,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _fyersAuthService = fyersAuthService;
            _tokenService = tokenService;
            _userService = userService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Setup()
        {
            try
            {
                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                var profile = await _userService.GetUserProfileAsync(user.Id);
                
                // Get active API credentials if available
                var hasCredentials = profile.HasActiveApiCredentials;
                var isConnected = profile.HasValidToken;
                
                // Set up view data
                ViewBag.HasCredentials = hasCredentials;
                ViewBag.IsConnected = isConnected;
                ViewBag.TokenExpiry = profile.TokenExpiresAt?.ToString("g") ?? "N/A";
                
                // Get redirect URL from configuration
                ViewBag.RedirectUrl = _configuration["FyersApi:RedirectUrl"] ?? "https://your-app-url/api/auth/fyers-callback";
                
                // If we have active credentials, get the AppId (but mask AppSecret)
                if (hasCredentials)
                {
                    var credentials = await HttpContext.RequestServices
                        .GetRequiredService<Data.AppDbContext>()
                        .ApiCredentials
                        .Where(c => c.UserId == user.Id && c.IsActive)
                        .FirstOrDefaultAsync();
                    
                    if (credentials != null)
                    {
                        ViewBag.AppId = credentials.AppId;
                        ViewBag.AppSecret = "••••••••"; // Mask the secret
                        ViewBag.RedirectUrl = credentials.RedirectUrl;
                    }
                }
                
                // If connected, get the access token (masked)
                if (isConnected)
                {
                    var token = await _tokenService.GetLatestTokenAsync();
                    if (token != null)
                    {
                        // Show only first few characters of the token
                        var maskedToken = token.AccessToken.Substring(0, 10) + "••••••••";
                        ViewBag.AccessToken = maskedToken;
                    }
                }
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Setup page");
                return View("Error", ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Connect()
        {
            try
            {
                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                // Check if we have active API credentials
                var hasCredentials = await _userService.HasActiveApiCredentialsAsync(user.Id);
                if (!hasCredentials)
                {
                    TempData["ErrorMessage"] = "No active API credentials found. Please set up your API credentials first.";
                    return RedirectToAction("Setup");
                }

                // Get the active credentials
                var credentials = await HttpContext.RequestServices
                    .GetRequiredService<Data.AppDbContext>()
                    .ApiCredentials
                    .Where(c => c.UserId == user.Id && c.IsActive)
                    .FirstOrDefaultAsync();

                if (credentials == null)
                {
                    TempData["ErrorMessage"] = "No active API credentials found. Please set up your API credentials first.";
                    return RedirectToAction("Setup");
                }

                // Generate the auth URL
                var authResponse = await _fyersAuthService.GenerateAuthUrlAsync(
                    credentials.AppId,
                    credentials.RedirectUrl);

                // Redirect to Fyers auth page
                return Redirect(authResponse.AuthUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Connect action");
                TempData["ErrorMessage"] = $"Error connecting to Fyers: {ex.Message}";
                return RedirectToAction("Setup");
            }
        }

        [HttpGet]
        [Route("api/auth/fyers-callback")]
        public async Task<IActionResult> FyersCallback([FromQuery] string code, [FromQuery] string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    TempData["ErrorMessage"] = "No authorization code received from Fyers";
                    return RedirectToAction("Setup");
                }

                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                // Get the active credentials
                var credentials = await HttpContext.RequestServices
                    .GetRequiredService<Data.AppDbContext>()
                    .ApiCredentials
                    .Where(c => c.UserId == user.Id && c.IsActive)
                    .FirstOrDefaultAsync();

                if (credentials == null)
                {
                    TempData["ErrorMessage"] = "No active API credentials found. Please set up your API credentials first.";
                    return RedirectToAction("Setup");
                }

                // Exchange the auth code for a token
                var tokenRequest = new Models.DTOs.TokenRequest
                {
                    AppId = credentials.AppId,
                    AppSecret = credentials.AppSecret,
                    AuthCode = code
                };

                var tokenResponse = await _fyersAuthService.GenerateTokenAsync(tokenRequest);
                if (!tokenResponse.Success)
                {
                    TempData["ErrorMessage"] = $"Failed to generate token: {tokenResponse.Error}";
                    return RedirectToAction("Setup");
                }

                // Save the token
                await _tokenService.SaveTokenAsync(tokenResponse, user.Id);

                TempData["SuccessMessage"] = "Successfully connected to Fyers API!";
                return RedirectToAction("Setup");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FyersCallback");
                TempData["ErrorMessage"] = $"Error processing Fyers callback: {ex.Message}";
                return RedirectToAction("Setup");
            }
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Disconnect()
        {
            try
            {
                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                await _tokenService.DeleteAllTokensAsync(user.Id);

                TempData["SuccessMessage"] = "Successfully disconnected from Fyers API";
                return RedirectToAction("Setup");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Disconnect action");
                TempData["ErrorMessage"] = $"Error disconnecting from Fyers: {ex.Message}";
                return RedirectToAction("Setup");
            }
        }
    }
}
