# 📚 Belderchin IDP

## 🏗️ Organized Project Structure

This repository has been reorganized for better maintainability and clarity with modern controller-based architecture.

```
Belderchin-IDP/
├── 📁 docs/                           # All documentation
│   ├── README.md                      # Documentation overview
│   ├── SETUP-GUIDE.md                 # Setup instructions
│   ├── IMPLEMENTATION-SUMMARY.md      # Architecture overview
│   ├── COMPREHENSIVE-TEST-RESULTS.md  # Complete test results
│   ├── POSTMAN-GUIDE.md               # API testing guide
│   └── ...                            # Other documentation files
├── 📁 postman/                        # API testing collections
│   ├── README.md                      # Postman collections guide
│   └── Belderchin-IDP-Mobile-Auth-Collection.postman_collection.json
├── 📁 infra/                          # Infrastructure services
│   ├── auth-bridge/                   # Custom authentication service
│   │   ├── Controllers/                # API controllers
│   │   │   ├── HealthController.cs
│   │   │   ├── VerificationController.cs
│   │   │   └── SessionsController.cs
│   │   └── Program.cs                  # Service configuration
│   ├── sms-service/                   # SMS delivery service
│   │   ├── Controllers/                # API controllers
│   │   │   ├── HealthController.cs
│   │   │   └── SMSController.cs
│   │   └── Program.cs                  # Service configuration
│   ├── backend/                        # Learning platform backend
│   │   ├── Controllers/                # API controllers
│   │   │   ├── AuthController.cs
│   │   │   ├── CoursesController.cs
│   │   │   ├── UsersController.cs
│   │   │   ├── AdminController.cs
│   │   │   └── HealthController.cs
│   │   ├── Services/                   # Business logic
│   │   ├── Models/                     # Data models
│   │   └── Program.cs                  # Service configuration
│   ├── kratos/                        # Identity management
│   ├── hydra/                         # OAuth2/OIDC provider
│   ├── oathkeeper/                    # API gateway
│   └── docker-compose.yml             # Service orchestration
├── 📄 .env.example                    # Environment variables template
├── 📄 .gitignore                      # Git ignore rules
├── 📄 LICENSE                         # License information
└── 📄 Belderchin-IDP.sln              # Solution file
```

---

## 🚀 Quick Start

### 1. Documentation
📖 **Start here**: [docs/README.md](./docs/README.md)

### 2. API Testing
📮 **Postman Collections**: [postman/README.md](./postman/README.md)

### 3. Infrastructure
🏗️ **Services**: [infra/README.md](./infra/README.md)

---

## 📋 What's Inside

### 📚 Documentation (`docs/`)
- **Setup Guide**: Complete installation and configuration
- **Architecture**: System design and component overview
- **Testing**: Comprehensive test results and guides
- **API Guides**: Postman collection usage instructions

### 📮 API Testing (`postman/`)
- **Mobile Auth Collection**: Complete authentication workflows
- **Test Scenarios**: Registration, login, session management
- **Admin Tools**: User management and debugging utilities

### 🏗️ Infrastructure (`infra/`)
- **ORY Stack**: Kratos, Hydra, Oathkeeper
- **Custom Services**: Auth Bridge, SMS Service
- **Database**: PostgreSQL with initialization scripts
- **Networking**: Docker Compose configuration

---

## 🔧 Key Features

- ✅ **Mobile-First Authentication**: Phone number based registration/login
- ✅ **SMS Verification**: Code-based authentication with fallback options
- ✅ **OAuth2/OIDC**: Complete token generation and validation
- ✅ **API Security**: JWT-based protected endpoints with role-based authorization
- ✅ **Controller Architecture**: Clean separation of concerns with dedicated controllers
- ✅ **Swagger Documentation**: Interactive API documentation for all services
- ✅ **Learning Platform Backend**: Course management, user progress, and content delivery
- ✅ **Role-Based Access Control**: Admin and regular user permissions
- ✅ **Admin Tools**: User management and debugging utilities
- ✅ **Developer Friendly**: Comprehensive documentation and testing tools

---

## 📞 Support

For questions and issues:
1. 📖 Check [docs/](./docs/) for documentation
2. 🧪 Review [COMPREHENSIVE-TEST-RESULTS.md](./docs/COMPREHENSIVE-TEST-RESULTS.md)
3. 🔧 Follow [SETUP-GUIDE.md](./docs/SETUP-GUIDE.md)
4. 📮 Use [Postman collections](./postman/) for testing

---

## 🏆 Project Status

**✅ Production Ready** - All components tested and working with modern architecture

- ✅ Identity Management (Kratos)
- ✅ OAuth2 Provider (Hydra)  
- ✅ API Gateway (Oathkeeper)
- ✅ Custom Services (Auth Bridge, SMS)
- ✅ Learning Platform Backend (Courses, Users, Progress)
- ✅ Controller-Based Architecture
- ✅ Swagger Documentation
- ✅ Role-Based Authorization
- ✅ Complete Test Coverage

---

## 🌐 API Endpoints

### 📚 Learning Platform Backend (Port 8082)
- **Swagger UI**: `http://localhost:8082/swagger`
- **Health**: `http://localhost:8082/api/health`
- **Authentication**: `POST /api/auth/login`
- **Courses**: `GET /api/courses`, `POST /api/courses` (admin)
- **User Management**: `GET /api/users/me/profile`, `PUT /api/users/me/profile`
- **Progress**: `GET /api/users/me/progress`, `POST /api/users/me/progress/{courseId}`
- **Admin**: `POST /api/admin/seed`, `GET /api/admin/statistics`

### 🔐 Auth Bridge (Port 8080)
- **Swagger UI**: `http://localhost:8080/swagger`
- **Health**: `http://localhost:8080/api/health`
- **Verification**: `POST /api/verification/start`, `POST /api/verification/confirm`
- **2FA**: `POST /api/verification/2fa/start`, `POST /api/verification/2fa/confirm`
- **Sessions**: `GET /api/sessions/validate`, `GET /api/sessions/whoami`

### 📱 SMS Service (Port 8083)
- **Swagger UI**: `http://localhost:8083/swagger`
- **Health**: `http://localhost:8083/api/health`
- **SMS**: `POST /api/sms/send`, `GET /api/sms/status/{messageId}`

---

*Last updated: February 22, 2026*
