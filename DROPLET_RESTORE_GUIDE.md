# Droplet Restore Guide — MiniApp

This guide restores the MiniApp (Telegram lotto bot) on a fresh Ubuntu droplet after a wipe.

---

## 1. Prerequisites — install Docker, Nginx, Certbot, Git

> **⚠️ Run ALL commands in this section first.** Docker is not pre-installed on a fresh droplet.

```bash
# Update package list
sudo apt-get update

# Install Docker (from official Docker repo)
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Install Nginx + Certbot
sudo apt-get install -y nginx certbot python3-certbot-nginx

# Install Git
sudo apt-get install -y git
```

Verify Docker works:
```bash
sudo docker run hello-world
```

> **Note:** If `sudo docker` still says "command not found" after the install, log out and back in (`exit` then SSH again), or run `newgrp docker` to refresh group membership.

---

## 2. Clone the repository

```bash
sudo mkdir -p /opt/miniapp
sudo chown "$USER:$USER" /opt/miniapp
cd /opt/miniapp
git clone https://github.com/Ayrat24/lotto-online.git .
```

---

## 3. Create the `.env` file

```bash
cp .env.example .env
nano .env
```

Fill in your real values (replace the placeholders):

```env
BOT_TOKEN=your-telegram-bot-token-here
BOT_WEBAPP_URL=https://your-domain.com
ADMIN_USERNAME=admin-panel
ADMIN_PASSWORD=your-long-random-admin-password
POSTGRES_DB=miniapp
POSTGRES_USER=miniapp
POSTGRES_PASSWORD=your-long-random-db-password
```

> **Important:** If you had a previous PostgreSQL volume with data you want to keep, you must use the **same** `POSTGRES_PASSWORD` as before. If the volume was wiped too, any password is fine.

Save and exit (`Ctrl+X`, then `Y`, then `Enter`).

---

## 4. Point your domain to the droplet IP

In your domain's DNS settings (DigitalOcean DNS or wherever you manage DNS):

| Type | Name | Value |
|------|------|-------|
| A    | @    | `<your-droplet-ipv4>` |
| A    | www  | `<your-droplet-ipv4>` |

Wait for DNS to propagate (usually a few minutes).

---

## 5. Set up Nginx reverse proxy + SSL

### 5a. Create Nginx site config

```bash
sudo nano /etc/nginx/sites-available/miniapp
```

Paste this config, replacing `your-domain.com` with your actual domain:

```nginx
server {
    server_name your-domain.com;

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

### 5b. Enable the site and remove default

```bash
sudo ln -s /etc/nginx/sites-available/miniapp /etc/nginx/sites-enabled/miniapp
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx
```

### 5c. Get SSL certificate

```bash
sudo certbot --nginx -d your-domain.com
```

If you also want `www.your-domain.com`:
```bash
sudo certbot --nginx -d your-domain.com -d www.your-domain.com
```

Follow the prompts. Certbot will automatically modify the Nginx config to add SSL.

---

## 6. Start the app

```bash
cd /opt/miniapp
sudo docker compose -f docker-compose.app.yml up -d --build
```

Check that both containers are running:
```bash
sudo docker compose -f docker-compose.app.yml ps
```

Expected output:
```
NAME                IMAGE                        STATUS          PORTS
miniapp-web         miniapp-app                  Up (healthy)    127.0.0.1:8080->8080/tcp
miniapp-postgres    postgres:16-alpine           Up (healthy)    5432/tcp
```

Check app logs if something is wrong:
```bash
sudo docker compose -f docker-compose.app.yml logs --tail 100 app
```

---

## 7. Verify the site is live

```bash
curl -I https://your-domain.com/
curl -I https://your-domain.com/app
curl -I https://your-domain.com/Admin
```

All should return `200 OK` or `302 Found` (redirect to login for Admin).

---

## 8. Set up the Telegram webhook

```bash
curl https://your-domain.com/bot/setWebhook
```

Verify the webhook is set correctly:
```bash
curl https://your-domain.com/bot/webhookInfo
```

Expected output should show the webhook URL pointing to `https://your-domain.com/bot`.

---

## 9. Final verification checklist

Run these checks:

```bash
# 1. Homepage loads
curl -s -o /dev/null -w "%{http_code}" https://your-domain.com/

# 2. Mini App loads
curl -s -o /dev/null -w "%{http_code}" https://your-domain.com/app

# 3. Admin panel loads (redirects to login)
curl -s -o /dev/null -w "%{http_code}" https://your-domain.com/Admin

# 4. Webhook info returns JSON
curl -s https://your-domain.com/bot/webhookInfo | head -20

# 5. Container status
sudo docker compose -f /opt/miniapp/docker-compose.app.yml ps
```

---

## 10. (Optional) Restore from a database backup

If you have a PostgreSQL dump from before the wipe:

```bash
# Copy the dump to the droplet, then restore:
sudo docker compose -f /opt/miniapp/docker-compose.app.yml exec -T db \
  psql -U miniapp -d miniapp < /path/to/your/backup.sql
```

---

## 11. (Optional) Set up GitHub Actions CD again

The GitHub Actions workflow in `.github/workflows/deploy-droplet.yml` expects these secrets in your GitHub repo:

| Secret | Value |
|--------|-------|
| `DROPLET_HOST` | Your droplet's IP address |
| `DROPLET_USER` | Your SSH username (usually `root`) |
| `DROPLET_SSH_KEY` | Private SSH key that can access the droplet |

To add them:
1. Go to your GitHub repo → **Settings** → **Secrets and variables** → **Actions**
2. Add each secret

After that, any push to `main`/`master` will automatically redeploy.

---

## Troubleshooting

### App container keeps restarting
```bash
sudo docker compose -f /opt/miniapp/docker-compose.app.yml logs app
```

### Database connection refused
Make sure the `db` container is healthy:
```bash
sudo docker compose -f /opt/miniapp/docker-compose.app.yml ps db
```

### Nginx 502 Bad Gateway
Check that the app container is running on port 8080:
```bash
curl -I http://127.0.0.1:8080/
```

### SSL certificate expired
```bash
sudo certbot renew
sudo systemctl reload nginx
```

### Port 8080 exposed to the internet
Verify the compose file binds to `127.0.0.1:8080:8080` (not `0.0.0.0:8080:8080`):
```bash
grep "8080" /opt/miniapp/docker-compose.app.yml
```

### Certificate error: `ERR_CERT_COMMON_NAME_INVALID` or "Your connection isn't private"
This means Certbot hasn't been run, or the SSL certificate doesn't cover the domain you're using.

**First, verify the app is actually running internally:**
```bash
curl -I http://127.0.0.1:8080/
```
If this returns a response, the app is fine — the problem is just SSL.

**Run Certbot to get a valid certificate:**
```bash
sudo certbot --nginx -d mini-app-test.store
```
If you also want `www.mini-app-test.store`:
```bash
sudo certbot --nginx -d mini-app-test.store -d www.mini-app-test.store
```

**If Certbot says "no certificate found" or asks for setup**, make sure the Nginx site config exists first:
```bash
sudo nano /etc/nginx/sites-available/miniapp
```
Paste this (replace domain with yours):
```nginx
server {
    server_name mini-app-test.store;

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
Then enable and reload:
```bash
sudo ln -s /etc/nginx/sites-available/miniapp /etc/nginx/sites-enabled/miniapp
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx
```

Then re-run:
```bash
sudo certbot --nginx -d mini-app-test.store
```

**Access the site with HTTP temporarily** to verify the app works:
```bash
curl -I http://mini-app-test.store/
curl -I http://mini-app-test.store/app
curl -I http://mini-app-test.store/bot/setWebhook
```
