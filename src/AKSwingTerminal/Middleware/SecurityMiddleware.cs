using AKSwingTerminal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AKSwingTerminal.Middleware
{
    public class TokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenRefreshMiddleware> _logger;

        public TokenRefreshMiddleware(RequestDelegate next, ILogger<TokenRefreshMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITokenService tokenService, IUserService userService)
        {
            try
            {
                // Skip token refresh for authentication endpoints
                if (context.Request.Path.StartsWithSegments("/api/auth") ||
                    context.Request.Path.StartsWithSegments("/Auth"))
                {
                    await _next(context);
                    return;
                }

                // Get the default user
                var user = await userService.GetDefaultUserAsync();
                if (user == null)
                {
                    await _next(context);
                    return;
                }

                // Check if token is about to expire and refresh if needed
                var isTokenExpired = await tokenService.IsTokenExpiredAsync();
                if (isTokenExpired)
                {
                    _logger.LogInformation("Token is expired or about to expire. Attempting to refresh...");
                    var refreshed = await tokenService.RefreshTokenIfNeededAsync();
                    
                    if (refreshed)
                    {
                        _logger.LogInformation("Token refreshed successfully");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to refresh token");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in token refresh middleware");
            }

            // Continue with the request
            await _next(context);
        }
    }

    public static class TokenRefreshMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenRefresh(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenRefreshMiddleware>();
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequiresFyersTokenAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
            var user = userService.GetDefaultUserAsync().GetAwaiter().GetResult();
            
            if (user == null)
            {
                context.Result = new RedirectToActionResult("Setup", "Auth", null);
                return;
            }

            var hasValidToken = userService.HasValidTokenAsync(user.Id).GetAwaiter().GetResult();
            if (!hasValidToken)
            {
                context.Result = new RedirectToActionResult("Setup", "Auth", null);
            }
        }
    }

    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Add("Content-Security-Policy", 
                "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;");

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
