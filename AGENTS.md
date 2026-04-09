# AGENTS.md

## What this project is
- `MiniApp` is a .NET 10 Telegram Mini App backend + Razor admin panel for a lotto flow.
- Runtime composition is centralized in `Program.cs`: service registration, middleware, endpoint mapping, Telegram bot mode, and startup migrations.
- Core domains live in `Data/` (`MiniAppUser`, `Ticket`, `Draw`, wallet + payments entities) with EF Core + PostgreSQL.

## Architecture and data flow
- Mini App client (`Pages/app.cshtml` + `wwwroot/js/miniapp.js`) calls Minimal APIs under `/api/*`.
- Mini App bootstrap loads localization first via `/api/localization/bootstrap`, then authenticates with `/api/auth/telegram`, then polls timeline/wallet APIs.
- Telegram auth is server-validated (`Features/Auth/TelegramAuthEndpoints.cs`) using `TelegramLogin/TelegramInitDataValidator.cs`.
- Ticket purchase (`/api/tickets/purchase`) targets the current `DrawState.Active` draw and is rejected when there is no active draw (`Features/Tickets/TicketsEndpoints.cs`).
- Draw creation is admin-only (`/api/admin/draws/start` and `Pages/Admin/Draws.cshtml.cs`), generating sequential draw IDs.
- Timeline (`/api/timeline`) returns a featured draw (active, otherwise latest relevant finished draw) plus current draw tickets and grouped history (`Features/Timeline/TimelineEndpoints.cs`).
- Crypto deposit flow uses BTCPay Greenfield API: user endpoints create/poll deposit intents (`/api/payments/deposits/create`, `/api/payments/deposits/status`) and webhook ingestion happens at `/api/webhooks/btcpay` (`Features/Payments/*`).
- Webhook processing is DB-backed and idempotent: duplicate delivery IDs are deduped via `payment_webhook_events` unique index, and wallet crediting is one-time per invoice reference.

## Key conventions in this repo
- Endpoint style: feature extension methods (`MapXEndpoints`) invoked from `Program.cs`.
- DTOs are `record` types colocated in small `*Models.cs` files (example: `Features/Draws/DrawsModels.cs`).
- Use `DateTimeOffset.UtcNow` for persisted timestamps.
- EF reads use `AsNoTracking()` in list/query pages and APIs.
- Local debug identity is resolved via `LocalDebugMode.TryGetDebugTelegramUserId(...)`; explicit `initData=local-debug` / `X-Dev-TelegramUserId` triggering is handled in auth + localization bootstrap endpoints.
- SignalR is removed; timeline updates are polling-based every ~4s (`wwwroot/js/miniapp.js`, `Features/Draws/DrawsHub.cs`).

## Configuration that blocks startup
- `BotToken` must be set when Telegram mode is enabled (non-local-debug); startup allows missing token only in Development local debug mode (`Program.cs`).
- `Database:ConnectionString` is validated on start unless Development local debug is enabled with in-memory DB fallback (`Data/DatabaseModule.cs`).
- `BotMode` is `Polling` or `Webhook`; polling hosted service only runs in polling mode.
- `Database:AutoMigrate` defaults to `true` in Development, `false` otherwise unless overridden.
- Admin cookie auth uses `Admin:Username` / `Admin:Password` (`Admin/AdminAuth.cs`).
- If `Payments:Enabled=true`, options validation requires `Payments:BtcPay:BaseUrl`, `Payments:BtcPay:StoreId`, `Payments:BtcPay:ApiKey`, `Payments:BtcPay:WithdrawalsPullPaymentId`, and `Payments:BtcPay:WithdrawalsPaymentMethod`.

## Developer workflows
- Local DB only: `docker compose up -d` (from `docker-compose.yml`).
- Full stack (app + DB): `docker compose -f docker-compose.app.yml up -d --build`.
- `Properties/launchSettings.json` includes a dedicated `local-debug` profile (`launchUrl: local-debug`) plus `http`/`https` profiles that open admin.
- EF migration flow (from README): `dotnet ef migrations add <Name>` then `dotnet ef database update`.
- Webhook ops endpoints exist for manual switching/debug: `/bot/setWebhook`, `/bot/webhookInfo`, `/bot/deleteWebhook`.

## Admin/ops guardrails
- Admin pages require cookie policy `AdminOnly` (except login/logout) under `Pages/Admin/*`.
- `Pages/Admin/DangerZone.cshtml.cs` drops and recreates `public` schema directly; use carefully.
- Deleting users cascades to tickets (configured in `Data/AppDbContext.cs`).

## When making changes
- New DB fields/entities: update `Data/*`, `AppDbContext`, then create migration in `Migrations/`.
- New API features: add `Features/<Area>/...Endpoints.cs`, DTOs in `*Models.cs`, and map in `Program.cs`.
- Changes to auth/initData handling must be mirrored across auth, localization bootstrap, tickets, timeline, wallet, and payments endpoints.
- If changing draw/ticket semantics, verify active-draw purchase rules and timeline featured/history grouping together.
- If changing payments: update `Features/Payments/*`, keep `AppDbContext` mappings aligned with migrations, and verify BTCPay webhook processing still preserves idempotent crediting.
- Preserve webhook signature handling (`BTCPay-Sig`) and audit trail states in `PaymentWebhookEvent` (`Received`/`Processed`/`Ignored`/`Failed`).

## Localization requirements (mandatory)
- All new user-facing UI text (Mini App, admin Razor pages, and bot messages) must use the localization system; avoid hardcoded strings unless they are transient debug-only diagnostics.
- When adding/changing UI or bot text, add/update keys in the localization table defaults and verify admin can edit those keys from the localization admin page.
- Keep locale coverage aligned across all three languages: `en`, `ru`, and `uz`.
- If you add a new page/component with text, include localization keys at implementation time (not as a follow-up).

## Local debug mode maintenance (important)
- Local debug mode is Development-only and must never affect production behavior; keep guards aligned in `Program.cs`, `appsettings.json`, and `docker-compose.app.yml`.
- Debug data is intentionally synthetic and seeded in `Features/Auth/LocalDebugSeed.cs`; if you change draw/ticket/user semantics, update seed logic so `/app` still has: 1 active draw, 1 upcoming draw, at least 2 finished draws with tickets for the debug user.
- Debug identity resolution lives in `Features/Auth/LocalDebugMode.cs`; if auth flow changes, keep auth/localization/tickets/timeline/wallet/payments endpoints consistent with the same debug user contract.
- Client bootstrap in `wwwroot/js/miniapp.js` must continue to fall back to local debug when Telegram `initData` is missing (normal browser), call localization bootstrap, then call auth before timeline/purchase APIs.
- Admin debug pages (`Pages/Admin/Draws.cshtml.cs`, `Pages/Admin/Users/Index.cshtml.cs`) should continue to load against seeded debug data so admin actions visibly affect `/app` in local runs.
- For any feature touching auth, timeline, tickets, or draws, verify both modes before merging: (1) local-debug browser at `/app`; (2) normal Telegram/production path.

## Deployment
- app is running on DigitalOcean
- project uses GitHub Actions for CD via `.github/workflows/deploy-droplet.yml`; on push to `main`/`master` it SSHes to the droplet, pulls the repo, and runs `docker compose -f docker-compose.app.yml up -d --build` on the server.
- Active SDK/runtime pins are `.NET 10` in `global.json`, `MiniApp.csproj`, `Dockerfile`, and `README.md`; `8.0.11` strings in `Migrations/*.Designer.cs` and `AppDbContextModelSnapshot.cs` are legacy EF migration metadata, not current SDK/runtime pins.
