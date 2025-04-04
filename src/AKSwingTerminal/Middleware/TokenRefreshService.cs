using AKSwingTerminal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AKSwingTerminal.Middleware
{
    public class TokenRefreshBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenRefreshBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Check every 15 minutes

        public TokenRefreshBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<TokenRefreshBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Refresh Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Token Refresh Background Service is checking for token expiry.");

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                        // Get the default user for single-user application
                        var user = await userService.GetDefaultUserAsync();
                        if (user != null)
                        {
                            // Check if token is about to expire and refresh if needed
                            var isTokenExpired = await tokenService.IsTokenExpiredAsync();
                            if (isTokenExpired)
                            {
                                _logger.LogInformation("Token is expired or about to expire. Attempting to refresh...");
                                var refreshed = await tokenService.RefreshTokenIfNeededAsync();
                                
                                if (refreshed)
                                {
                                    _logger.LogInformation("Token refreshed successfully by background service.");
                                }
                                else
                                {
                                    _logger.LogWarning("Failed to refresh token in background service.");
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Token is still valid. No refresh needed.");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No user found for token refresh.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Token Refresh Background Service.");
                }

                // Wait for the next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Token Refresh Background Service is stopping.");
        }
    }

    public class TokenRefreshActionFilter : IAsyncActionFilter
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<TokenRefreshActionFilter> _logger;

        public TokenRefreshActionFilter(
            ITokenService tokenService,
            ILogger<TokenRefreshActionFilter> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                // Check if token is about to expire and refresh if needed
                var isTokenExpired = await _tokenService.IsTokenExpiredAsync();
                if (isTokenExpired)
                {
                    _logger.LogInformation("Token is expired or about to expire. Attempting to refresh in action filter...");
                    var refreshed = await _tokenService.RefreshTokenIfNeededAsync();
                    
                    if (refreshed)
                    {
                        _logger.LogInformation("Token refreshed successfully in action filter.");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to refresh token in action filter.");
                        
                        // If this is a trading controller action, redirect to auth setup
                        if (context.Controller is Controllers.TradingController)
                        {
                            _logger.LogWarning("Redirecting to Auth Setup due to token refresh failure.");
                            context.Result = new RedirectToActionResult("Setup", "Auth", null);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Token Refresh Action Filter.");
            }

            // Continue with the action execution
            await next();
        }
    }

    public static class TokenRefreshExtensions
    {
        public static IServiceCollection AddTokenRefreshServices(this IServiceCollection services)
        {
            // Register the background service
            services.AddHostedService<TokenRefreshBackgroundService>();
            
            // Register the action filter
            services.AddScoped<TokenRefreshActionFilter>();
            
            return services;
        }
        
        public static MvcOptions AddTokenRefreshFilter(this MvcOptions options)
        {
            // Add the action filter globally
            options.Filters.Add<TokenRefreshActionFilter>();
            
            return options;
        }
    }
}
