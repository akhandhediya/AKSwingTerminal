# Fyers API Integration - Phase 1 Documentation

## Overview

This document provides comprehensive documentation for Phase 1 of the Fyers API integration project. Phase 1 focuses on setting up the project structure, implementing authentication with the Fyers API, and creating a trading dashboard.

## Project Structure

The project follows a clean, organized structure with the following components:

```
FyersApiIntegration/
├── src/
│   ├── FyersApiIntegration.Api/           # Main application
│   │   ├── Controllers/                   # MVC controllers
│   │   ├── Data/                          # Database context and migrations
│   │   ├── Endpoints/                     # Minimal API endpoints
│   │   ├── Middleware/                    # Custom middleware
│   │   ├── Models/                        # Domain models and DTOs
│   │   ├── Services/                      # Business logic services
│   │   ├── Views/                         # Razor views
│   │   │   ├── Auth/                      # Authentication views
│   │   │   ├── Home/                      # Home views
│   │   │   ├── Shared/                    # Shared layouts
│   │   │   └── Trading/                   # Trading views
│   │   ├── wwwroot/                       # Static files
│   │   │   ├── css/                       # Stylesheets
│   │   │   ├── js/                        # JavaScript files
│   │   │   ├── lib/                       # Library files
│   │   │   └── images/                    # Image files
│   │   ├── Program.cs                     # Application entry point
│   │   └── appsettings.json               # Configuration settings
│   └── FyersApiIntegration.Tests/         # Unit tests
│       ├── Controllers/                   # Controller tests
│       └── Services/                      # Service tests
└── FyersApiIntegration.sln                # Solution file
```

## Technology Stack

- **Backend**: ASP.NET Core 8.0 with Minimal API and MVC
- **Frontend**: Razor views with jQuery and JavaScript
- **UI Framework**: Bootstrap 5
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: Fyers API OAuth flow
- **Testing**: xUnit with Moq for unit testing

## Database Schema

The database schema includes the following tables:

### User

Stores information about the application user.

```sql
CREATE TABLE [dbo].[Users] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Username] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(255) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

### ApiCredentials

Stores Fyers API credentials for authentication.

```sql
CREATE TABLE [dbo].[ApiCredentials] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] INT NOT NULL,
    [AppId] NVARCHAR(100) NOT NULL,
    [AppSecret] NVARCHAR(255) NOT NULL,
    [RedirectUrl] NVARCHAR(255) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_ApiCredentials_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);
```

### FyersToken

Stores authentication tokens received from Fyers API.

```sql
CREATE TABLE [dbo].[FyersTokens] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] INT NOT NULL,
    [AccessToken] NVARCHAR(MAX) NOT NULL,
    [RefreshToken] NVARCHAR(MAX) NOT NULL,
    [ExpiresAt] DATETIME2 NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_FyersTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);
```

## Authentication Flow

The authentication flow with Fyers API follows these steps:

1. **Setup API Credentials**:
   - User enters Fyers API credentials (App ID, App Secret, Redirect URL)
   - Credentials are stored securely in the database

2. **Generate Auth URL**:
   - System generates an authentication URL using the Fyers API
   - User is redirected to the Fyers login page

3. **Authorization**:
   - User logs in to their Fyers account and authorizes the application
   - Fyers redirects back to the application with an authorization code

4. **Token Generation**:
   - Application exchanges the authorization code for access and refresh tokens
   - Tokens are stored securely in the database

5. **Token Refresh**:
   - System automatically refreshes tokens before they expire
   - Both background service and action filter mechanisms are implemented

## Key Components

### Services

#### FyersAuthService

Handles authentication with the Fyers API.

```csharp
public interface IFyersAuthService
{
    Task<AuthUrlResponse> GenerateAuthUrlAsync(string appId, string redirectUri);
    Task<TokenResponse> GenerateTokenAsync(TokenRequest request);
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> ValidateTokenAsync(string accessToken);
}
```

#### TokenService

Manages authentication tokens.

```csharp
public interface ITokenService
{
    Task<FyersToken> GetLatestTokenAsync();
    Task<FyersToken> SaveTokenAsync(TokenResponse tokenResponse, int userId);
    Task<bool> DeleteTokenAsync(int tokenId);
    Task<bool> IsTokenExpiredAsync();
    Task<bool> RefreshTokenIfNeededAsync();
    Task<bool> ValidateTokenAsync(string accessToken);
}
```

#### ApiCredentialsService

Manages Fyers API credentials.

```csharp
public interface IApiCredentialsService
{
    Task<List<ApiCredentialsDto>> GetAllCredentialsAsync(int userId);
    Task<ApiCredentialsDto> GetCredentialByIdAsync(int userId, int credentialId);
    Task<ApiCredentialsDto> GetActiveCredentialAsync(int userId);
    Task<ApiCredentialsDto> CreateCredentialsAsync(int userId, ApiCredentialsUpdateDto dto);
    Task<bool> UpdateCredentialsAsync(int userId, int credentialId, ApiCredentialsUpdateDto dto);
    Task<bool> DeleteCredentialsAsync(int userId, int credentialId);
    Task<bool> SetCredentialsActiveStatusAsync(int userId, int credentialId, bool isActive);
}
```

### Controllers

#### AuthController

Handles authentication-related views and actions.

```csharp
public class AuthController : Controller
{
    // Actions:
    public async Task<IActionResult> Setup();
    public async Task<IActionResult> Connect(int id);
    public async Task<IActionResult> Callback(string code);
    public async Task<IActionResult> Logout();
}
```

#### ApiCredentialsController

Manages API credentials.

```csharp
public class ApiCredentialsController : Controller
{
    // Actions:
    public async Task<IActionResult> Index();
    public async Task<IActionResult> Create();
    public async Task<IActionResult> Create(ApiCredentialsUpdateDto dto);
    public async Task<IActionResult> Edit(int id);
    public async Task<IActionResult> Edit(int id, ApiCredentialsUpdateDto dto);
    public async Task<IActionResult> Delete(int id);
    public async Task<IActionResult> SetActive(int id);
}
```

#### TradingController

Handles trading dashboard and related functionality.

```csharp
public class TradingController : Controller
{
    // Actions:
    public async Task<IActionResult> Dashboard();
}
```

### Token Refresh Mechanism

Two complementary mechanisms ensure tokens are always valid:

1. **Background Service**: Periodically checks for token expiry and refreshes tokens proactively.

```csharp
public class TokenRefreshBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Checks token expiry every 15 minutes and refreshes if needed
    }
}
```

2. **Action Filter**: Checks token validity before controller actions and refreshes if needed.

```csharp
public class TokenRefreshActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Checks token before controller actions and refreshes if needed
    }
}
```

## User Interface

### Authentication Setup

The authentication setup page allows users to:
- View existing API credentials
- Add new API credentials
- Edit existing credentials
- Set active credentials for authentication
- Delete credentials

### Fyers API Connection

The connection page provides:
- Authentication URL for Fyers login
- Instructions for connecting to Fyers
- Connection status information
- Token information and expiry details

### Trading Dashboard

The trading dashboard includes:
- Account summary with balance and margin information
- Watchlist for tracking symbols
- Market depth view
- Market indices display
- Holdings table
- Positions table
- Recent orders list
- Quick order form for placing trades

## Testing

Unit tests cover the following components:

### FyersAuthService Tests

- Generating authentication URLs
- Token generation
- Token refreshing
- Token validation

### TokenService Tests

- Retrieving latest tokens
- Saving tokens
- Deleting tokens
- Checking token expiration
- Token validation

### AuthController Tests

- Setup page functionality
- Connection page functionality
- Callback handling
- Logout functionality

## Configuration

The application uses the following configuration settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FyersApiIntegration;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "FyersApi": {
    "BaseUrl": "https://api.fyers.in/api/v3",
    "AuthUrl": "https://api.fyers.in/api/v3/auth"
  },
  "JwtSettings": {
    "SecretKey": "your_secret_key_here",
    "ExpiryInMinutes": 60,
    "Issuer": "FyersApiIntegration",
    "Audience": "FyersApiIntegration"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## Deployment Instructions

1. **Database Setup**:
   - Create a SQL Server database
   - Update the connection string in `appsettings.json`
   - Run Entity Framework migrations: `dotnet ef database update`

2. **Application Settings**:
   - Update Fyers API configuration in `appsettings.json`
   - Set a secure JWT secret key

3. **Build and Deploy**:
   - Build the application: `dotnet build`
   - Publish the application: `dotnet publish -c Release`
   - Deploy to IIS or other hosting environment

## Next Steps (Phase 2)

1. Implement WebSocket integration for real-time market data
2. Add order placement functionality
3. Implement portfolio management features
4. Create reporting and analytics dashboard
5. Add user preferences and settings
6. Implement advanced trading features (alerts, strategies)

## Conclusion

Phase 1 of the Fyers API integration project provides a solid foundation with authentication, token management, and a basic trading dashboard. The architecture is designed to be scalable and maintainable, with clear separation of concerns and comprehensive test coverage.
