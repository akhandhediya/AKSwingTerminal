using AKSwingTerminal.Services;
using Microsoft.AspNetCore.Mvc;

namespace AKSwingTerminal.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/user");

            // Get user profile
            group.MapGet("/profile", async (
                [FromServices] IUserService userService) =>
            {
                try
                {
                    var user = await userService.GetDefaultUserAsync();
                    if (user == null)
                    {
                        return Results.NotFound("No user found");
                    }

                    var profile = await userService.GetUserProfileAsync(user.Id);
                    return Results.Ok(profile);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });
        }
    }
}
