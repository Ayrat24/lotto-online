# Telegram.Bot Mini-App example

This example was built starting from Visual Studio template project: ASP.NET Core Web App (Razor Pages)

## SDK / runtime

- Project SDK target: `.NET 10`
- Local development requires a `.NET 10 SDK`
- Docker build/runtime images are based on `.NET 10`
- Older `8.0.11` strings inside `Migrations/*.Designer.cs` and `Migrations/AppDbContextModelSnapshot.cs` are legacy EF migration metadata, not the active SDK/runtime used by the app

Minimal changes were made:
- Program.cs: for the bot webhook and starting the example website as a Telegram Mini-App
- _Layout.cshtml: importing required telegram-web-app.js

-----

Now we also included the WebAppDemo and DurgerKingBot example bot
(in demo.cshtml, cafe.cshtml, Cafe.cs and some wwwroot static files)

## WebAppDemo & DurgerKingBot examples

Static data imported from official WebApps:
- https://webappcontent.telegram.org/cafe
- https://webappcontent.telegram.org/demo

Server-side code reconstructed from above, and adapted from:
- https://github.com/arynyklas/DurgerKingBot
- https://github.com/telegram-bot-php/durger-king

## Notes

For DurgerKing to serve invoices, you will need to set a "PaymentProviderToken" in appsettings.json
_(typically from [Stripe in TEST mode](https://telegrambots.github.io/book/4/payments.html))_

Sending WebAppData to bot (button "Send time to bot") works only when opening the webapp via a ReplyKeyboardButton
_(try using the second "Hello World!" button)_

## PostgreSQL (Docker) local setup

This project includes a `docker-compose.yml` that starts a local PostgreSQL instance.

### 1) Start PostgreSQL

```cmd
cd /d E:\Projects\lotto\MiniApp
docker compose up -d
```

Postgres will be exposed on `127.0.0.1:5432` only.

Default credentials in `docker-compose.yml`:
- database: `miniapp`
- user: `miniapp`
- password: `miniapp`

### 2) Point the app to PostgreSQL (User Secrets)

```cmd
cd /d E:\Projects\lotto\MiniApp
dotnet user-secrets set "Database:ConnectionString" "Host=127.0.0.1;Port=5432;Database=miniapp;Username=miniapp;Password=miniapp"
```

### 3) Create/update schema (EF Core migrations)

```cmd
cd /d E:\Projects\lotto\MiniApp
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4) Stop PostgreSQL

```cmd
cd /d E:\Projects\lotto\MiniApp
docker compose down
```

If you want to delete the DB data completely:

```cmd
docker compose down -v
```

## Run the full stack with Docker (App + PostgreSQL)

This repo includes:
- `Dockerfile` (builds the ASP.NET app)
- `docker-compose.app.yml` (runs **app + Postgres**)

### 1) Create a `.env` file (recommended)

Create `E:\Projects\lotto\MiniApp\.env` with:

```env
BOT_TOKEN=put-your-telegram-bot-token-here
BOT_WEBAPP_URL=https://your-public-https-domain
ADMIN_USERNAME=admin-panel
ADMIN_PASSWORD=use-a-long-random-admin-password
POSTGRES_DB=miniapp
POSTGRES_USER=miniapp
POSTGRES_PASSWORD=use-a-long-random-db-password
```

Production note: `docker-compose.app.yml` no longer publishes PostgreSQL on port `5432`, so the database is reachable only from the internal Docker network unless you explicitly expose it yourself.

### 2) Start

```cmd
cd /d E:\Projects\lotto\MiniApp
docker compose -f docker-compose.app.yml up -d --build
```

Then open:
- App: http://localhost:8080/
- Admin panel: http://localhost:8080/Admin (will redirect to /Admin/Login)

## Droplet update / deploy notes

If your droplet deploys this project with Docker Compose, you do **not** need a host-installed `.NET` SDK/runtime for the app itself. The build and runtime come from the repo's `Dockerfile` (`mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0`).

### Recommended server prerequisites

- Docker Engine
- Docker Compose plugin (`docker compose`)

### Update the droplet to the current app/runtime

```bash
cd /path/to/MiniApp
cp .env.example .env
# edit .env and set real BOT_TOKEN, BOT_WEBAPP_URL, ADMIN_*, POSTGRES_*
docker compose -f docker-compose.app.yml pull
docker compose -f docker-compose.app.yml up -d --build
docker compose -f docker-compose.app.yml ps
```

### Verify the running container runtime

```bash
docker compose -f docker-compose.app.yml exec app dotnet --info
```

You should see a `.NET 10` runtime inside the `app` container.

### If you already have an existing Postgres volume

Changing `POSTGRES_PASSWORD` in `.env` does **not** automatically rotate the password inside an already-initialized Postgres data volume. If your current DB still uses the old/default password, rotate it first and then redeploy:

```bash
docker compose -f docker-compose.app.yml exec db \
  psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
  -c "ALTER USER $POSTGRES_USER WITH PASSWORD 'your-new-long-random-password';"
```

Then update `.env` with the same new `POSTGRES_PASSWORD` value and run `docker compose -f docker-compose.app.yml up -d --build` again.

### Only if you run `dotnet` directly on the droplet

Install a `.NET 10` SDK/runtime on the host only when you plan to run commands like `dotnet build`, `dotnet ef`, or `dotnet MiniApp.dll` directly on the server outside Docker.

### 3) Stop

```cmd
cd /d E:\Projects\lotto\MiniApp
docker compose -f docker-compose.app.yml down
```

To wipe DB data:

```cmd
docker compose -f docker-compose.app.yml down -v
```
