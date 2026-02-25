# 📮 Postman Collections

This folder contains Postman collections for testing the Belderchin IDP system with the **new unified authentication architecture**.

## 📋 Available Collections

### 🎯 **NEW: Simplified Auth Flow Collection**
- **[NEW-AUTH-FLOW.postman_collection.json](./NEW-AUTH-FLOW.postman_collection.json)** ⭐ **RECOMMENDED**
- **Status**: ✅ **CURRENT ARCHITECTURE**
- **Flow**: Direct Auth-Bridge to JWT to Backend APIs
- **Benefits**: 50% fewer API calls, simplified logic

### 🔄 **LEGACY: Mobile Auth Collection**
- **[Belderchin-IDP-Mobile-Auth-Collection.postman_collection.json](./Belderchin-IDP-Mobile-Auth-Collection.postman_collection.json)**
- **Status**: ⚠️ **DEPRECATED ARCHITECTURE**
- **Flow**: Auth-Bridge → Backend Login → Backend APIs
- **Usage**: For reference and migration purposes only

---

## 🎯 **NEW SIMPLIFIED AUTH FLOW (RECOMMENDED)**

### 🔄 **Architecture Overview**
```
🔹 NEW SIMPLIFIED FLOW:
1. Client → Auth-Bridge verification → JWT Token + User Data
2. Client → Backend APIs (JWT)

🔹 OLD COMPLEX FLOW:
1. Client → Auth-Bridge verification → Session Token
2. Client → Backend Login (session + phone) → JWT Token  
3. Client → Backend APIs (JWT)
```

### ✅ **Key Benefits of New Flow**
- ✅ **50% fewer API calls** for authentication
- ✅ **Single source of truth** for user data
- ✅ **Better performance** (no inter-service auth calls)
- ✅ **Simplified client logic** 
- ✅ **Centralized authentication management**

### 🎯 **Collection Features**

#### **Auth-Bridge Endpoints (Port 8080)**
- ✅ **Start Verification**: `POST /verification/start`
- ✅ **Confirm Verification & Get JWT**: `POST /verification/confirm` 
- ✅ **Get User Profile**: `GET /verification/profile` (JWT protected)
- ✅ **2FA Support**: `POST /verification/2fa/start`, `POST /verification/2fa/confirm`
- ✅ **Development Tools**: Test user creation, JWT validation

#### **Backend Endpoints (Port 8082)**
- ✅ **Public Endpoint**: `GET /api/test/public`
- ✅ **Protected API**: `GET /api/test/user-info` (JWT required)
- ✅ **Business Logic**: Course management, user progress, etc.

#### **Smart Features**
- 🤖 **Automatic JWT Handling**: Tokens stored in variables automatically
- 🔄 **Flow Management**: Variables passed between requests
- 📝 **Detailed Logging**: Console output for debugging
- ✅ **Error Handling**: Clear success/failure indicators

---

## 🚀 Quick Start

### Prerequisites:
1. **Docker Compose**: All services running (`docker-compose up -d`)
2. **Postman**: Desktop application installed
3. **Environment**: Local development environment

### Setup Instructions:

#### 1. **Import Collection**
- Open Postman
- Click "Import" → "File"
- Select `NEW-AUTH-FLOW.postman_collection.json`
- Collection will appear with "🎯" prefix

#### 2. **Configure Variables**
- Open collection variables
- Verify default values:
  ```
  auth_bridge_url: http://localhost:8080
  backend_url: http://localhost:8082
  ```

#### 3. **Run Authentication Flow**
```
🎯 NEW FLOW (Recommended):
1. 📱 Start Verification → Get verification_id
2. ✅ Confirm Verification → Get JWT token + user data
3. 📚 Access Backend APIs → Use JWT token automatically
```

#### 4. **Test Development Tools**
- Create test users for development
- Validate JWT tokens
- Test backend integration

---

## 📊 **Collection Variables**

| Variable | Default Value | Description |
|-----------|---------------|-------------|
| `auth_bridge_url` | `http://localhost:8080` | Auth-Bridge service URL |
| `backend_url` | `http://localhost:8082` | Backend service URL |
| `verification_id` | *auto-set* | Verification ID from start response |
| `2fa_id` | *auto-set* | 2FA verification ID |
| `jwt_token` | *auto-set* | JWT token from confirmation |
| `user_id` | *auto-set* | User ID from JWT token |
| `user_phone` | *auto-set* | User phone number |
| `user_email` | *auto-set* | User email address |

---

## 🧪 Testing Scenarios

### ✅ **Working Scenarios**

#### 1. **Complete Authentication Flow**
```
1. POST /verification/start (phone/email)
   → Response: verification_id + delivery_method

2. POST /verification/confirm (id + code)
   → Response: jwt_token + user_data

3. GET /api/test/user-info (with JWT)
   → Response: user_info + authentication_data
```

#### 2. **2FA Authentication**
```
1. POST /verification/2fa/start
   → Response: 2fa_id

2. POST /verification/2fa/confirm (id + code)
   → Response: jwt_token + user_data
```

#### 3. **Profile Management**
```
1. GET /verification/profile (with JWT)
   → Response: user_profile_data

2. PUT /verification/profile (with JWT + data)
   → Response: updated_profile
```

#### 4. **Development Testing**
```
1. POST /test/create-user
   → Response: test_user + jwt_token

2. GET /test/validate-jwt (with JWT)
   → Response: validation_result + claims
```

### ⚠️ **Expected Behaviors**

#### **Success Indicators**
- ✅ **200 OK**: Verification started successfully
- ✅ **JWT Token**: Authentication complete, ready for API calls
- ✅ **User Data**: Profile information retrieved
- ✅ **Backend Access**: APIs accessible with JWT

#### **Error Handling**
- ❌ **400 Bad Request**: Invalid phone/email format
- ❌ **401 Unauthorized**: Invalid verification code or JWT
- ❌ **404 Not Found**: Verification ID not found
- ❌ **429 Too Many Requests**: Too many attempts

---

## 🔧 Configuration

### Environment Setup
- **Auth-Bridge**: `http://localhost:8080` (Port 8080)
- **Backend**: `http://localhost:8082` (Port 8082)
- **Services**: All running via Docker Compose

### Test Data
- **Phone**: `+1234567890` (test phone number)
- **Email**: `test@example.com` (test email)
- **Code**: `123456` (test verification code)

---

## 🎯 Success Indicators

When everything is working correctly with the **new architecture**:

- ✅ **Verification starts** return verification IDs
- ✅ **JWT tokens issued** directly from confirmation
- ✅ **Backend APIs accessible** with JWT tokens
- ✅ **User data retrieved** from Redis cache
- ✅ **No login step needed** in backend
- ✅ **Performance improved** with fewer API calls

---

## 🔄 Migration Notes

### From LEGACY to NEW:
1. **Stop using** `Belderchin-IDP-Mobile-Auth-Collection.postman_collection.json`
2. **Start using** `NEW-AUTH-FLOW.postman_collection.json`
3. **Update client applications** to use new flow
4. **Remove backend login API** calls from applications

### Benefits:
- **Simplified code**: Fewer authentication steps
- **Better performance**: Reduced network calls
- **Easier maintenance**: Single auth service
- **Modern architecture**: JWT-first design

---

## 📚 Documentation

For detailed setup and testing instructions:
- **[Architecture Migration Summary](../docs/ARCHITECTURE-MIGRATION-SUMMARY.md)**
- **[Setup Guide](../docs/SETUP-GUIDE.md)**
- **[Comprehensive Test Results](../docs/COMPREHENSIVE-TEST-RESULTS.md)**

---

## 🎉 Status

**✅ NEW AUTH FLOW COLLECTION READY FOR PRODUCTION**

*Last updated: February 25, 2026 - Architecture Migration Complete*
