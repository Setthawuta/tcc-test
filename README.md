# Auth System — .NET 10 + Angular 21 + PostgreSQL

ระบบ Authentication ตาม requirement IT 02-1 (Login) / IT 02-2 (Register) / IT 02-3 (Welcome)
Backend ใช้ Clean Architecture, Frontend ใช้ Angular 21 Signals

## Quick Start

```bash
docker-compose up -d --build
```

| Service | URL |
|---|---|
| Frontend | http://localhost:4200 |
| Backend API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |
| PostgreSQL | localhost:5432 |

## Tech Stack

**Backend:** .NET 10, EF Core 10 + Npgsql, BCrypt (work factor 12), JWT HS256, FluentValidation, Serilog
**Frontend:** Angular 21 standalone + Signals, Reactive Forms, Tailwind CSS v3, functional Interceptors/Guards
**Infra:** PostgreSQL 16, Docker multi-stage, Nginx reverse-proxy

## API Endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | — | Register new user |
| POST | `/api/auth/login` | — | Login, returns access + refresh token |
| POST | `/api/auth/refresh` | — | Rotate refresh token, get new access token |
| POST | `/api/auth/logout` | — | Revoke refresh token |
| GET | `/api/auth/me` | JWT | Current user info |
| GET | `/health` | — | Health check |

## Token Flow

- **Access token:** JWT HS256, อายุ 60 นาที (configurable ผ่าน `Jwt__ExpirationMinutes`)
- **Refresh token:** random opaque string, อายุ 7 วัน, เก็บเป็น SHA-256 hash ใน DB (ไม่เก็บ raw)
- **Rotation:** ทุกครั้งที่ refresh จะออก token ใหม่ + revoke ตัวเก่า (`replaced_by_token_id` ชี้ไปตัวใหม่)
- **Reuse detection:** ถ้ามีการใช้ refresh token ที่ถูก revoke แล้ว → revoke ทุก token ของ user ทันที
- **Frontend:** `auth.interceptor.ts` refresh อัตโนมัติเมื่อเจอ 401, queue concurrent requests เพื่อกัน race condition

## Database Schema

```sql
CREATE TABLE users (
    id                  UUID         PRIMARY KEY,
    username            VARCHAR(50)  NOT NULL,
    password_hash       VARCHAR(60)  NOT NULL,            -- BCrypt
    created_at          TIMESTAMPTZ  NOT NULL,
    updated_at          TIMESTAMPTZ  NOT NULL,
    last_login_at       TIMESTAMPTZ  NULL,
    failed_login_count  INT          NOT NULL DEFAULT 0,
    locked_until        TIMESTAMPTZ  NULL
);
CREATE UNIQUE INDEX idx_users_username_lower ON users (LOWER(username));

CREATE TABLE refresh_tokens (
    id                    UUID         PRIMARY KEY,
    user_id               UUID         NOT NULL,
    token_hash            VARCHAR(128) NOT NULL,          -- SHA-256
    expires_at            TIMESTAMPTZ  NOT NULL,
    created_at            TIMESTAMPTZ  NOT NULL,
    revoked_at            TIMESTAMPTZ  NULL,
    replaced_by_token_id  UUID         NULL
);
CREATE UNIQUE INDEX idx_refresh_tokens_token_hash ON refresh_tokens (token_hash);
CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens (user_id);
```

## Environment Variables

ดู `.env` ที่ root — ที่สำคัญ:

| Variable | Purpose |
|---|---|
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | Database credentials |
| `JWT_SECRET` | JWT signing key (ต้องยาว 32+ ตัวอักษร) |
| `JWT_ISSUER` / `JWT_AUDIENCE` | JWT claims |
| `JWT_EXPIRATION_MINUTES` | Access token lifetime (default 60) |
| `FRONTEND_ORIGIN` | CORS allowed origin |

สร้าง secret ด้วย: `openssl rand -base64 48`

## Security

- **Password:** BCrypt work factor 12, constant-time verify
- **JWT:** HS256, full validation (issuer/audience/lifetime, `ClockSkew = Zero`)
- **Refresh token:** opaque + SHA-256 hashed at rest, rotation + reuse detection
- **Account lockout:** track `failed_login_count` / `locked_until` ใน users table
- **Login:** generic error message (no user enumeration)

## Development (Without Docker)

```bash
# Backend
cd auth-system/backend
dotnet ef database update --project src/AuthSystem.Infrastructure --startup-project src/AuthSystem.Api
dotnet run --project src/AuthSystem.Api

# Frontend
cd auth-system/frontend
npm install && npm start
```

## License
MIT
