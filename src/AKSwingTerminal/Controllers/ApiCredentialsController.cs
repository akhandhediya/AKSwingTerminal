using AKSwingTerminal.Models.DTOs;
using AKSwingTerminal.Services;
using Microsoft.AspNetCore.Mvc;

namespace AKSwingTerminal.Controllers
{
    public class ApiCredentialsController : Controller
    {
        private readonly IApiCredentialsService _apiCredentialsService;
        private readonly IUserService _userService;
        private readonly ILogger<ApiCredentialsController> _logger;

        public ApiCredentialsController(
            IApiCredentialsService apiCredentialsService,
            IUserService userService,
            ILogger<ApiCredentialsController> logger)
        {
            _apiCredentialsService = apiCredentialsService;
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                var credentials = await _apiCredentialsService.GetAllCredentialsAsync(user.Id);
                return View(credentials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API credentials");
                TempData["ErrorMessage"] = $"Error retrieving API credentials: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(new ApiCredentialsUpdateDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(ApiCredentialsUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(dto);
                }

                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                await _apiCredentialsService.CreateCredentialsAsync(user.Id, dto);
                TempData["SuccessMessage"] = "API credentials created successfully";
                return RedirectToAction("Setup", "Auth");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API credentials");
                ModelState.AddModelError("", $"Error creating API credentials: {ex.Message}");
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                var credentials = await _apiCredentialsService.GetAllCredentialsAsync(user.Id);
                var credential = credentials.FirstOrDefault(c => c.Id == id);
                
                if (credential == null)
                {
                    return NotFound($"API credentials with ID {id} not found");
                }

                var dto = new ApiCredentialsUpdateDto
                {
                    AppId = credential.AppId,
                    AppSecret = "", // Don't send the secret to the client
                    RedirectUrl = credential.RedirectUrl
                };

                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API credentials for edit");
                TempData["ErrorMessage"] = $"Error retrieving API credentials: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ApiCredentialsUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(dto);
                }

                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                var success = await _apiCredentialsService.UpdateCredentialsAsync(user.Id, id, dto);
                if (!success)
                {
                    return NotFound($"API credentials with ID {id} not found");
                }

                TempData["SuccessMessage"] = "API credentials updated successfully";
                return RedirectToAction("Setup", "Auth");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API credentials");
                ModelState.AddModelError("", $"Error updating API credentials: {ex.Message}");
                return View(dto);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                var success = await _apiCredentialsService.DeleteCredentialsAsync(user.Id, id);
                if (!success)
                {
                    TempData["ErrorMessage"] = $"API credentials with ID {id} not found";
                    return RedirectToAction("Index");
                }

                TempData["SuccessMessage"] = "API credentials deleted successfully";
                return RedirectToAction("Setup", "Auth");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API credentials");
                TempData["ErrorMessage"] = $"Error deleting API credentials: {ex.Message}";
                return RedirectToAction("Setup", "Auth");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetActive(int id)
        {
            try
            {
                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    return NotFound("No user found");
                }

                var success = await _apiCredentialsService.SetCredentialsActiveStatusAsync(user.Id, id, true);
                if (!success)
                {
                    TempData["ErrorMessage"] = $"API credentials with ID {id} not found";
                    return RedirectToAction("Index");
                }

                TempData["SuccessMessage"] = "API credentials set as active successfully";
                return RedirectToAction("Setup", "Auth");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting API credentials as active");
                TempData["ErrorMessage"] = $"Error setting API credentials as active: {ex.Message}";
                return RedirectToAction("Setup", "Auth");
            }
        }
    }
}
