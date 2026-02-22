# 📚 Belderchin IDP Documentation

Welcome to the Belderchin IDP documentation folder. This contains all project documentation organized for easy reference.

## 📋 Available Documentation

### 🚀 Getting Started
- **[SETUP-GUIDE.md](./SETUP-GUIDE.md)** - Detailed setup and configuration instructions

### 🔧 Technical Documentation
- **[IMPLEMENTATION-SUMMARY.md](./IMPLEMENTATION-SUMMARY.md)** - Complete implementation overview
- **[CODE-GENERATION-LOCATIONS.md](./CODE-GENERATION-LOCATIONS.md)** - Code generation and file locations
- **[CODE-STORAGE-EXPLANATION.md](./CODE-STORAGE-EXPLANATION.md)** - Code storage and architecture explanation

### 🧪 Testing & Results
- **[COMPREHENSIVE-TEST-RESULTS.md](./COMPREHENSIVE-TEST-RESULTS.md)** - Complete system test results
- **[FINAL-TEST-RESULTS.md](./FINAL-TEST-RESULTS.md)** - Final testing summary
- **[debug.md](./debug.md)** - Debugging guide and troubleshooting

### 📮 API Testing
- **[POSTMAN-GUIDE.md](./POSTMAN-GUIDE.md)** - Postman collection usage guide

---

## 🏗️ Project Overview

This repository contains an ORY-based Identity Provider (IDP) stack that supports:

- Phone number (E.164, e.g. `+98...`) as the primary identifier
- Mobile-only registration with SMS verification
- Code-based authentication (password optional)
- Token generation/validation for frontend and backend via OAuth2/OIDC
- Local testing with static verification codes

---

## 📂 Project Structure

```
Belderchin-IDP/
├── docs/                    # 📚 All documentation (this folder)
├── postman/                 # 📮 Postman collections
├── infra/                   # 🏗️ Infrastructure and services
│   ├── auth-bridge/        # 🔐 Custom authentication bridge
│   ├── sms-service/        # 📱 SMS delivery service
│   ├── kratos/             # 🆔 Identity management
│   ├── hydra/              # 🔑 OAuth2/OIDC provider
│   ├── oathkeeper/         # 🛡️ API gateway
│   └── postgres-init/      # 🗄️ Database initialization
├── sample-api/             # 🎯 Example protected API
└── Belderchin-IDP.sln      # 📦 Solution file
```

---

## 🚀 Quick Links

### For Development:
1. **Setup Instructions**: [SETUP-GUIDE.md](./SETUP-GUIDE.md)
2. **API Testing**: [POSTMAN-GUIDE.md](./POSTMAN-GUIDE.md)

### For Understanding:
1. **Architecture**: [IMPLEMENTATION-SUMMARY.md](./IMPLEMENTATION-SUMMARY.md)
2. **Code Organization**: [CODE-STORAGE-EXPLANATION.md](./CODE-STORAGE-EXPLANATION.md)

### For Testing:
1. **Test Results**: [COMPREHENSIVE-TEST-RESULTS.md](./COMPREHENSIVE-TEST-RESULTS.md)
2. **Debugging**: [debug.md](./debug.md)

## Architecture

- ORY Kratos: identity, registration/login flows, verification, 2FA
- ORY Hydra: OAuth2/OIDC token issuer (access tokens, ID tokens)
- ORY Oathkeeper: edge proxy / API gateway validating JWT access tokens
- Postgres: persistence for Kratos and Hydra
- `sms-service` (.NET): sends SMS via Kavenegar; can be used by Kratos courier/webhooks
- `auth-bridge` (.NET): app-driven orchestration for SMS verification + 2FA (with email fallback)
- `mailhog` (Docker): fake SMTP server for email testing and development
- `sample-api` (.NET): demonstration API showing ORY authentication integration

## Prerequisites

- Docker + Docker Compose
- .NET 8.0 SDK (for running .NET services locally)

## Quick start

1. Create an `.env` file (see `.env.example`).
2. Start the stack:

```bash
docker compose -f infra/docker-compose.yml up -d
```

3. (Optional) Run the sample API locally:

```bash
cd sample-api
dotnet run
```

The sample API will be available at `https://localhost:5001/swagger` for testing authentication flows.

## MailHog Email Testing

MailHog is included for email testing during development:

### Access MailHog
- **Web UI**: `http://localhost:8025`
- **SMTP Server**: `localhost:1025`

### Features
- Captures all emails sent by the system
- Provides web interface to view emails
- No real email delivery during development
- Works with both Kratos and auth-bridge email sending

### Email Testing Workflow
1. Start the stack with Docker Compose
2. Access MailHog UI at `http://localhost:8025`
3. Trigger verification/2FA flows that use email fallback
4. View captured emails in the MailHog interface
5. Extract verification codes from email content

## Local Testing Setup

### Postman Collection

A comprehensive Postman collection is available for testing the complete authentication workflow:

**File**: `Belderchin-IDP-Auth-Workflow-Final.postman_collection.json`

#### Testing Steps:

1. **Import** the collection into Postman
2. **Get Registration Flow**: Starts the registration process
3. **Submit Registration**: Submits phone number with SMS channel
4. **Get Verification Code**: Check Kratos logs for 6-digit code
5. **Submit Verification**: Complete registration with the code

#### Get Verification Code:

```bash
docker-compose logs kratos | grep "registration_code"
```

#### Example Output:
```
"registration_code":"866440"
```

#### Update Postman Variable:
- Set `verificationCode` to the actual 6-digit code from logs
- Default phone: `+989203020402`
- Default password: `SuperSecurePassword!2024#Random`

### Service Endpoints

| Service | Type | URL | Health Check |
|---------|------|-----|--------------|
| Kratos | Public API | `http://localhost:4433` | `http://localhost:4433/health/ready` |
| Kratos | Admin API | `http://localhost:4434` | `http://localhost:4434/health/ready` |
| Hydra | Public API | `http://localhost:4444` | `http://localhost:4444/health/ready` |
| Hydra | Admin API | `http://localhost:4445` | `http://localhost:4445/health/ready` |
| Oathkeeper | Public API | `http://localhost:4455` | `http://localhost:4455/health/alive` |
| Oathkeeper | Admin API | `http://localhost:4456` | `http://localhost:4456/health/alive` |
| SMS Service | API | `http://localhost:8081` | `http://localhost:8081/health` |
| Auth Bridge | API | `http://localhost:8080` | `http://localhost:8080/health` |
| MailHog | SMTP | `localhost:1025` | N/A |
| MailHog | Web UI | `http://localhost:8025` | N/A |
| Sample API | API | `http://localhost:5000` | `http://localhost:5000/health` |
| PostgreSQL | Database | `localhost:5432` | N/A |

> **Note**: All services are connected to the `belderchin-idp` Docker network for internal communication.

## Phone number rules

- Phone numbers must be stored and sent in international E.164 format (e.g. `+989121234567`).
- Default country code is `+98` (Iran) but users can register from other countries.

## Flows (high level)

- Registration:
  - User signs up with phone number only (password optional)
  - System verifies phone via SMS (6-digit numeric code)
  - User can set password later after verification
- Login:
  - User logs in with phone number (password if set, otherwise SMS verification only)
  - 2FA challenge via SMS (fallback to email)
- Tokens:
  - Frontend obtains OAuth2 tokens from Hydra (OIDC)
  - Backend validates JWT via Oathkeeper/Hydra JWKS

## Mobile-Only Authentication

The system is configured for mobile-only authentication with SMS verification:

### Registration Flow

1. **Get Registration Flow**:
```bash
curl -s "http://localhost:4433/self-service/registration/api" | jq -r '.id'
```

2. **Submit Registration**:
```bash
curl -X POST "http://localhost:4433/self-service/registration?flow=<FLOW_ID>" \
  -H "Content-Type: application/json" \
  -d '{
    "method": "code",
    "traits": {
      "phone": "+989203020402"
    },
    "channel": "sms"
  }'
```

3. **Get Verification Code**:
```bash
docker-compose logs kratos | grep "registration_code"
```

4. **Submit Verification**:
```bash
curl -X POST "http://localhost:4433/self-service/registration?flow=<FLOW_ID>" \
  -H "Content-Type: application/json" \
  -d '{
    "method": "code",
    "traits": {
      "phone": "+989203020402"
    },
    "code": "866440"
  }'
```

### Login Flow

1. **Password Login**:
```bash
curl -X POST "http://localhost:4433/self-service/login?flow=<FLOW_ID>" \
  -H "Content-Type: application/json" \
  -d '{
    "method": "password",
    "identifier": "+989203020402",
    "password": "SuperSecurePassword!2024#Random"
  }'
```

2. **Code Login**:
```bash
curl -X POST "http://localhost:4433/self-service/login?flow=<FLOW_ID>" \
  -H "Content-Type: application/json" \
  -d '{
    "method": "code",
    "identifier": "+989203020402",
    "channel": "sms"
  }'
```

## Configuration

### Kratos Configuration

Key configuration files:

- `infra/kratos/kratos.yml` - Main Kratos configuration
- `infra/kratos/identity.schema.json` - Phone-only identity schema

#### Identity Schema Features:
- Phone number as primary identifier
- Support for both password and code credentials
- E.164 phone number validation
- Mobile-only authentication

#### Courier Configuration:
- Basic SMTP setup for code generation
- 6-digit numeric codes for verification
- Email templates for fallback (if configured)

### Database Setup

The system uses PostgreSQL with proper schema migrations:

```bash
# Run migrations
docker-compose run --rm kratos-migrate

# Check credential types
docker-compose exec postgres psql -U ory -d ory_kratos -c "SELECT * FROM identity_credential_types;"
```

### Sample API

A demonstration .NET Web API showing how to integrate with the ORY IDP stack for authentication and authorization.

#### Features

- **JWT Token Validation**: Validates tokens issued by ORY Hydra
- **Role-based Access Control**: Public, User, and Admin endpoints
- **Swagger Documentation**: Interactive API documentation with JWT support
- **Sample Endpoints**: Complete CRUD operations with different authorization levels

#### Quick Start

1. Start the IDP stack:
```bash
docker compose -f infra/docker-compose.yml up -d
```

2. Run the sample API:
```bash
cd sample-api
dotnet run
```

3. Access the API:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

#### API Endpoints

- **Public**: `/api/public/*` - No authentication required
- **User**: `/api/user/*` - Requires valid JWT token
- **Admin**: `/api/admin/*` - Requires admin role

For detailed documentation, see [sample-api/README.md](sample-api/README.md).

## Development & Debugging

### VS Code Debugging

The project includes comprehensive debugging configurations for VS Code:

- **Debug SMS Service**: Starts SMS service on port 8081
- **Debug Auth Bridge**: Starts auth bridge on port 8080  
- **Debug Sample API**: Starts sample API
- **Debug All Services**: Starts all .NET services simultaneously

### Debugging Workflow

1. Start infrastructure services:
```bash
docker compose -f infra/docker-compose.yml up -d
```

2. Use VS Code debug configurations to run .NET services locally with breakpoints

3. For detailed debugging instructions, see [debug.md](debug.md)

### IDE Support

- **VS Code**: Use provided launch configurations
- **Visual Studio / Rider**: Automatic launch settings detection

## Production Deployment

For production deployment, you must:

- Configure real SMTP credentials for email delivery
- Set up proper SMS service (Kavenegar or other provider)
- Configure allowed CORS origins for your frontend
- Use proper SSL certificates
- Set up monitoring and logging
- Configure backup and disaster recovery

## Troubleshooting

### Common Issues

1. **Code Verification Fails**: Check Kratos logs for the actual 6-digit code
2. **Database Issues**: Run migrations and check credential types table
3. **Courier Configuration**: Ensure SMTP is properly configured
4. **Network Issues**: Check Docker network connectivity

### Debug Commands

```bash
# Check Kratos logs
docker-compose logs kratos

# Check database schema
docker-compose exec postgres psql -U ory -d ory_kratos -c "\dt"

# Check registration codes
docker-compose exec postgres psql -U ory -d ory_kratos -c "SELECT * FROM identity_registration_codes LIMIT 5;"
```

## Notes

This stack is meant as a production-ready baseline. You must:

- Provide real SMTP credentials
- Provide a Kavenegar API key
- Configure allowed CORS origins for your frontend
- Set up proper monitoring and logging
