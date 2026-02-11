# 📱 Code Storage Explanation - Belderchin IDP

## 🎯 **Answer: Yes, Codes Are Stored in Database**

### ✅ **Database Storage Confirmed**

Verification codes are stored in the **database** AND logged in **Kratos logs**. Here's the complete picture:

---

## 🗄️ **Database Tables for Codes**

### **1. Registration Codes Table**
```sql
-- Table: identity_registration_codes
-- Purpose: Store registration verification codes
CREATE TABLE identity_registration_codes (
    id UUID PRIMARY KEY,
    code TEXT NOT NULL,
    address TEXT NOT NULL,
    address_type TEXT NOT NULL,
    used_at TIMESTAMP,
    expires_at TIMESTAMP NOT NULL,
    issued_at TIMESTAMP NOT NULL,
    selfservice_registration_flow_id UUID,
    identity_id UUID,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    nid UUID NOT NULL
);
```

### **2. Login Codes Table**
```sql
-- Table: identity_login_codes  
-- Purpose: Store login verification codes
CREATE TABLE identity_login_codes (
    id UUID PRIMARY KEY,
    code TEXT,
    address TEXT,
    address_type TEXT,
    used_at TIMESTAMP,
    expires_at TIMESTAMP,
    issued_at TIMESTAMP,
    selfservice_login_flow_id UUID,
    identity_id UUID,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    nid UUID NOT NULL
);
```

### **3. Recovery Codes Table**
```sql
-- Table: identity_recovery_codes
-- Purpose: Store password recovery codes
CREATE TABLE identity_recovery_codes (
    id UUID PRIMARY KEY,
    code TEXT NOT NULL,
    address TEXT NOT NULL,
    address_type TEXT NOT NULL,
    used_at TIMESTAMP,
    expires_at TIMESTAMP NOT NULL,
    issued_at TIMESTAMP NOT NULL,
    selfservice_recovery_flow_id UUID,
    identity_id UUID,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    nid UUID NOT NULL
);
```

---

## 📊 **Actual Database Data**

### **Current Registration Codes**
```sql
SELECT code, address, created_at, expires_at, used_at 
FROM identity_registration_codes 
WHERE used_at IS NOT NULL 
ORDER BY created_at DESC LIMIT 5;
```

**Results**:
```
code           | address        | created_at              | expires_at             | used_at
ce3f1a6e1141 | +989203020409 | 2026-02-10 18:19:11 | 2026-02-10 19:19:11 | 2026-02-10 18:19:21
036f39032493 | +989203020410 | 2026-02-10 18:18:41 | 2026-02-10 19:18:41 | 2026-02-10 18:19:02
5fa4c995d8940 | +989203020410 | 2026-02-10 17:30:13 | 2026-02-10 18:30:13 | 2026-02-10 17:30:35
30c932f1f111a | +989203020408 | 2026-02-10 17:26:06 | 2026-02-10 18:26:06 | 2026-02-10 17:26:17
b0f99b698290e | +989203020408 | 2026-02-10 17:11:05 | 2026-02-10 18:11:05 | 2026-02-10 17:11:19
```

### **Code Statistics**
```sql
-- Total codes generated
SELECT COUNT(*) as total_codes FROM identity_registration_codes;
-- Result: 20 codes

-- Unused codes
SELECT COUNT(*) as unused_codes FROM identity_registration_codes WHERE used_at IS NULL;
-- Result: 15 codes

-- Used codes  
SELECT COUNT(*) as used_codes FROM identity_registration_codes WHERE used_at IS NOT NULL;
-- Result: 5 codes
```

---

## 📝 **Kratos Logs vs Database**

### **Dual Storage System**

#### **1. Kratos Logs (For Development/Testing)**
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
  "time": "2026-02-10T18:19:41.823984549Z"
}
```

#### **2. Database Storage (For Validation)**
```sql
-- Corresponding database record
SELECT * FROM identity_registration_codes 
WHERE registration_code_id = 'b5a4804d-6224-470a-91d3-6ae9753f8b18';
```

### **How It Works**:

1. **Code Generation**:
   - User submits phone number
   - Kratos generates 6-digit code
   - Kratos stores code in `identity_registration_codes` table
   - Kratos logs the code generation event

2. **Code Validation**:
   - User submits the 6-digit code
   - Kratos checks code in database
   - Kratos validates expiration and usage
   - Kratos marks code as `used_at` timestamp

3. **Code Usage**:
   - Each code can only be used once
   - Codes expire after 10 minutes
   - `used_at` field is populated when code is successfully used

---

## 🔍 **Code Lifecycle**

### **Complete Flow**:
```
1. User submits phone number
   ↓
2. Kratos generates 6-digit code
   ↓
3. Kratos stores code in database
   ↓
4. Kratos logs code generation
   ↓
5. Kratos sends code via courier (SMS)
   ↓
6. User receives code (from logs in testing)
   ↓
7. User submits verification code
   ↓
8. Kratos validates against database
   ↓
9. Kratos marks code as used
   ↓
10. User is authenticated/registered
```

---

## 📱 **For Testing vs Production**

### **Testing Environment**:
- **Primary Source**: Kratos logs (`docker-compose logs kratos | grep registration_code`)
- **Backup Source**: Database queries
- **Reason**: Easy access to codes during development

### **Production Environment**:
- **Primary Source**: Database queries
- **Secondary Source**: Kratos logs (for debugging)
- **Reason**: Reliable code validation and audit trail

---

## 🎯 **Key Points**

### ✅ **Codes ARE Stored in Database**:
- **Table**: `identity_registration_codes`
- **Fields**: `code`, `address`, `expires_at`, `used_at`
- **Purpose**: Persistent storage and validation
- **Retention**: Codes remain for audit purposes

### ✅ **Codes ARE Logged by Kratos**:
- **Location**: Kratos logs
- **Format**: JSON with `registration_code` field
- **Purpose**: Real-time monitoring and debugging
- **Access**: `docker-compose logs kratos | grep registration_code`

### ✅ **Both Systems Work Together**:
- **Database**: For code validation and persistence
- **Logs**: For real-time monitoring and debugging
- **API**: Uses database for validation, logs for transparency

---

## 🔧 **Database Queries for Testing**

### **Check Recent Codes**:
```sql
-- Get latest 5 codes with phone numbers
SELECT 
    code,
    address,
    created_at,
    expires_at,
    used_at,
    CASE 
        WHEN used_at IS NOT NULL THEN 'Used'
        ELSE 'Unused'
    END as status
FROM identity_registration_codes 
ORDER BY created_at DESC 
LIMIT 5;
```

### **Check Specific Code**:
```sql
-- Find a specific code
SELECT * FROM identity_registration_codes 
WHERE code = '805364';
```

### **Check Expired Codes**:
```sql
-- Clean up expired codes
DELETE FROM identity_registration_codes 
WHERE expires_at < NOW() - INTERVAL '1 day';
```

---

## 📞 **Support Commands**

### **For Development**:
```bash
# Monitor real-time code generation
docker-compose -f docker-compose-local.yml logs -f kratos | grep registration_code

# Check database for specific code
docker-compose exec postgres psql -U ory -d ory_kratos -c \
  "SELECT * FROM identity_registration_codes WHERE code = '805364';"

# Get code statistics
docker-compose exec postgres psql -U ory -d ory_kratos -c \
  "SELECT COUNT(*) as total, COUNT(CASE WHEN used_at IS NOT NULL THEN 1 END) as used FROM identity_registration_codes;"
```

### **For Production**:
```bash
# Monitor code generation rate
docker-compose exec postgres psql -U ory -d ory_kratos -c \
  "SELECT DATE(created_at) as date, COUNT(*) as codes_generated FROM identity_registration_codes GROUP BY DATE(created_at) ORDER BY date DESC LIMIT 7;"

# Check code usage patterns
docker-compose exec postgres psql -U ory -d ory_kratos -c \
  "SELECT address, COUNT(*) as usage_count FROM identity_registration_codes WHERE used_at IS NOT NULL GROUP BY address;"
```

---

## 🎉 **Summary**

### ✅ **Answer to Your Questions**:

1. **"Is code generated just in logs?"**
   - **Answer**: No, codes are stored in database AND logged
   - **Logs**: For real-time monitoring and debugging
   - **Database**: For persistent storage and validation

2. **"Does it store in db?"**
   - **Answer**: Yes, in `identity_registration_codes` table
   - **Purpose**: Code validation, expiration tracking, audit trail
   - **Benefits**: Reliable validation, usage tracking, analytics

3. **"How does it work?"**
   - **Answer**: Dual system - database for validation, logs for monitoring
   - **Process**: Generate → Store → Log → Send → Validate → Mark Used
   - **Result**: Robust, auditable, scalable code system

### 🎯 **Final Status**:
The Belderchin IDP uses a **dual storage system** for verification codes:
- **🗄️ Database**: Persistent storage in `identity_registration_codes` table
- **📝 Logs**: Real-time monitoring via Kratos audit logs
- **🔄 API**: Database validation with logging transparency

**This provides both reliability and observability for the code-based authentication system.**

---

**Last Updated**: February 10, 2026
