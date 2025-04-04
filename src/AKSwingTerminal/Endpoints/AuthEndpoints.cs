using AKSwingTerminal.Models.DTOs;
using AKSwingTerminal.Services;
using Microsoft.AspNetCore.Mvc;

namespace AKSwingTerminal.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/auth");

            // Get authentication status
            group.MapGet("/status", async (
                [FromServices] IUserService userService) =>
            {
                try
                {
                    var user = await userService.GetDefaultUserAsync();
                    if (user == null)
                    {
                        return Results.NotFound("No user found");
                    }

                    var hasActiveCredentials = await userService.HasActiveApiCredentialsAsync(user.Id);
                    var hasValidToken = await userService.HasValidTokenAsync(user.Id);

                    // Get token expiry if available
                    DateTime? tokenExpiresAt = null;
                    if (hasValidToken)
                    {
                        var profile = await userService.GetUserProfileAsync(user.Id);
                        tokenExpiresAt = profile.TokenExpiresAt;
                    }

                    return Results.Ok(new
                    {
                        UserId = user.Id,
                        HasActiveApiCredentials = hasActiveCredentials,
                        HasValidToken = hasValidToken,
                        TokenExpiresAt = tokenExpiresAt
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });

            // Generate authentication URL
            group.MapPost("/generate-auth-url", async (
                [FromBody] AuthRequest request,
                [FromServices] IFyersAuthService fyersAuthService) =>
            {
                try
                {
                    var response = await fyersAuthService.GenerateAuthUrlAsync(
                        request.AppId,
                        request.RedirectUri);

                    return Results.Ok(response);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });

            // Generate token from auth code
            group.MapPost("/token", async (
                [FromBody] TokenRequest request,
                [FromServices] IFyersAuthService fyersAuthService,
                [FromServices] ITokenService tokenService,
                [FromServices] IUserService userService) =>
            {
                try
                {
                    var tokenResponse = await fyersAuthService.GenerateTokenAsync(request);
                    if (!tokenResponse.Success)
                    {
                        return Results.BadRequest(new { Error = tokenResponse.Error });
                    }

                    // Get the default user for single-user application
                    var user = await userService.GetDefaultUserAsync();
                    if (user == null)
                    {
                        return Results.NotFound("No user found");
                    }

                    // Save the token
                    await tokenService.SaveTokenAsync(tokenResponse, user.Id);

                    // Generate JWT for local authentication
                    var jwtToken = await tokenService.GenerateJwtTokenAsync(user.Id);

                    return Results.Ok(new
                    {
                        Success = true,
                        AccessToken = tokenResponse.AccessToken,
                        ExpiresAt = tokenResponse.ExpiresAt,
                        JwtToken = jwtToken
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });

            // Refresh token
            group.MapPost("/refresh", async (
                [FromServices] ITokenService tokenService,
                [FromServices] IUserService userService) =>
            {
                try
                {
                    var success = await tokenService.RefreshTokenIfNeededAsync();
                    if (!success)
                    {
                        return Results.BadRequest(new { Error = "Failed to refresh token" });
                    }

                    // Get the default user for single-user application
                    var user = await userService.GetDefaultUserAsync();
                    if (user == null)
                    {
                        return Results.NotFound("No user found");
                    }

                    // Generate new JWT
                    var jwtToken = await tokenService.GenerateJwtTokenAsync(user.Id);

                    return Results.Ok(new
                    {
                        Success = true,
                        JwtToken = jwtToken
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });

            // Disconnect (revoke token)
            group.MapPost("/disconnect", async (
                [FromServices] ITokenService tokenService,
                [FromServices] IUserService userService) =>
            {
                try
                {
                    var user = await userService.GetDefaultUserAsync();
                    if (user == null)
                    {
                        return Results.NotFound("No user found");
                    }

                    var success = await tokenService.DeleteAllTokensAsync(user.Id);
                    return Results.Ok(new { Success = success });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });
        }
    }
}
