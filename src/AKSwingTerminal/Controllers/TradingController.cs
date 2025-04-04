using AKSwingTerminal.Attributes;
using AKSwingTerminal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AKSwingTerminal.Controllers
{
    [RequiresFyersToken]
    public class TradingController : Controller
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<TradingController> _logger;

        public TradingController(
            IUserService userService,
            ITokenService tokenService,
            ILogger<TradingController> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                // Check if token is valid, if not redirect to setup
                var hasValidToken = await _userService.HasValidTokenAsync(user.Id);
                if (!hasValidToken)
                {
                    TempData["ErrorMessage"] = "Your Fyers API connection has expired. Please reconnect.";
                    return RedirectToAction("Setup", "Auth");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading trading dashboard");
                TempData["ErrorMessage"] = $"Error loading dashboard: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            try
            {
                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                // Check if token is valid, if not redirect to setup
                var hasValidToken = await _userService.HasValidTokenAsync(user.Id);
                if (!hasValidToken)
                {
                    TempData["ErrorMessage"] = "Your Fyers API connection has expired. Please reconnect.";
                    return RedirectToAction("Setup", "Auth");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders page");
                TempData["ErrorMessage"] = $"Error loading orders: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Holdings()
        {
            try
            {
                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                // Check if token is valid, if not redirect to setup
                var hasValidToken = await _userService.HasValidTokenAsync(user.Id);
                if (!hasValidToken)
                {
                    TempData["ErrorMessage"] = "Your Fyers API connection has expired. Please reconnect.";
                    return RedirectToAction("Setup", "Auth");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading holdings page");
                TempData["ErrorMessage"] = $"Error loading holdings: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Positions()
        {
            try
            {
                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                // Check if token is valid, if not redirect to setup
                var hasValidToken = await _userService.HasValidTokenAsync(user.Id);
                if (!hasValidToken)
                {
                    TempData["ErrorMessage"] = "Your Fyers API connection has expired. Please reconnect.";
                    return RedirectToAction("Setup", "Auth");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading positions page");
                TempData["ErrorMessage"] = $"Error loading positions: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }
    }
}
