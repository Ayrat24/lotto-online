# Telegram.Bot Mini-App example

This example was built starting from Visual Studio template project: ASP.NET Core Web App (Razor Pages)

## SDK / runtime

- Project SDK target: `.NET 10`
- Local development requires a `.NET 10 SDK`
- Docker build/runtime images are based on `.NET 10`

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

Postgres will be exposed on `localhost:5432`.

Default credentials in `docker-compose.yml`:
- database: `miniapp`
- user: `miniapp`
- password: `miniapp`

### 2) Point the app to PostgreSQL (User Secrets)

```cmd
cd /d E:\Projects\lotto\MiniApp
dotnet user-secrets set "Database:ConnectionString" "Host=localhost;Port=5432;Database=miniapp;Username=miniapp;Password=miniapp"
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
ADMIN_USERNAME=admin
ADMIN_PASSWORD=change-me
```

### 2) Start

```cmd
cd /d E:\Projects\lotto\MiniApp
docker compose -f docker-compose.app.yml up -d --build
```

Then open:
- App: http://localhost:8080/
- Admin panel: http://localhost:8080/Admin (will redirect to /Admin/Login)

### 3) Stop

```cmd
cd /d E:\Projects\lotto\MiniApp
docker compose -f docker-compose.app.yml down
```

To wipe DB data:

```cmd
docker compose -f docker-compose.app.yml down -v
```
