# Hikma Abroad API

REST API backend for the Hikma Consult study-abroad platform built with **ASP.NET Core 9 Web API** and **MongoDB**.

## Quick Start

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [MongoDB](https://www.mongodb.com/try/download/community) (local) or MongoDB Atlas

### Run Locally
```bash
# Clone and navigate
cd HikmaAbroad

# Restore packages
dotnet restore

# Run (MongoDB must be running on localhost:27017)
dotnet run
```

Open **Swagger UI**: [http://localhost:5076/swagger](http://localhost:5076/swagger)

### Run with Docker
```bash
docker-compose up --build
```
API available at `http://localhost:8080/swagger`

---

## Environment Variables

| Variable | Description | Default |
|---|---|---|
| `MONGO_URI` | MongoDB connection string | `mongodb://localhost:27017` |
| `JWT_SECRET` | JWT signing secret (min 32 chars) | Set in appsettings.json |
| `S3_ACCESS_KEY` | S3/Spaces access key | - |
| `S3_SECRET_KEY` | S3/Spaces secret key | - |
| `SMTP_HOST` | SMTP server host | - |
| `SMTP_USER` | SMTP username | - |
| `SMTP_PASSWORD` | SMTP password | - |

---

## Seed Data

On first run, the API automatically seeds:
- **Admin user**: `admin@hikmaconsult.com` / `Admin@123`
- **Site settings** with hero, navbar, footer
- **4 destinations**: Malaysia, UK, Canada, Australia
- **4 services**: University Application, Visa Processing, Scholarship Guidance, Pre-Departure Briefing
- **2 counsellors**: Riyad Ahmed, Fatima Khan
- **2 student experiences**: Ahsan Rahman, Nadia Islam
- **About page** with sample content

---

## API Endpoints

Base URL: `/api/v1`

All responses follow the shape:
```json
{ "success": true, "data": { ... }, "error": null }
```

### Public Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/v1/home` | Aggregated home page data |
| GET | `/api/v1/destinations` | Active destinations |
| GET | `/api/v1/services` | Active services |
| GET | `/api/v1/counsellors` | All counsellors |
| GET | `/api/v1/experiences` | Student experiences |
| GET | `/api/v1/pages/{key}` | Get page by key (e.g., "about") |
| POST | `/api/v1/students` | Submit or save draft |
| GET | `/api/v1/students/draft/{draftToken}` | Get draft by token |

### Admin Endpoints (JWT Required)

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/v1/auth/login` | Login, returns JWT |
| GET | `/api/v1/settings` | Get site settings |
| PUT | `/api/v1/settings` | Update site settings |
| GET/POST/PUT/DELETE | `/api/v1/destinations/*` | Destinations CRUD |
| GET/POST/PUT/DELETE | `/api/v1/experiences/*` | Experiences CRUD |
| GET/POST/PUT/DELETE | `/api/v1/services/*` | Services CRUD |
| GET/POST/PUT/DELETE | `/api/v1/counsellors/*` | Counsellors CRUD |
| PUT | `/api/v1/pages/{key}` | Create/update page |
| GET | `/api/v1/students` | List students (paged) |
| GET | `/api/v1/students/{id}` | Get student detail |
| PUT | `/api/v1/students/{id}/mark-contacted` | Mark student contacted |
| DELETE | `/api/v1/students/drafts/cleanup` | Cleanup old drafts |
| GET | `/api/v1/export/students` | CSV export |
| POST | `/api/v1/upload` | Upload image file |

---

## Frontend Developer Guide

### Authentication Flow
1. POST `/api/v1/auth/login` with `{ "email": "...", "password": "..." }`
2. Store the returned `token` in localStorage/memory
3. Include in all admin requests: `Authorization: Bearer {token}`

### Draft Token Flow
1. Generate a unique token client-side (e.g., `crypto.randomUUID()`) and store in `localStorage`
2. On form field change, POST to `/api/v1/students`:
   ```json
   {
     "name": "Araf",
     "email": "a@gmail.com",
     "phone": "+8801XXXXXXXX",
     "fromCountry": "Bangladesh",
     "lastAcademicLevel": "HSC",
     "draftToken": "your-local-token",
     "isSubmitted": false
   }
   ```
3. Backend returns the saved draft with the `draftToken`
4. On page reload, GET `/api/v1/students/draft/{draftToken}` to restore form data
5. On final submit, POST with `"isSubmitted": true`
6. After successful submission, clear the `draftToken` from localStorage

### Upload Flow
1. Use `multipart/form-data` POST to `/api/v1/upload` with JWT
2. Field name: `file`
3. Allowed types: `image/jpeg`, `image/png`, `image/webp`
4. Max size: 5MB
5. Response includes `url` field — use this URL in hero banners, counsellor photos, etc.

### Example: Submit Student
```javascript
const response = await fetch('/api/v1/students', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    name: 'Araf',
    email: 'a@gmail.com',
    phone: '+8801XXXXXXXX',
    fromCountry: 'Bangladesh',
    lastAcademicLevel: 'HSC',
    draftToken: localStorage.getItem('draftToken'),
    isSubmitted: true
  })
});
const result = await response.json();
// result.success === true
// result.data._id, result.data.draftToken, etc.
```

---

## Project Structure

```
HikmaAbroad/
├── Configuration/       # Settings POCOs
├── Controllers/         # API controllers
├── Data/                # MongoDB context
├── Helpers/             # Validation utilities
├── Models/
│   ├── Entities/        # MongoDB document models
│   ├── DTOs/            # Request/response DTOs
│   └── ApiResponse.cs   # Standard response wrapper
├── Services/            # Business logic services
├── wwwroot/uploads/     # Local file uploads
├── Program.cs           # App entry point & DI setup
├── appsettings.json     # Configuration
├── Dockerfile
├── docker-compose.yml
└── README.md
```

---

## Configuration

### Storage Modes
In `appsettings.json`, set `Storage:Mode`:
- `"Local"` — files saved to `wwwroot/uploads/`, served as static files
- `"S3"` — files uploaded to S3-compatible storage (AWS S3, DigitalOcean Spaces)

### Rate Limiting
The `POST /api/v1/students` endpoint is rate-limited to **10 requests per minute per IP**.

### CORS
Allowed origins configured in `appsettings.json` under `Cors:AllowedOrigins`.

### Email Notifications
Set `Email:Enabled` to `true` and configure SMTP settings to receive email notifications when students submit applications.

---

## Swagger / OpenAPI

- **Swagger UI**: `/swagger`
- **OpenAPI JSON**: `/swagger/v1/swagger.json`

The OpenAPI JSON can be imported into Postman as a collection.
