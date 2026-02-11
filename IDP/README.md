# 📚 Belderchin IDP

## 🏗️ Organized Project Structure

This repository has been reorganized for better maintainability and clarity.

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
│   ├── sms-service/                   # SMS delivery service
│   ├── kratos/                        # Identity management
│   ├── hydra/                         # OAuth2/OIDC provider
│   ├── oathkeeper/                    # API gateway
│   └── docker-compose.yml             # Service orchestration
├── 📁 sample-api/                     # Example protected API
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
- ✅ **API Security**: JWT-based protected endpoints
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

**✅ Production Ready** - All components tested and working

- ✅ Identity Management (Kratos)
- ✅ OAuth2 Provider (Hydra)  
- ✅ API Gateway (Oathkeeper)
- ✅ Custom Services (Auth Bridge, SMS)
- ✅ Sample API Integration
- ✅ Complete Test Coverage

---

*Last reorganized: February 10, 2026*
