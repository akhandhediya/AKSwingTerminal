using AKSwingTerminal.Data;
using AKSwingTerminal.Models;
using AKSwingTerminal.Models.DTOs;
using AKSwingTerminal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AKSwingTerminal.Endpoints
{
    public static class ApiCredentialsEndpoints
    {
        public static void MapApiCredentialsEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/credentials");

            // Get all API credentials
            group.MapGet("/", async (
                [FromServices] AppDbContext dbContext,
                [FromServices] IUserService userService) =>
            {
                try
                {
                    var user = await userService.GetDefaultUserAsync();
                    if (user == null)
                    {
                        return Results.NotFound("No user found");
                    }

                    var credentials = await dbContext.ApiCredentials
                        .Where(c => c.UserId == user.Id)
                        .Select(c => new ApiCredentialsDto
                        {
                            Id = c.Id,
                            AppId = c.AppId,
                            AppSecret = "••••••••", // Mask the secret
                            RedirectUrl = c.RedirectUrl,
                            IsActive = c.IsActive,
                            CreatedAt = c.CreatedAt
                        })
                        .ToListAsync();

                    return Results.Ok(credentials);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });

            // Get active API credentials
            group.MapGet("/active", async (
                [FromServices] AppDbContext dbContext,
                [FromServices] IUserService userService) =>
            {
                try
                {
                    var user = await userService.GetDefaultUserAsync();
                    if (user == null)
                    {
                        return Results.NotFound("No user found");
                    }

                    var credentials = await dbContext.ApiCredentials
                        .Where(c => c.UserId == user.Id && c.IsActive)
                        .Select(c => new ApiCredentialsDto
                        {
                            Id = c.Id,
                            AppId = c.AppId,
                            AppSecret = "••••••••", // Mask the secret
                            RedirectUrl = c.RedirectUrl,
                            IsActive = c.IsActive,
                            CreatedAt = c.CreatedAt
                        })
                        .FirstOrDefaultAsync();

                    if (credentials == null)
                    {
                        return Results.NotFound("No active API credentials found");
                    }

                    return Results.Ok(credentials);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });

            // Create or update API credentials
            group.MapPost("/", async (
                [FromBody] ApiCredentialsUpdateDto dto,
                [FromServices] AppDbContext dbContext,
                [FromServices] IUserService userService) =>
            {
                try
                {
                    var user = await userService.GetDefaultUserAsync();
                    if (user == null)
                    {
                        return Results.NotFound("No user found");
                    }

                    // For single-user application, we'll manage only one set of credentials
                    // Deactivate all existing credentials
                    var existingCredentials = await dbContext.ApiCredentials
                        .Where(c => c.UserId == user.Id)
                        .ToListAsync();

                    foreach (var cred in existingCredentials)
                    {
                        cred.IsActive = false;
                        cred.UpdatedAt = DateTime.UtcNow;
                    }

                    // Create new credentials
                    var newCredentials = new ApiCredentials
                    {
                        UserId = user.Id,
                        AppId = dto.AppId,
                        AppSecret = dto.AppSecret,
                        RedirectUrl = dto.RedirectUrl,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    dbContext.ApiCredentials.Add(newCredentials);
                    await dbContext.SaveChangesAsync();

                    return Results.Created($"/api/credentials/{newCredentials.Id}", new ApiCredentialsDto
                    {
                        Id = newCredentials.Id,
                        AppId = newCredentials.AppId,
                        AppSecret = "••••••••", // Mask the secret
                        RedirectUrl = newCredentials.RedirectUrl,
                        IsActive = newCredentials.IsActive,
                        CreatedAt = newCredentials.CreatedAt
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });

            // Delete API credentials
            group.MapDelete("/{id}", async (
                int id,
                [FromServices] AppDbContext dbContext,
                [FromServices] IUserService userService) =>
            {
                try
                {
                    var user = await userService.GetDefaultUserAsync();
                    if (user == null)
                    {
                        return Results.NotFound("No user found");
                    }

                    var credentials = await dbContext.ApiCredentials
                        .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

                    if (credentials == null)
                    {
                        return Results.NotFound($"API credentials with ID {id} not found");
                    }

                    dbContext.ApiCredentials.Remove(credentials);
                    await dbContext.SaveChangesAsync();

                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });
        }
    }
}
