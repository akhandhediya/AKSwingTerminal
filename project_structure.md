# FyersApiIntegration Project Structure

```
FyersApiIntegration/
├── src/
│   ├── FyersApiIntegration.Api/             # Main API project
│   │   ├── Program.cs                       # Entry point and app configuration
│   │   ├── appsettings.json                 # Configuration settings
│   │   ├── appsettings.Development.json     # Development configuration
│   │   ├── Properties/                      # Project properties
│   │   ├── Endpoints/                       # API endpoint definitions
│   │   │   ├── AuthEndpoints.cs             # Authentication endpoints
│   │   │   ├── UserEndpoints.cs             # User profile endpoints
│   │   │   └── ApiCredentialsEndpoints.cs   # API credentials management
│   │   ├── Services/                        # Business logic services
│   │   │   ├── FyersAuthService.cs          # Fyers authentication service
│   │   │   ├── TokenService.cs              # Token management service
│   │   │   └── UserService.cs               # User management service
│   │   ├── Data/                            # Data access
│   │   │   ├── AppDbContext.cs              # EF Core DB context
│   │   │   ├── Migrations/                  # EF Core migrations
│   │   │   └── Repositories/                # Data repositories
│   │   │       ├── UserRepository.cs        # User data repository
│   │   │       └── ApiCredentialsRepository.cs # API credentials repository
│   │   └── Models/                          # Data models
│   │       ├── User.cs                      # User model
│   │       ├── ApiCredentials.cs            # API credentials model
│   │       ├── FyersToken.cs                # Fyers token model
│   │       └── DTOs/                        # Data transfer objects
│   │           ├── AuthRequest.cs           # Authentication request DTO
│   │           ├── AuthResponse.cs          # Authentication response DTO
│   │           └── UserProfile.cs           # User profile DTO
│   └── FyersApiIntegration.Tests/           # Test project
│       ├── AuthServiceTests.cs              # Authentication service tests
│       ├── TokenServiceTests.cs             # Token service tests
│       └── UserServiceTests.cs              # User service tests
└── FyersApiIntegration.sln                  # Solution file
```
