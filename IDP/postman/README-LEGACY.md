# 📮 Postman Collections

This folder contains Postman collections for testing the Belderchin IDP system.

## 📋 Available Collections

### 🎯 Mobile Authentication Collection
- **[Belderchin-IDP-Mobile-Auth-Collection.postman_collection.json](./Belderchin-IDP-Mobile-Auth-Collection.postman_collection.json)**

Complete mobile authentication workflow with code-based login for Belderchin Identity Provider - **FULLY TESTED AND WORKING**

#### Features:
- ✅ **Mobile Registration**: Register new users with phone number and SMS verification
- ✅ **Code Login**: Login existing users with phone number and SMS code (no password required)
- ✅ **Password Login**: Traditional password-based login for users who prefer it
- ✅ **Session Management**: Check current session and logout functionality
- ✅ **Admin Tools**: Administrative tools for user management and debugging

#### Authentication Flows:
1. **Mobile Registration Flow**
   - Get Registration Flow
   - Submit Registration - Request SMS Code
   - Submit Registration - Verify SMS Code

2. **Mobile Code Login Flow**
   - Get Code Login Flow
   - Request Login Code
   - Submit Login Code

3. **Password Login Flow** (Optional)
   - Get Password Login Flow
   - Submit Password Login

4. **Session Management**
   - Get Current Session
   - Logout

5. **Admin Tools**
   - Get User by Phone
   - Get User Credentials

---

## 🚀 Quick Start

### Prerequisites:
1. **Docker Compose**: All services running (`docker-compose up -d`)
2. **Postman**: Desktop application installed
3. **Environment**: Local development environment

### Setup Instructions:
1. **Import Collection**:
   - Open Postman
   - Click "Import" → "File"
   - Select `Belderchin-IDP-Mobile-Auth-Collection.postman_collection.json`

2. **Configure Variables**:
   - Open collection variables
   - Verify `baseUrl` is `http://localhost:4433`
   - Verify `baseUrlAdmin` is `http://localhost:4434`
   - Update `phoneNumber` as needed

3. **Run Tests**:
   - Execute requests in sequence
   - Check console output for verification codes
   - Use codes from Kratos logs for verification

### Getting Verification Codes:
```bash
# Get the latest verification code
docker-compose logs kratos | grep registration_code | tail -1
```

---

## 🔧 Configuration

### Collection Variables:
| Variable | Default Value | Description |
|----------|---------------|-------------|
| `baseUrl` | `http://localhost:4433` | Kratos Public API URL |
| `baseUrlAdmin` | `http://localhost:4434` | Kratos Admin API URL |
| `phoneNumber` | `+1234567890` | Test phone number |
| `password` | `SuperSecurePassword!2024#Random$%^&*` | Test password |
| `verificationCode` | `123456` | ⚠️ Update with actual code |
| `csrfToken` | *auto-set* | CSRF protection token |

### Environment Requirements:
- **Kratos**: Running on ports 4433/4434
- **Hydra**: Running on ports 4444/4445
- **Postgres**: Database backend
- **SMS Service**: Optional (for actual SMS delivery)

---

## 🧪 Testing Scenarios

### ✅ Working Scenarios:
1. **New User Registration**: Complete phone number registration with SMS verification
2. **Existing User Login**: Login with phone number and SMS code
3. **Password Authentication**: Traditional login with phone and password
4. **Session Management**: Check active sessions and logout
5. **Admin Operations**: User lookup and credential management

### ⚠️ Configuration Notes:
- **SMS Service**: Requires `KAVENEGAR_API_KEY` for actual SMS delivery
- **CSRF Tokens**: Automatically extracted from flow responses
- **Phone Validation**: Uses international format `+[1-9][1-14 digits]`

---

## 📚 Documentation

For detailed setup and testing instructions:
- **[POSTMAN-GUIDE.md](../docs/POSTMAN-GUIDE.md)** - Complete usage guide
- **[COMPREHENSIVE-TEST-RESULTS.md](../docs/COMPREHENSIVE-TEST-RESULTS.md)** - Test results
- **[SETUP-GUIDE.md](../docs/SETUP-GUIDE.md)** - System setup instructions

---

## 🔍 Troubleshooting

### Common Issues:
1. **400 Bad Request**: Check CSRF token is included
2. **Phone Validation**: Use international format with country code
3. **Verification Code**: Get latest code from Kratos logs
4. **Service Unavailable**: Ensure all Docker services are running

### Debug Commands:
```bash
# Check service status
docker-compose ps

# View Kratos logs
docker-compose logs kratos

# Get verification codes
docker-compose logs kratos | grep registration_code
```

---

## 🎯 Success Indicators

When everything is working correctly:
- ✅ Registration flow returns `state: "sent_email"`
- ✅ Verification codes appear in Kratos logs
- ✅ User creation returns identity information
- ✅ Session check returns active user data
- ✅ Admin tools find user by phone number

---

*Last updated: February 10, 2026*  
*Version: 1.0 - Fully Tested*
