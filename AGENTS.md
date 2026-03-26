# AGENTS.md

## What this project is
- `MiniApp` is a .NET 10 Telegram Mini App backend + Razor admin panel for a lotto flow.
- Runtime composition is centralized in `Program.cs`: service registration, middleware, endpoint mapping, Telegram bot mode, and startup migrations.
- Core domains live in `Data/` (`MiniAppUser`, `Ticket`, `Draw`) with EF Core + PostgreSQL.

## Architecture and data flow
- Mini App client (`Pages/app.cshtml` + `wwwroot/js/miniapp.js`) calls Minimal APIs under `/api/*`.
- Telegram auth is server-validated (`Features/Auth/TelegramAuthEndpoints.cs`) using `TelegramLogin/TelegramInitDataValidator.cs`.
- Ticket purchase (`/api/tickets/purchase`) creates tickets for `nextDrawId = max(draws)+1`, so tickets may exist before a draw is created (`Features/Tickets/TicketsEndpoints.cs`).
- Draw creation is admin-only (`/api/admin/draws/start` and `Pages/Admin/Draws.cshtml.cs`), generating sequential draw IDs.
- Timeline (`/api/timeline`) merges user tickets + existing draws + synthetic upcoming draw group (`Features/Timeline/TimelineEndpoints.cs`).

## Key conventions in this repo
- Endpoint style: feature extension methods (`MapXEndpoints`) invoked from `Program.cs`.
- DTOs are `record` types colocated in small `*Models.cs` files (example: `Features/Draws/DrawsModels.cs`).
- Use `DateTimeOffset.UtcNow` for persisted timestamps.
- EF reads use `AsNoTracking()` in list/query pages and APIs.
- Dev-only auth shortcut exists in ticket/timeline APIs via header `X-Dev-TelegramUserId` when `ASPNETCORE_ENVIRONMENT=Development`.
- SignalR is removed; timeline updates are polling-based every ~4s (`wwwroot/js/miniapp.js`, `Features/Draws/DrawsHub.cs`).

## Configuration that blocks startup
- `BotToken` must be set; app throws at startup if missing/placeholder (`Program.cs`).
- `Database:ConnectionString` is validated on start (`Data/DatabaseModule.cs`).
- `BotMode` is `Polling` or `Webhook`; polling hosted service only runs in polling mode.
- `Database:AutoMigrate` defaults to `true` in Development, `false` otherwise unless overridden.
- Admin cookie auth uses `Admin:Username` / `Admin:Password` (`Admin/AdminAuth.cs`).

## Developer workflows
- Local DB only: `docker compose up -d` (from `docker-compose.yml`).
- Full stack (app + DB): `docker compose -f docker-compose.app.yml up -d --build`.
- Local run profile opens admin by default (`Properties/launchSettings.json`).
- EF migration flow (from README): `dotnet ef migrations add <Name>` then `dotnet ef database update`.
- Webhook ops endpoints exist for manual switching/debug: `/bot/setWebhook`, `/bot/webhookInfo`, `/bot/deleteWebhook`.

## Admin/ops guardrails
- Admin pages require cookie policy `AdminOnly` (except login/logout) under `Pages/Admin/*`.
- `Pages/Admin/DangerZone.cshtml.cs` drops and recreates `public` schema directly; use carefully.
- Deleting users cascades to tickets (configured in `Data/AppDbContext.cs`).

## When making changes
- New DB fields/entities: update `Data/*`, `AppDbContext`, then create migration in `Migrations/`.
- New API features: add `Features/<Area>/...Endpoints.cs`, DTOs in `*Models.cs`, and map in `Program.cs`.
- Changes to auth/initData handling must be mirrored across auth, tickets, and timeline endpoints.
- If changing draw/ticket semantics, verify both purchase endpoint and timeline grouping logic together.

## Deployment
- app is running on DigitalOcean
- project uses GitHub Actions for CD via `.github/workflows/deploy-droplet.yml`; on push to `main`/`master` it SSHes to the droplet, pulls the repo, and runs `docker compose -f docker-compose.app.yml up -d --build` on the server.
- Active SDK/runtime pins are `.NET 10` in `global.json`, `MiniApp.csproj`, `Dockerfile`, and `README.md`; `8.0.11` strings in `Migrations/*.Designer.cs` and `AppDbContextModelSnapshot.cs` are legacy EF migration metadata, not current SDK/runtime pins.
