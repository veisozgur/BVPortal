# BV Portal Production Deployment

## Required values

Create `.env` from `.env.example` and provide strong production values:

- `BV_DOMAIN`: public domain name, for example `portal.example.com`
- `BV_SQL_PASSWORD`: strong SQL Server password
- `BV_JWT_KEY`: random value with at least 32 characters
- `BV_ADMIN_PHONE`: administrator phone in international format
- SMTP, NetGSM and Mikro values when those integrations are enabled

## Server requirements

- Linux server with a public IP address
- Docker Engine and Docker Compose plugin
- Domain A/AAAA record pointed to the server
- TCP ports 80 and 443 open
- A backup location for the `bvportal_sql_data` Docker volume

## First deployment

```bash
git clone https://github.com/veisozgur/BVPortal.git
cd BVPortal
cp .env.example .env
nano .env
docker compose pull
docker compose build --pull
docker compose up -d
docker compose ps
```

After the SQL container becomes healthy, apply EF Core migrations from the API image or an administrative workstation before accepting traffic.

## Verification

```bash
docker compose ps
docker compose logs --tail=200 api
docker compose logs --tail=200 web
curl -fsS http://127.0.0.1:7001/health
```

## Important

The current compose file publishes SQL Server, API and Web ports directly. Place the application behind a TLS reverse proxy and restrict ports `1433`, `7000` and `7001` with the host firewall before exposing the server to the internet.
