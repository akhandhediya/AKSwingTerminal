using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AKSwingTerminal.Services;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace AKSwingTerminal.Attributes
{
    public class RequiresFyersTokenAttribute : TypeFilterAttribute
    {
        public RequiresFyersTokenAttribute() : base(typeof(RequiresFyersTokenFilter))
        {
        }
    }

    public class RequiresFyersTokenFilter : IAsyncActionFilter
    {
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;
        private readonly ILogger<RequiresFyersTokenFilter> _logger;

        public RequiresFyersTokenFilter(
            ITokenService tokenService,
            IUserService userService,
            ILogger<RequiresFyersTokenFilter> logger)
        {
            _tokenService = tokenService;
            _userService = userService;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                var user = await _userService.GetDefaultUserAsync();
                if (user == null)
                {
                    _logger.LogWarning("No user found when checking for Fyers token");
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    return;
                }

                var hasValidToken = await _userService.HasValidTokenAsync(user.Id);
                if (!hasValidToken)
                {
                    _logger.LogWarning("No valid Fyers token found for user {UserId}", user.Id);
                    if (context.Controller is Controller controller)
                    {
                        controller.TempData["ErrorMessage"] = "Your Fyers API connection has expired. Please reconnect.";
                    }
                    context.Result = new RedirectToActionResult("Setup", "Auth", null);
                    return;
                }

                await next();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RequiresFyersToken filter");
                if (context.Controller is Controller controller)
                {
                    controller.TempData["ErrorMessage"] = "An error occurred while checking your Fyers API connection.";
                }
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }
}
