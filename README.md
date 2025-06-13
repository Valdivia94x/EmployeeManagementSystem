# 👨‍💼 Employee Management System

> 🚧 Project under development – things may change frequently!

This project is a RESTful Web API built with ASP.NET Core for securely managing employee data. It enforces role-based access control using JWT authentication and provides clear logging and comprehensive test coverage.

## ✨ Features:

🔐 JWT Authentication & Role-based Authorization (Admin, HR, Employee)  
📝 CRUD operations for Employee records  
🧱 Service & Repository pattern  
🧪 Unit Testing with xUnit  
📋 Structured Logging using ILogger  
📖 Swagger UI for API documentation and testing  

## 🔒 Access Control:
- Admins and HR can access all employee data.  
- Regular employees can only view or modify their own records.  
- Unauthorized actions return proper HTTP status codes (403 or 401).  

---

This solution is designed with clean architecture principles and is ready for production-level enhancements.