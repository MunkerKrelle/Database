# MMO Relational Database

A PostgreSQL database for an MMO-style game, with a C# (.NET 8 / EF Core) console app that exercises it. The project is about designing and operating a relational database properly: a constrained schema, role-based access, streaming replication, transactions, and measured index optimisation. Everything runs locally with Docker.

## Highlights

| Topic | What is implemented | Where to look |
| --- | --- | --- |
| **Schema design** | 13 tables, composite keys, `CHECK` constraints, a custom `ENUM` (`item_quality`), cascading/`SET NULL` deletes | `SQL/create_tables.sql` |
| **Business rules in the DB** | Triggers enforce that a character can only ride a mount it has collected, and that inventory slots/stack sizes are valid | `SQL/create_tables.sql` |
| **Data integrity** | A composite foreign key `(guild_id, realm_id)` guarantees a character's guild belongs to the character's realm | `SQL/create_tables.sql` |
| **Security / roles** | Least-privilege roles: `developer`, `game_server`, `moderator` (no access to e-mail, column-level grant, GDPR-minded), `replicator` | `SQL/roles.sql` |
| **Replication** | Primary + hot standby (streaming replication via `pg_basebackup`); the app verifies a write on the primary shows up on the standby | `docker-compose.yml`, `pg_hba.conf` |
| **Transactions (ACID)** | `BuyItem` moves an item and gold between two players in one transaction with commit / rollback | `Server/GameServer.cs` |
| **Authentication** | Passwords hashed with bcrypt (salt stored inside the hash) | `Server/GameServer.cs` |
| **Index benchmark** | Compares *no index* vs *B-tree* vs *Hash* on 50,000 generated characters using `EXPLAIN (ANALYZE, BUFFERS)` | `Data/Index.cs` |
| **Queries** | LINQ queries written to mirror relational-algebra examples (joins, grouping, `HAVING`-style filters) | `Program.cs` |

## Tech stack

- **PostgreSQL 17** (primary + standby)
- **.NET 8**, **Entity Framework Core 8** with Npgsql and snake_case naming
- **BCrypt.Net-Next** for password hashing
- **Docker / Docker Compose** for the whole environment

## Getting started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine + Compose)
- Optional, to run the app outside Docker: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Option A: everything in Docker

```bash
git clone https://github.com/MunkerKrelle/Database.git
cd Database/"Relationel Database"/Relationel_Database
docker compose up --build
```

This starts the primary (`localhost:5432`), the standby (`localhost:5433`) and the app. The SQL files in `SQL/` run automatically on the first start (tables, then test data, then roles). The app prints its output and exits when it is done; the database containers keep running.

Re-run just the app:

```bash
docker compose up app
```

Reset everything (the SQL scripts only run on an empty database volume):

```bash
docker compose down -v
```

### Option B: database in Docker, app on your machine

```bash
cd Database/"Relationel Database"/Relationel_Database
docker compose up -d postgres postgres_standby
dotnet run --launch-profile "Relationel Database"
```

The launch profile in `Properties/launchSettings.json` sets the three connection strings the app needs (`CONNECTION_STRING`, `STANDBY_CONNECTION_STRING`, `ADMIN_CONNECTION_STRING`). In Visual Studio, open `Relationel_Database.slnx` and press F5 with the same profile.

## What happens when you run it

1. **Connection check** against the primary.
2. **Player session**: sign up, a failed login (wrong password), a successful login, character creation, and an item trade inside a transaction.
3. **Replication check**: reads the new character from the *standby* database.
4. **Test data**: generates 50,000 accounts and characters (with inventory, mounts and quests) if the database is still small. The first run takes a little while.
5. **Index benchmark**: runs five queries with no index, B-tree indexes and Hash indexes, then prints a comparison of scan type, planning/execution time and buffer hits. The temporary indexes are always dropped afterwards.
6. **Queries**: a sample of characters, pending quest rewards (join), and gold per guild (grouping).

## Reading the index benchmark

`EXPLAIN (ANALYZE, BUFFERS)` shows how PostgreSQL executed each query. Without an index the planner uses a **Sequential Scan** (reads the whole table). With a suitable index it can use an **Index Scan**, which reads far fewer blocks. B-tree indexes support equality *and* range queries (`BETWEEN`, `<`, `>`); Hash indexes only support equality, so they cannot help the `level BETWEEN 50 AND 60` query. Run the app to see the measured numbers on your own machine, since timings depend on hardware.

## Roles

| Role | Purpose | Access |
| --- | --- | --- |
| `developer` | Development and administration | All tables |
| `game_server` | Used by the C# server | Read everything; write player data only |
| `moderator` | Investigating players | Read player tables only; no game content; no e-mail column |
| `replicator` | Standby server | Replication only |

## Notes

- The passwords in `docker-compose.yml`, `launchSettings.json` and `roles.sql` are **local development placeholders**. Never reuse them anywhere real.
- Ports `5432` and `5433` must be free on your machine.
- This is a learning project: the client/server split is simulated inside one console app.
