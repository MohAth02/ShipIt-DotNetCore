# ShipIt

ASP.NET Core 8 warehouse inventory API for employees, companies, products, stock, and inbound/outbound orders.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL

## Database

Create two Postgres databases (one for the app, one for tests). Ask a teammate for a dump and restore it into both.

Then apply migrations, including `Database/Migrations/001_add_employee_id.sql`:

```bash
psql -d your_database_name -f Database/Migrations/001_add_employee_id.sql
```

Add a `.env` file in both `ShipIt/` and `ShipItTest/`:

```
POSTGRES_CONNECTION_STRING=Server=127.0.0.1;Port=5432;Database=your_database_name;User Id=your_user;Password=your_password;
```

## Run

```bash
cd ShipIt
dotnet run
```

The API listens on `http://localhost:5000` and `https://localhost:5001`. Check it with `GET /status`.

## Test

```bash
cd ShipItTest
dotnet test
```

Point `ShipItTest/.env` at the test database. Tests truncate tables before each run.

## Endpoints

- `GET /status` — warehouse counts
- `GET/POST/DELETE /employees` — look up by name, warehouse, or `GET /employees/by-id/{id}`
- `GET/POST /companies`
- `GET/POST /products`
- `GET/POST /orders/inbound`
- `POST /orders/outbound` — returns truck loading
