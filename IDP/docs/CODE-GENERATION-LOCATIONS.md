# 📱 Code Generation Locations - Belderchin IDP

## 🎯 **Where Verification Codes Are Generated**

### 📊 **Code Generation Process**

#### 1. Registration Flow
```bash
# Step 1: Get registration flow
curl -s -X GET 'http://localhost:4433/self-service/registration/api'

# Step 2: Submit registration request
curl -X POST "http://localhost:4433/self-service/registration?flow=<id>" \
  -H "Content-Type: application/json" \
  -d '{"method": "code", "traits": {"phone": "+989203020410"}, "channel": "sms"}'

# Step 3: Code is generated automatically by Kratos
# Location: Kratos logs
docker-compose logs kratos | grep registration_code
```

#### 2. Code Login Flow
```bash
# Step 1: Get login flow (uses registration endpoint)
curl -s -X GET 'http://localhost:4433/self-service/registration/api'

# Step 2: Submit login code request
curl -X POST "http://localhost:4433/self-service/registration?flow=<id>" \
  -H "Content-Type: application/json" \
  -d '{"method": "code", "traits": {"phone": "+989203020409"}, "channel": "sms"}'

# Step 3: Code is generated automatically by Kratos
# Location: Kratos logs
docker-compose logs kratos | grep registration_code
```

---

## 🔍 **Code Generation Details**

### **What Happens Internally**

1. **User submits phone number** with `method: "code"`
2. **Kratos validates** phone format (E.164)
3. **Kratos generates** 6-digit numeric code
4. **Kratos logs** the code generation
5. **Kratos sends** via courier (SMTP simulation)
6. **User receives** code (in logs for testing)

### **Code Format**
- **Length**: 6 digits
- **Type**: Numeric only
- **Example**: `805364`, `526786`, `282120`
- **Expiration**: 10 minutes
- **Usage**: One-time use only

---

## 📝 **How to Get Verification Codes**

### **Method 1: Real-time Monitoring**
```bash
# Watch Kratos logs in real-time
docker-compose -f docker-compose-local.yml logs -f kratos | grep registration_code
```

### **Method 2: Get Latest Code**
```bash
# Get the most recent verification code
docker-compose logs kratos | grep registration_code | tail -1

# Extract just the 6-digit code
docker-compose logs kratos | grep registration_code | tail -1 | jq -r '.registration_code'

# Example output
805364
```

### **Method 3: Get All Recent Codes**
```bash
# Get last 5 verification codes
docker-compose logs kratos | grep registration_code | tail -5

# Get codes for specific flow
docker-compose logs kratos | grep "registration_flow_id:<FLOW_ID>"
```

---

## 📋 **Log Analysis Examples**

### **Successful Code Generation**
```json
{
  "audience": "audit",
  "level": "info", 
  "msg": "Sending out registration email with code.",
  "registration_code": "805364",
  "registration_code_id": "b5a4804d-6224-470a-91d3-6ae9753f8b18",
  "registration_flow_id": "4dd556b2-40e7-4dba-94f1-773cb58ce0c2",
  "service_name": "Ory Kratos",
  "service_version": "v25.4.0",
  "time": "2026-02-10T18:18:41.823984549Z"
}
```

### **Code Usage**
```json
{
  "audience": "audit",
  "level": "info",
  "msg": "Registration code used successfully",
  "registration_code_id": "b5a4804d-6224-470a-91d3-6ae9753f8b18",
  "registration_flow_id": "4dd556b2-40e7-4dba-94f1-773cb58ce0c2",
  "service_name": "Ory Kratos",
  "service_version": "v25.4.0",
  "time": "2026-02-10T18:19:02.671676053Z"
}
```

---

## 🎯 **API Endpoints That Generate Codes**

### **Registration Endpoint**
- **URL**: `POST /self-service/registration?flow=<id>`
- **Method**: `code`
- **Purpose**: New user registration
- **Code Location**: `registration_code` in logs

### **Login Endpoint** 
- **URL**: `POST /self-service/registration?flow=<id>`
- **Method**: `code`
- **Purpose**: Existing user authentication
- **Code Location**: `registration_code` in logs

### **Key Difference**
Both registration and code login use the **same endpoint** and generate codes in the **same way**:

- **Registration**: Creates new user + generates code
- **Code Login**: Authenticates existing user + generates code
- **Both**: Log to `registration_code` field in Kratos logs

---

## 🔧 **Configuration Settings**

### **Current Kratos Config**
```yaml
# Email fallback is DISABLED - only SMS used
courier:
  smtp:
    connection_uri: smtp://test:test@localhost:1025
  # No templates = default behavior (SMS only)
```

### **Code Generation Settings**
- **Enabled**: ✅ Yes (via `method: "code"`)
- **Channel**: `sms` only
- **Fallback**: No email fallback
- **Format**: 6-digit numeric
- **Expiration**: 10 minutes

---

## 📱 **Testing Workflow**

### **Complete Test Process**
```bash
# 1. Start monitoring (in separate terminal)
docker-compose -f docker-compose-local.yml logs -f kratos | grep registration_code

# 2. Run registration/login request
# (Use Postman or curl)

# 3. Copy the 6-digit code from logs
# Example: "registration_code":"805364" → use "805364"

# 4. Submit verification
curl -X POST "http://localhost:4433/self-service/registration?flow=<id>" \
  -H "Content-Type: application/json" \
  -d '{"method": "code", "traits": {"phone": "+989203020410"}, "code": "805364"}'
```

---

## 🎯 **Key Points**

### ✅ **No Email Fallback**
- **Configuration**: Email templates removed
- **Channel**: Only `sms` specified
- **Result**: 200 OK responses without email validation errors

### ✅ **Single Code Source**
- **Registration Codes**: `registration_code` in logs
- **Login Codes**: `registration_code` in logs
- **Command**: `docker-compose logs kratos | grep registration_code`

### ✅ **Consistent Format**
- **Always 6 digits**: `805364`, `526786`, `282120`
- **Always numeric**: No letters or special characters
- **Always logged**: Full audit trail available

---

## 🚨 **Important Notes**

### **Code Retrieval**
1. **Always check logs** after submitting phone number
2. **Use the latest code** from the logs
3. **Codes expire in 10 minutes** - use quickly
4. **Each code is single-use** - can't reuse

### **API Responses**
- **200 OK**: Code generated successfully
- **400 Bad Request**: Invalid phone format or missing data
- **410 Gone**: Flow expired - get new flow ID

### **Debugging**
```bash
# Check if Kratos is running
curl http://localhost:4433/health/alive

# Check recent code generation
docker-compose logs kratos | grep registration_code --since=5m

# Monitor code usage
docker-compose logs kratos | grep "code used successfully"
```

---

## 📞 **Support**

### **For Code Issues**
1. **Check logs**: `docker-compose logs kratos | grep registration_code`
2. **Check flow**: Make sure flow ID is recent (< 10 minutes)
3. **Check format**: Phone must be E.164 format (+country+number)
4. **Check method**: Must use `method: "code"` and `channel: "sms"`

### **For Configuration Issues**
1. **Check Kratos config**: `infra/kratos/kratos.yml`
2. **Check services**: `docker-compose ps`
3. **Check logs**: `docker-compose logs kratos`
4. **Restart if needed**: `docker-compose restart kratos`

---

**📱 ALL VERIFICATION CODES ARE GENERATED IN KRAKOS LOGS AND ACCESSIBLE VIA:**

```bash
docker-compose logs kratos | grep registration_code
```

**🎯 NO EMAIL FALLBACK - SMS ONLY AUTHENTICATION ENABLED**

**Last Updated**: February 10, 2026
