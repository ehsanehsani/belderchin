# 📱 Postman Guide - Belderchin IDP Mobile Authentication

## 🎯 **Collection Overview**

**File**: `Belderchin-IDP-Mobile-Auth-Collection.postman_collection.json`

**Features**:
- 📱 **Mobile Registration**: Phone + SMS verification
- 🔐 **Mobile Code Login**: Password-less authentication
- 🔑 **Password Login**: Traditional option
- 🎮 **Session Management**: Complete lifecycle
- 🔧 **Admin Tools**: User management and debugging

---

## 🚀 **Quick Start**

### 1. Import Collection
1. Open Postman
2. Click **Import**
3. Select `Belderchin-IDP-Mobile-Auth-Collection.postman_collection.json`
4. Collection imported with all authentication methods

### 2. Environment Variables
The collection comes with pre-configured variables:

| Variable | Value | Description |
|----------|---------|-------------|
| `baseUrl` | `http://localhost:4433` | Kratos Public API |
| `baseUrlAdmin` | `http://localhost:4434` | Kratos Admin API |
| `phoneNumber` | `+989203020409` | Test phone number |
| `password` | `SuperSecurePassword!2024#Random$%^&*` | Test password |
| `verificationCode` | `123456` | ⚠️ **UPDATE THIS** |

### 3. ⚠️ **IMPORTANT - Update Verification Code**
The `verificationCode` variable **MUST** be updated with the actual 6-digit code:

```bash
# Get the actual verification code
docker-compose logs kratos | grep registration_code
```

**Example Output**:
```
"registration_code":"805364"
```

**Update Postman Variable**:
- Set `verificationCode` to `805364`
- Do this **EVERY TIME** you run a new registration or login

### 📱 **Where Codes Are Generated**
- **Registration Codes**: Generated when users submit phone number
- **Login Codes**: Generated when existing users request login
- **Location**: Kratos logs under `registration_code` field
- **Format**: Always 6-digit numeric codes
- **Expiration**: 10 minutes from generation
- **Command**: `docker-compose logs kratos | grep registration_code`

### 🔧 **No Email Fallback**
- **Configuration**: Email templates disabled in `kratos.yml`
- **Channel**: Only SMS (`channel: "sms"`) 
- **Result**: Clean 200 OK responses without email validation
- **Benefit**: Faster API responses, no email dependency

---

## 📱 **Mobile Registration Workflow**

### **Step 1**: Get Registration Flow
- **Request**: `📱 1. Mobile Registration` → `Get Registration Flow`
- **Expected**: Flow ID in response
- **Console**: `📋 Registration Flow ID: [ID]`

### **Step 2**: Request SMS Code
- **Request**: `Submit Registration - Request SMS Code`
- **Body**: Phone number + SMS channel
- **Expected**: `state: "sent_email"`
- **Console**: `📱 SMS code sent successfully!`

### **Step 3**: Get Verification Code
```bash
# Run this command in terminal
docker-compose logs kratos | grep registration_code
```

**Look for**: `"registration_code":"XXXXXX"`
**Example**: `"registration_code":"805364"`

### **Step 4**: Submit Verification Code
- **Request**: `Submit Registration - Verify SMS Code`
- **Body**: Phone number + 6-digit code
- **Expected**: New user created or existing user verified
- **Console**: `✅ NEW USER CREATED!` or `✅ EXISTING USER VERIFIED!`

---

## 🔐 **Mobile Code Login Workflow**

### **Step 1**: Get Code Login Flow
- **Request**: `🔐 2. Mobile Code Login` → `Get Code Login Flow`
- **Expected**: Flow ID in response
- **Console**: `📋 Code Login Flow ID: [ID]`

### **Step 2**: Request Login Code
- **Request**: `Request Login Code`
- **Body**: Phone number + SMS channel
- **Expected**: `state: "sent_email"`
- **Console**: `📱 Login code sent successfully!`

### **Step 3**: Get Login Code
```bash
# Same command as registration
docker-compose logs kratos | grep registration_code
```

### **Step 4**: Submit Login Code
- **Request**: `Submit Login Code`
- **Body**: Phone number + 6-digit code
- **Expected**: `state: "passed_challenge"`
- **Console**: `🎉 CODE LOGIN SUCCESSFUL!`

**🎯 Key Achievement**: User authenticated **without password**!

---

## 🔑 **Password Login Workflow (Optional)**

### **Step 1**: Get Password Login Flow
- **Request**: `🔑 3. Password Login (Optional)` → `Get Password Login Flow`
- **Expected**: Flow ID in response
- **Console**: `📋 Password Login Flow ID: [ID]`

### **Step 2**: Submit Password Login
- **Request**: `Submit Password Login`
- **Body**: Phone number + password
- **Expected**: Session token and ID
- **Console**: `✅ PASSWORD LOGIN SUCCESSFUL!`

---

## 🎮 **Session Management**

### Check Current Session
- **Request**: `🎮 4. Session Management` → `Get Current Session`
- **Expected**: User identity if logged in
- **Console**: `✅ SESSION ACTIVE` or `❌ NO ACTIVE SESSION`

### Logout
- **Request**: `Logout`
- **Expected**: Session cleared
- **Console**: `✅ LOGOUT SUCCESSFUL`

---

## 🔧 **Admin Tools**

### Get User by Phone
- **Request**: `🔧 5. Admin Tools` → `Get User by Phone`
- **Purpose**: Check if user exists
- **Console**: `✅ USER FOUND` or `❌ USER NOT FOUND`

### Get User Credentials
- **Request**: `Get User Credentials`
- **Purpose**: Debug credential types
- **Console**: Shows password and code credentials

---

## 🧪 **Testing Different Users**

### Test New Registration
1. **Change**: `phoneNumber` to new number (e.g., `+989203020411`)
2. **Run**: Complete registration workflow
3. **Result**: New user created

### Test Existing User Login
1. **Use**: Existing phone number (`+989203020409`)
2. **Run**: Code login workflow
3. **Result**: User authenticated via code

### Test Password Login
1. **Use**: User with password credentials
2. **Run**: Password login workflow
3. **Result**: Session created

---

## 🔍 **Troubleshooting**

### Common Issues

#### 1. "Invalid or already used code"
**Cause**: Using wrong verification code
**Solution**: 
```bash
# Get fresh code
docker-compose logs kratos | grep registration_code
# Update Postman variable with new code
```

#### 2. "Flow expired"
**Cause**: Flow ID expired (10 minutes)
**Solution**: Get new flow ID and restart

#### 3. "User not found"
**Cause**: Phone number not registered
**Solution**: Register user first

#### 4. "Could not find a strategy"
**Cause**: Wrong method or missing credentials
**Solution**: Check user has required credentials

### Debug Commands

```bash
# Check Kratos logs
docker-compose logs kratos

# Get latest verification code
docker-compose logs kratos | grep registration_code | tail -1

# Check service status
docker-compose ps

# Restart Kratos if needed
docker-compose restart kratos
```

---

## 📊 **Expected Results**

### Successful Registration
```json
{
  "identity": {
    "id": "d1d2522b-3ee2-4530-860c-6eb8e902c272",
    "traits": {
      "phone": "+989203020410"
    },
    "state": "active"
  }
}
```

### Successful Code Login
```json
{
  "state": "passed_challenge",
  "messages": [
    {
      "text": "An account with the same identifier exists already.",
      "type": "error"
    }
  ]
}
```

### Successful Password Login
```json
{
  "session_token": "ory_st_57A3tDkm4WSw65EZM9H4BR8qWpYow69C",
  "session": {
    "id": "ad746de8-16c7-4aab-b824-1babccd91cc9",
    "active": true,
    "identity": {
      "id": "55d8aa89-d9ae-426b-bd68-57c62b7c59fe",
      "traits": {
        "phone": "+989203020409"
      }
    }
  }
}
```

---

## 🎯 **Success Indicators**

### Console Messages
- ✅ **Registration**: `📱 SMS code sent successfully!` → `✅ NEW USER CREATED!`
- ✅ **Code Login**: `📱 Login code sent successfully!` → `🎉 CODE LOGIN SUCCESSFUL!`
- ✅ **Password Login**: `✅ PASSWORD LOGIN SUCCESSFUL!`
- ✅ **Session**: `✅ SESSION ACTIVE`
- ✅ **Logout**: `✅ LOGOUT SUCCESSFUL`

### Key Achievements
- 📱 **Mobile-Only Registration**: Phone + SMS verification
- 🔐 **Password-Less Login**: SMS code authentication
- 🔑 **Flexible Options**: Both code and password login
- 🎮 **Complete Session Management**: Full lifecycle
- 🔧 **Admin Tools**: Debugging and management

---

## 🚀 **Production Usage**

### For Production Deployment
1. **Update URLs**: Change `baseUrl` to production Kratos URL
2. **SMS Integration**: Replace SMTP with real SMS service
3. **Security**: Add SSL certificates
4. **Monitoring**: Set up health checks
5. **Rate Limiting**: Prevent abuse

### Performance Tips
- **Cache Flow IDs**: Reuse within 10-minute window
- **Batch Operations**: Process multiple users efficiently
- **Monitor Logs**: Track success/failure rates
- **Optimize SMS**: Use reliable SMS provider

---

## 📞 **Support**

### For Issues
1. **Check Services**: `docker-compose ps`
2. **Check Logs**: `docker-compose logs kratos`
3. **Check Codes**: `docker-compose logs kratos | grep registration_code`
4. **Check Database**: `docker-compose exec postgres psql -U ory -d ory_kratos -c "SELECT COUNT(*) FROM identities;"`

### For Help
- **Documentation**: `README.md`, `SETUP-GUIDE.md`
- **Test Results**: `FINAL-TEST-RESULTS.md`
- **Implementation**: `CODE-LOGIN-IMPLEMENTATION.md`

---

**🎉 THE BELDERCHIN IDP MOBILE AUTHENTICATION SYSTEM IS READY FOR TESTING!**

**Status**: ✅ **FULLY TESTED AND DOCUMENTED**

**Last Updated**: February 10, 2026
