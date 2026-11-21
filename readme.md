# UserManagementAPI

An ASP.NET Core Web API built for **TechHive Solutions** to allow HR and IT teams to create, retrieve, update, and delete internal user records securely and reliably.

---

## Features

- **Full CRUD Endpoints**: Create, read (single & paginated list), update, and delete user records (`/api/users`).
- **Input Validation & Data Integrity**:
  - Data annotation validation for required fields, field lengths, and valid email formats (`400 Bad Request` via `ValidationProblem`).
  - Duplicate email prevention (`409 Conflict`).
  - Positive integer ID validation and safe lookups (`404 Not Found` for non-existent users).
- **Performance Optimization**:
  - Thread-safe in-memory storage (`ConcurrentDictionary<int, User>`).
  - Bounded pagination (`page`, `pageSize`) and case-insensitive filtering (`department`, `search`) on `GET /api/users`.
- **Custom Middleware Pipeline** (configured in exact order):
  1. **Exception-Handling Middleware** (`ExceptionHandlingMiddleware`): Catches unhandled exceptions and returns standardized JSON error responses (`{ "error": "Internal server error." }`).
  2. **Token Authentication Middleware** (`TokenAuthenticationMiddleware`): Validates `Authorization: Bearer <token>` or `X-Api-Key` headers and returns `401 Unauthorized` for missing or invalid tokens.
  3. **Request/Response Logging Middleware** (`RequestResponseLoggingMiddleware`): Logs the HTTP method, request path, final response status code, and execution duration.

---

## Project Structure

```text
UserManagementAPI/
├── UserManagementAPI.csproj
├── Program.cs
├── appsettings.json
├── Models/
│   └── User.cs
├── DTOs/
│   └── UserDtos.cs
├── Services/
│   ├── IUserService.cs
│   └── InMemoryUserService.cs
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   ├── TokenAuthenticationMiddleware.cs
│   └── RequestResponseLoggingMiddleware.cs
└── UserManagementAPI.http
```

---

## Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)

### Run the Application
```bash
dotnet restore
dotnet run
```
The API will start on the local port shown in the console output (for example, `http://localhost:5041`).

---

## Authentication

All `/api/users` endpoints require a valid token configured in `appsettings.json`:

- `techhive-hr-token-2026`
- `techhive-it-token-2026`

Include the token in your request header:
```http
Authorization: Bearer techhive-hr-token-2026
```

---

## API Endpoints

| Method | Endpoint | Description | Status Codes |
| :--- | :--- | :--- | :--- |
| `GET` | `/health` | Public health check endpoint | `200 OK` |
| `GET` | `/api/users` | Retrieve paginated/filtered users (`page`, `pageSize`, `department`, `search`) | `200 OK`, `401 Unauthorized` |
| `GET` | `/api/users/{id}` | Retrieve a specific user by ID | `200 OK`, `400 Bad Request`, `401 Unauthorized`, `404 Not Found` |
| `POST` | `/api/users` | Create a new user | `201 Created`, `400 Bad Request`, `401 Unauthorized`, `409 Conflict` |
| `PUT` | `/api/users/{id}` | Update an existing user | `200 OK`, `400 Bad Request`, `401 Unauthorized`, `404 Not Found`, `409 Conflict` |
| `DELETE` | `/api/users/{id}` | Delete a user by ID | `204 No Content`, `400 Bad Request`, `401 Unauthorized`, `404 Not Found` |
| `GET` | `/api/users/simulate-error` | Simulates an unhandled exception to test error-handling middleware | `500 Internal Server Error` |

---

## Example Requests

### Retrieve All Users
```bash
curl -i -H "Authorization: Bearer techhive-hr-token-2026" http://localhost:5041/api/users
```

### Create a User
```bash
curl -i -X POST http://localhost:5041/api/users \
  -H "Authorization: Bearer techhive-hr-token-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Nadia Mansour",
    "email": "nadia.mansour@techhive.io",
    "department": "HR",
    "role": "Recruiter"
  }'
```

---

## Debugging & Improvements Summary

During development and review with Copilot, the following issues were identified and resolved:
1. **Unvalidated User Input**: Added `DataAnnotations` (`[Required]`, `[EmailAddress]`, `[StringLength]`) and request validation helpers so empty names, malformed emails, or duplicate email addresses are rejected cleanly with `400` or `409` responses.
2. **Non-Existent User Lookups**: Replaced unsafe dictionary/list lookups with `TryGetValue` and positive ID checks so missing IDs return `404 Not Found` rather than throwing exceptions.
3. **Unhandled Exceptions**: Wrapped endpoint operations with structured error handling and added `ExceptionHandlingMiddleware` at the start of the pipeline to guarantee consistent JSON `500` responses without leaking stack traces.
4. **Query & Concurrency Bottlenecks**: Switched to `ConcurrentDictionary` with atomic ID increments (`Interlocked.Increment`) and added clamped pagination (`page`, `pageSize`) to `GET /api/users` to prevent unbounded memory/response payloads.
5. **Middleware Ordering**: Ordered the pipeline as **Error Handling $\rightarrow$ Token Authentication $\rightarrow$ Request/Response Logging** so unauthorized requests and unhandled exceptions are properly caught and logged with accurate final status codes.