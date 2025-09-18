Docker and PostgreSQL (development)

Prereqs:
- Docker Desktop

Commands:
- docker compose up --build

Services:
- db: postgres on 5432 with db=gameroombooking, user=grbs, pass=grbs_pass
- api: ASP.NET Core on http://localhost:8080

App config:
- ConnectionStrings__DefaultConnection is provided by compose.
- ASPNETCORE_ENVIRONMENT=Development

Migrations:
- The app runs EnsureCreated() in Development. For schema changes, prefer EF Core migrations in the future.
