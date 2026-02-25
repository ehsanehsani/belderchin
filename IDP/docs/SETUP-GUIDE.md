# 🚀 Belderchin IDP - Setup Guide (NEW Architecture)

## 🎯 **Overview**

Complete setup guide for Belderchin Identity Provider with **unified authentication architecture** using Auth-Bridge as the complete authentication service.

**🆕 NEW ARCHITECTURE FEATURES**:
- 🎯 **Unified Auth Service**: Auth-Bridge handles all authentication
- 🔐 **Direct JWT Issuance**: No separate login step needed
- 📱 **Mobile-First**: Phone + SMS verification
- 🗄️ **Centralized User Data**: Redis-based user management
- ⚡ **Better Performance**: 50% fewer API calls
- 🔧 **Simplified Integration**: Clear service boundaries

---

## 🔄 **Architecture Comparison**

### 🆕 **NEW Simplified Architecture**
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Mobile App   │    │   Web App     │    │   Client      │
│                │    │                │    │                │
│ 📱 +989xxxxx │◄──►│ 🔐 Login      │◄──►│ 🧪 Testing    │
│                │    │                │    │                │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Auth-Bridge (Port 8080)                   │
│                                                         │
│  🎯 Complete Auth Service:                              │
│  📱 Verification  🔐 JWT Issuance  👤 Profile Mgmt      │
│                                                         │
│  🗄️ Redis (User Data Cache)                            │
└─────────────────────────────────────────────────────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Backend API    │    │  Other Services │    │  Future Apps   │
│  (Port 8082)   │    │                │    │                │
│ 📚 Courses     │    │ 🔧 Business     │    │ 🚀 New Features│
│ 👤 User Progress│    │    Logic       │    │                │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### 🔄 **OLD Complex Architecture (Deprecated)**
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Mobile App   │    │   Web App     │    │   Client      │
│                │    │                │    │                │
│ 📱 +989xxxxx │◄──►│ 🔐 Login      │◄──►│ 🧪 Testing    │
│                │    │                │    │                │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Auth-Bridge (Port 8080)                   │
│                                                         │
│  📱 Verification  🎮 Session Management                   │
│                                                         │
│  🗄️ Redis (Session Storage)                             │
└─────────────────────────────────────────────────────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Backend API (Port 8082)                   │
│                                                         │
│  🔐 Login API  📚 Business Logic  👤 User Management     │
│                                                         │
│  🗄️ Database (User Data)                                │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📋 **Prerequisites**

### System Requirements
- **Docker**: 20.10+ and Docker Compose
- **Node.js**: 16+ (for development tools)
- **Memory**: 4GB+ RAM
- **Storage**: 10GB+ free space

### Network Requirements
- **Ports**: 8080, 8082, 6379, 5432 must be available
- **Internet**: For package downloads and SMS simulation

---

## 🚀 **Quick Start**

### 1. **Clone and Setup**
```bash
# Clone repository
git clone <repository-url>
cd Belderchin-IDP

# Navigate to infrastructure
cd infra
```

### 2. **Start All Services**
```bash
# Start all services with new architecture
docker-compose up -d

# Wait for services to be ready (30-60 seconds)
docker-compose ps
```

### 3. **Verify Services**
```bash
# Check Auth-Bridge health
curl http://localhost:8080/api/health

# Check Backend health
curl http://localhost:8082/api/health

# Check Redis connection
docker-compose logs redis | grep "Ready to accept connections"
```

### 4. **Test New Authentication Flow**
```bash
# Import new Postman collection
# File: postman/NEW-AUTH-FLOW.postman_collection.json

# Run authentication flow:
# 1. Start Verification → Get verification_id
# 2. Confirm Verification → Get JWT token
# 3. Access Backend APIs → Use JWT token
```

---

## 🎯 **Service Configuration**

### **Auth-Bridge (Port 8080) - Complete Auth Service**

#### **Key Endpoints**
- `POST /verification/start` - Start phone/email verification
- `POST /verification/confirm` - Confirm and get JWT token
- `GET /verification/profile` - Get user profile (JWT protected)
- `POST /verification/2fa/start` - Start 2FA verification
- `POST /verification/2fa/confirm` - Confirm 2FA and get JWT

#### **Environment Variables**
```yaml
environment:
  ASPNETCORE_ENVIRONMENT: Development
  SMTP_HOST: mailhog
  SMTP_PORT: 1025
  SMTP_USERNAME: ""
  SMTP_PASSWORD: ""
  SMTP_FROM: noreply@belderchin.local
  REDIS_CONNECTIONSTRING: redis:6379
  JWT_KEY: "belderchin-secret-key-1234567890-abcdefghijklmnopqrstuvwxyz-12"
  JWT_ISSUER: "belderchin"
  JWT_AUDIENCE: "belderchin-users"
```

#### **Swagger Documentation**
- **URL**: `http://localhost:8080/swagger`
- **Features**: Complete API documentation with examples

---

### **Backend (Port 8082) - Business Logic Service**

#### **Key Endpoints**
- `GET /api/test/public` - Public endpoint (no auth)
- `GET /api/test/user-info` - Protected endpoint (JWT required)
- `GET /api/courses` - Course management
- `GET /api/users/me/profile` - User profile (JWT protected)

#### **Environment Variables**
```yaml
environment:
  ASPNETCORE_ENVIRONMENT: Development
  REDIS_CONNECTIONSTRING: redis:6379
  JWT_KEY: "belderchin-secret-key-1234567890-abcdefghijklmnopqrstuvwxyz-12"
  JWT_ISSUER: "belderchin"
  JWT_AUDIENCE: "belderchin-users"
```

#### **Key Changes from OLD Architecture**
- ✅ **Removed**: `POST /api/auth/login` endpoint
- ✅ **Added**: JWT validation middleware
- ✅ **Added**: Redis integration for user data
- ✅ **Enhanced**: Business logic focus only

---

### **Redis (Port 6379) - User Data Cache**

#### **Data Structure**
```
user:{userId} → {
  "id": "user-id",
  "phone": "+1234567890",
  "email": "user@example.com",
  "userType": "Regular",
  "displayName": "User Name",
  "createdAt": "2026-02-25T...",
  "updatedAt": "2026-02-25T..."
}
```

#### **Connection String**
```bash
# Local development
redis:6379

# Production
redis:password@host:port
```

---

## 🧪 **Testing Guide**

### **1. Using Postman Collection**

#### **Import Collection**
1. Open Postman
2. Click "Import" → "File"
3. Select `postman/NEW-AUTH-FLOW.postman_collection.json`
4. Collection appears with "🎯" prefix

#### **Run Authentication Flow**
```
🎯 NEW SIMPLIFIED FLOW:
1. 📱 Start Verification
   → POST /verification/start
   → Response: verification_id

2. ✅ Confirm Verification & Get JWT
   → POST /verification/confirm
   → Response: jwt_token + user_data

3. 📚 Access Backend APIs
   → GET /api/test/user-info
   → Header: Authorization: Bearer <jwt_token>
   → Response: user_info + authentication_data
```

#### **Test Development Tools**
```
🧪 DEVELOPMENT TESTING:
1. POST /test/create-user → Create test user + JWT
2. GET /test/validate-jwt → Validate JWT token
3. GET /api/test/public → Test backend connectivity
```

### **2. Manual Testing**

#### **Start Verification**
```bash
curl -X POST http://localhost:8080/verification/start \
  -H "Content-Type: application/json" \
  -d '{"phone": "+1234567890", "email": "test@example.com"}'
```

#### **Confirm Verification (Get JWT)**
```bash
# Use verification_id from previous response
curl -X POST http://localhost:8080/verification/confirm \
  -H "Content-Type: application/json" \
  -d '{"id": "verification-id", "code": "123456"}'
```

#### **Access Backend API**
```bash
# Use jwt_token from verification response
curl -X GET http://localhost:8082/api/test/user-info \
  -H "Authorization: Bearer <jwt-token>"
```

---

## 🔧 **Configuration Details**

### **JWT Configuration**

#### **Token Claims**
```json
{
  "userId": "user-id",
  "phoneNumber": "+1234567890",
  "userType": "Regular",
  "role": "Regular",
  "email": "user@example.com",
  "tokenSource": "auth-bridge",
  "nbf": 1772007504,
  "exp": 1772612304,
  "iat": 1772007504,
  "iss": "belderchin",
  "aud": "belderchin-users"
}
```

#### **Validation Settings**
```csharp
var validationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = "belderchin",
    ValidateAudience = true,
    ValidAudience = "belderchin-users",
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero,
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(key)
};
```

### **Redis Configuration**

#### **Connection Settings**
```csharp
var options = ConfigurationOptions.Parse(redisConnectionString);
options.AbortOnConnectFail = false;
options.ConnectRetry = 3;
options.ConnectTimeout = 5000;
```

#### **Data Expiration**
```csharp
// User data expires after 1 year
await _redisDb.StringSetAsync($"user:{userId}", userData, TimeSpan.FromDays(365));
```

---

## 🚀 **Development Workflow**

### **1. Local Development**
```bash
# Start services
docker-compose up -d

# Monitor logs
docker-compose logs -f auth-bridge
docker-compose logs -f backend

# Test with Postman
# Import NEW-AUTH-FLOW.postman_collection.json
```

### **2. Code Changes**
```bash
# Rebuild specific service
docker-compose up -d --build auth-bridge
docker-compose up -d --build backend

# Clear Redis cache
docker-compose exec redis redis-cli FLUSHALL
```

### **3. Debugging**
```bash
# Check service status
docker-compose ps

# View logs
docker-compose logs auth-bridge | grep "JWT"
docker-compose logs backend | grep "Redis"

# Test connectivity
curl http://localhost:8080/api/health
curl http://localhost:8082/api/health
```

---

## 📊 **Migration Guide**

### **From OLD to NEW Architecture**

#### **Client Application Changes**
```javascript
// OLD FLOW (Deprecated)
async function login(phone, sessionToken) {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ phone, sessionToken })
  });
  const { token } = await response.json();
  return token;
}

// NEW FLOW (Recommended)
async function verifyAndGetToken(phone, code) {
  const response = await fetch('/verification/confirm', {
    method: 'POST',
    body: JSON.stringify({ id: verificationId, code })
  });
  const { jwt_token } = await response.json();
  return jwt_token;
}
```

#### **Backend Service Changes**
```csharp
// OLD: Login endpoint (Removed)
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request) { ... }

// NEW: JWT validation only
[HttpGet("user-info")]
[Authorize]
public async Task<IActionResult> GetUserInfo() { ... }
```

---

## 🔍 **Troubleshooting**

### **Common Issues**

#### **1. JWT Token Not Working**
```bash
# Check JWT token format
echo "jwt-token" | cut -d'.' -f2 | base64 -d

# Validate JWT with Auth-Bridge
curl -H "Authorization: Bearer <jwt>" \
  http://localhost:8080/test/validate-jwt
```

#### **2. Redis Connection Issues**
```bash
# Check Redis status
docker-compose exec redis redis-cli ping

# Check Redis logs
docker-compose logs redis
```

#### **3. Backend JWT Validation**
```bash
# Check backend logs for JWT errors
docker-compose logs backend | grep "JWT"

# Test backend with valid JWT
curl -H "Authorization: Bearer <valid-jwt>" \
  http://localhost:8082/api/test/user-info
```

#### **4. Service Communication**
```bash
# Check service connectivity
docker-compose exec auth-bridge curl http://backend:8080/api/health
docker-compose exec backend curl http://auth-bridge:8080/api/health
```

---

## 📚 **Documentation**

### **Related Documentation**
- **[Architecture Migration Summary](./ARCHITECTURE-MIGRATION-SUMMARY.md)**
- **[Postman Collection Guide](../postman/README.md)**
- **[Comprehensive Test Results](./COMPREHENSIVE-TEST-RESULTS.md)**

### **API Documentation**
- **Auth-Bridge Swagger**: `http://localhost:8080/swagger`
- **Backend Swagger**: `http://localhost:8082/swagger`

---

## 🎯 **Production Deployment**

### **Environment Variables**
```bash
# Production configuration
export ASPNETCORE_ENVIRONMENT=Production
export REDIS_CONNECTIONSTRING=redis-cluster:6379
export JWT_KEY="your-production-jwt-key"
export SMTP_HOST="your-smtp-provider"
export SMTP_PORT=587
export SMTP_USERNAME="your-smtp-username"
export SMTP_PASSWORD="your-smtp-password"
```

### **Security Considerations**
- ✅ **JWT Key**: Use strong, unique keys
- ✅ **Redis**: Enable authentication and TLS
- ✅ **Network**: Use internal Docker networks
- ✅ **Monitoring**: Set up health checks and logging

---

## 🎉 **Setup Complete**

**✅ NEW ARCHITECTURE READY FOR PRODUCTION**

When you see:
- ✅ Auth-Bridge running on port 8080
- ✅ Backend running on port 8082
- ✅ Redis connected and caching user data
- ✅ JWT tokens issued directly from verification
- ✅ Backend APIs accessible with JWT tokens
- ✅ Postman collection working correctly

**🎯 Your Belderchin IDP is ready with the new unified authentication architecture!**

---

*Last updated: February 25, 2026 - Architecture Migration Complete*
