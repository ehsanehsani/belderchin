# Debugging Configuration

This project includes debugging configurations for both VS Code and Visual Studio/Rider.

## VS Code Debugging

### Prerequisites
1. Install the C# Dev Kit extension for VS Code
2. Have .NET 8.0 SDK installed

### Debug Configurations
- **Debug SMS Service**: Starts the SMS service on port 8081
- **Debug Auth Bridge**: Starts the auth bridge service on port 8080
- **Debug Sample API**: Starts the sample API
- **Debug All Services**: Starts both SMS and Auth Bridge services simultaneously

### Tasks
- **build-all**: Builds all .NET projects
- **run-sms-service**: Runs SMS service
- **run-auth-bridge**: Runs Auth Bridge service
- **run-sample-api**: Runs Sample API
- **docker-compose-up**: Starts Docker services (Kratos, Hydra, etc.)
- **docker-compose-down**: Stops Docker services

## Debugging Workflow

### Option 1: Full Stack Debugging
1. Start infrastructure services:
   ```bash
   docker compose -f infra/docker-compose.yml up -d
   ```
2. In VS Code, run "Debug All Services" to start .NET services
3. Set breakpoints in any of the .NET projects

### Option 2: Individual Service Debugging
1. Start infrastructure services:
   ```bash
   docker compose -f infra/docker-compose.yml up -d
   ```
2. Run individual service:
   - SMS Service: "Debug SMS Service"
   - Auth Bridge: "Debug Auth Bridge"
   - Sample API: "Debug Sample API"

### Option 3: Mixed Mode (Docker + Local Debugging)
1. Comment out the service you want to debug locally in `docker-compose.yml`
2. Start remaining services with Docker
3. Run the local service using VS Code debugger

## Environment Variables for Debugging

The launch settings include development environment variables. Update these in:
- `infra/sms-service/Properties/launchSettings.json`
- `infra/auth-bridge/Properties/launchSettings.json`

Required variables to update:
- `KAVENEGAR_API_KEY`: Your Kavenegar API key
- `KAVENEGAR_SENDER`: Your sender number
- `SMTP_CONNECTION_URI`: Your SMTP server (for email fallback)

## Visual Studio / Rider Debugging

### JetBrains Rider

Rider configuration files have been added to `.idea/` directory:

#### Available Run Configurations:
- **Debug SMS Service**: Starts SMS service on port 8081
- **Debug Auth Bridge**: Starts auth bridge service on port 8080
- **Debug Sample API**: Starts sample API
- **Debug All Services**: Starts both SMS and Auth Bridge services simultaneously

#### Usage:
1. Open the project in Rider
2. Go to **Run → Edit Configurations...**
3. Select the desired configuration from the list
4. Set breakpoints in your code
5. Click the Debug button or press `Alt+F5`

#### Configuration Files:
- `.idea/workspace.xml` - Main workspace configuration
- `.idea/runConfigurations/` - Individual run configurations
- `.idea/modules.xml` - Project module configuration
- `.idea/misc.xml` - Miscellaneous project settings

### Visual Studio

Both IDEs will automatically detect the launch settings. Simply:
1. Open the solution or project
2. Set breakpoints
3. Press F5 or use the Run/Debug button

## Port Mappings

| Service | Debug Port | Docker Port |
|---------|------------|-------------|
| SMS Service | 8081 | 8081 |
| Auth Bridge | 8080 | 8080 |
| Sample API | varies | varies |
| Kratos Public | 4433 | 4433 |
| Kratos Admin | 4434 | 4434 |
| Hydra Public | 4444 | 4444 |
| Hydra Admin | 4445 | 4445 |
| Oathkeeper Public | 4455 | 4455 |
| Oathkeeper Admin | 4456 | 4456 |

## Common Debugging Scenarios

### Testing SMS Flow
1. Start infrastructure services
2. Debug Auth Bridge service
3. Set breakpoint in verification endpoint
4. Test with curl/Postman

### Testing Registration Flow
1. Start infrastructure services  
2. Debug both Auth Bridge and SMS services
3. Set breakpoints in registration endpoints
4. Test complete flow

### Attaching to Running Processes
Use "Attach to SMS Service" or "Attach to Auth Bridge" configurations to attach to already running processes.
