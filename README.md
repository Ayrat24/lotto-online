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
BOT_WEBAPP_URL=https://your-domain.example
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

Then open from the same machine:
- App: http://127.0.0.1:8080/
- Admin panel: http://127.0.0.1:8080/Admin (will redirect to /Admin/Login)

For public production access, put the app behind a reverse proxy on your real HTTPS domain.

## Migrate from ngrok to a real domain on DigitalOcean

This app already supports a real public domain. In production you mainly need to:

1. point DNS to the droplet,
2. terminate HTTPS on the droplet,
3. proxy the domain to `127.0.0.1:8080`,
4. set `BOT_WEBAPP_URL` to the real domain,
5. reset the Telegram webhook.

### 1) Pick the public URL you want to keep

Use one of these patterns:

- `https://example.com`
- `https://www.example.com`
- `https://app.example.com`
- `https://example.com/app`

For this project, the simplest option is usually:

```text
https://your-domain.example
```

The app will serve the mini app at `/app`, admin at `/Admin`, Telegram webhook at `/bot`, and BTCPay webhook at `/api/webhooks/btcpay`.

### 2) Point your domain to the droplet

In DigitalOcean DNS:

- create an `A` record for `@` -> your droplet IPv4
- optionally create `A` or `CNAME` for `www` or `app`

Example:

```text
@    A      <your_droplet_ip>
www  CNAME  @
```

Wait for DNS to resolve before continuing.

### 3) Keep Docker app private on the droplet

`docker-compose.app.yml` binds the app to `127.0.0.1:8080`, so it is reachable only from the droplet itself. Your public traffic should go through a reverse proxy like Nginx.

### 4) Install a simple reverse proxy + TLS on the droplet

These commands are for Ubuntu/Debian on the droplet.

Install Nginx and Certbot:

```bash
sudo apt-get update
sudo apt-get install -y nginx certbot python3-certbot-nginx
```

Create an Nginx site config:

```bash
sudo nano /etc/nginx/sites-available/miniapp
```

Use this config, replacing `your-domain.example`:

```nginx
server {
    server_name your-domain.example;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;

        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

Enable it and remove the default site:

```bash
sudo ln -s /etc/nginx/sites-available/miniapp /etc/nginx/sites-enabled/miniapp
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx
```

Request HTTPS certificates:

```bash
sudo certbot --nginx -d your-domain.example
```

If you also want `www`:

```bash
sudo certbot --nginx -d your-domain.example -d www.your-domain.example
```

### 5) Update the app env file on the droplet

On the server, set `BOT_WEBAPP_URL` to the final HTTPS domain.

```bash
cd /path/to/MiniApp
cp .env.example .env
nano .env
```

Minimum values:

```env
BOT_TOKEN=<your bot token>
BOT_WEBAPP_URL=https://your-domain.example
ADMIN_USERNAME=admin-panel
ADMIN_PASSWORD=<long-random-password>
POSTGRES_DB=miniapp
POSTGRES_USER=miniapp
POSTGRES_PASSWORD=<long-random-db-password>
```

### 6) Redeploy on the droplet

```bash
cd /path/to/MiniApp
docker compose -f docker-compose.app.yml up -d --build
docker compose -f docker-compose.app.yml ps
```

### 7) Point Telegram to the new domain

After the site is live on HTTPS, reset the bot webhook to the new domain:

```bash
curl https://your-domain.example/bot/setWebhook
curl https://your-domain.example/bot/webhookInfo
```

Expected webhook target:

```text
https://your-domain.example/bot
```

### 8) Update your bot's Mini App button in Telegram

This app uses `BotWebAppUrl` as the public Mini App URL source. After redeploying with the new domain, the bot will open the Mini App from the new URL automatically when users press the inline button.

### 9) What changes compared to ngrok

Replace this:

```text
BOT_WEBAPP_URL=https://something.ngrok-free.app
```

with this:

```text
BOT_WEBAPP_URL=https://your-domain.example
```

You no longer need:

- ngrok running on your machine
- a temporary tunnel URL
- frequent webhook reconfiguration because the tunnel URL changed

### 10) Quick verification checklist

Check all of these after deploy:

- `https://your-domain.example/` loads
- `https://your-domain.example/app` loads
- `https://your-domain.example/Admin` loads
- `https://your-domain.example/tonconnect-manifest.json` returns JSON
- `https://your-domain.example/bot/webhookInfo` shows the correct webhook URL
- opening the bot and pressing the Mini App button opens the new domain

### Common issues

- **Telegram refuses the webhook**: the domain must be public HTTPS with a valid certificate.
- **Mini App opens the wrong host**: confirm `.env` has the final `BOT_WEBAPP_URL` and redeploy.
- **Referral links use the wrong host**: this repo now prefers `BOT_WEBAPP_URL` for generated invite links.
- **App thinks requests are HTTP**: make sure Nginx sends `X-Forwarded-Proto` and `X-Forwarded-Host`.
- **Port 8080 is still public**: check the compose port binding is `127.0.0.1:8080:8080`.

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
