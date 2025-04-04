using AKSwingTerminal.Services;
using Microsoft.AspNetCore.Mvc;
using AKSwingTerminal.Models.DTOs;

namespace AKSwingTerminal.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IUserService userService,
            ILogger<HomeController> logger)
        {
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
                    // For a single-user application, we should have a default user
                    // If not, redirect to an error page or setup page
                    return RedirectToAction("Setup", "Auth");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Home Index");
                return View("Error", ex.Message);
            }
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Error()
        {
            return View();
        }
    }
}
