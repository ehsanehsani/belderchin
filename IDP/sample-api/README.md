# Sample API with ORY Authentication

This is a sample .NET Web API project that demonstrates authentication and authorization using the ORY IDP stack (Kratos, Hydra, Oathkeeper).

## Features

### Authentication & Authorization
- **JWT Token Validation**: Validates tokens issued by ORY Hydra
- **Role-based Access Control**: Different endpoints for different user roles
- **Public Endpoints**: Accessible without authentication
- **User Endpoints**: Require valid JWT token
- **Admin Endpoints**: Require admin role

### API Endpoints

#### Public Endpoints (No Authentication Required)
- `GET /api/public/info` - Get public information
- `GET /api/public/status` - Get system status
- `POST /api/public/echo` - Echo endpoint for testing
- `GET /health` - Health check endpoint

#### User Endpoints (Authentication Required)
- `GET /api/user/profile` - Get current user profile
- `GET /api/user/data` - Get user data
- `PUT /api/user/profile` - Update user profile
- `GET /api/user/preferences` - Get user preferences

#### Admin Endpoints (Admin Role Required)
- `GET /api/admin/users` - Get all users
- `GET /api/admin/statistics` - Get system statistics
- `PUT /api/admin/users/{userId}/role` - Update user role
- `DELETE /api/admin/users/{userId}` - Delete user
- `GET /api/admin/dashboard` - Get admin dashboard

## Setup

### Prerequisites
- .NET 8.0 SDK
- ORY IDP stack running (Kratos, Hydra, Oathkeeper)
- Docker (for running the IDP stack)

### Configuration

1. **Update appsettings.json** with your ORY Hydra configuration:
```json
{
  "Jwt": {
    "Issuer": "http://localhost:4444/",
    "Audience": "sample-api"
  }
}
```

2. **Start the ORY IDP stack**:
```bash
cd ../infra
docker compose -f docker-compose.yml up -d
```

3. **Run the sample API**:
```bash
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## Authentication Flow

### 1. Register a User
Use your ORY Kratos instance to register a user with phone number and password.

### 2. Login and Get Tokens
Authenticate with ORY Kratos and obtain OAuth2 tokens from ORY Hydra.

### 3. Access Protected Endpoints
Include the JWT access token in the Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

## Testing the API

### Public Endpoints (No Token Required)
```bash
# Get public info
curl -X GET "http://localhost:5000/api/public/info"

# Get system status
curl -X GET "http://localhost:5000/api/public/status"

# Echo test
curl -X POST "http://localhost:5000/api/public/echo" \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello World"}'
```

### User Endpoints (Requires JWT Token)
```bash
# Get user profile (replace <token> with your JWT)
curl -X GET "http://localhost:5000/api/user/profile" \
  -H "Authorization: Bearer <token>"

# Get user data
curl -X GET "http://localhost:5000/api/user/data" \
  -H "Authorization: Bearer <token>"

# Update user profile
curl -X PUT "http://localhost:5000/api/user/profile" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"email": "newemail@example.com"}'
```

### Admin Endpoints (Requires Admin Role)
```bash
# Get all users
curl -X GET "http://localhost:5000/api/admin/users" \
  -H "Authorization: Bearer <admin-token>"

# Get system statistics
curl -X GET "http://localhost:5000/api/admin/statistics" \
  -H "Authorization: Bearer <admin-token>"

# Update user role
curl -X PUT "http://localhost:5000/api/admin/users/user-123/role" \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{"role": "admin"}'
```

## Sample Users

The service includes sample users for testing:
- **Regular User**: `user-123` (phone: +989121234567)
- **Admin User**: `admin-456` (phone: +989128765432)
- **Regular User 2**: `user-789` (phone: +989121112223)

Note: These are sample in-memory users. In a real application, you would integrate with your ORY Kratos user store.

## Role-based Authorization

The API implements role-based access control:

- **Public**: No authentication required
- **User**: Requires valid JWT token (any authenticated user)
- **Admin**: Requires valid JWT token with `role` claim set to `admin`

## Development

### Project Structure
```
sample-api/
├── Controllers/
│   ├── PublicController.cs     # Public endpoints
│   ├── UserController.cs       # User endpoints
│   └── AdminController.cs      # Admin endpoints
├── Services/
│   ├── IUserService.cs         # Service interface
│   └── UserService.cs          # Service implementation
├── Models/
│   └── UserModels.cs           # Data models
├── Program.cs                  # Application entry point
├── appsettings.json            # Configuration
└── README.md                   # This file
```

### Adding New Endpoints
1. Create appropriate controller (Public, User, or Admin)
2. Add authentication/authorization attributes:
   - `[AllowAnonymous]` for public endpoints
   - `[Authorize]` for user endpoints
   - `[Authorize(Policy = "AdminOnly")]` for admin endpoints
3. Implement your business logic

### JWT Token Validation
The API automatically validates JWT tokens from ORY Hydra:
- Validates token signature using Hydra's JWKS endpoint
- Validates issuer and audience claims
- Validates token expiration

## Integration with ORY IDP

This sample API is designed to work with the ORY IDP stack in this repository:

1. **ORY Kratos**: User identity management and authentication
2. **ORY Hydra**: OAuth2/OIDC token issuance
3. **ORY Oathkeeper**: API gateway for token validation (optional)

The API can be deployed behind Oathkeeper for additional security, or validate tokens directly as shown in this sample.

## Security Considerations

- Always use HTTPS in production
- Validate all input data
- Implement proper logging and monitoring
- Use short-lived access tokens
- Implement token refresh mechanisms
- Consider rate limiting for public endpoints
