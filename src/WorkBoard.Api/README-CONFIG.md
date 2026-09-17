# Configuration

Nothing secret lives in this folder. `appsettings.json` holds the shape and the safe defaults;
every value that would be a credential is blank there and supplied at run time.

| Setting | Where it comes from | Notes |
| --- | --- | --- |
| `ConnectionStrings:Default` | env `ConnectionStrings__Default` | Blank in `appsettings.json`. Development defaults to `.\SQLEXPRESS` with Windows authentication, so there is no password to commit. |
| `Jwt:SigningKey` | user secrets locally, env `Jwt__SigningKey` elsewhere | At least 32 characters, or the host refuses to start. |
| `Database:MigrateOnStartup` | config | `true` only in Development. See `docs/adr/012-migrations-in-the-pipeline.md`. |
| `Database:SeedDemoData` | config | `true` only in Development. |
| `OpenApi:Enabled` | config | Enables `/openapi/v1.json`, Scalar at `/scalar/v1`, and Swagger UI at `/swagger`; `true` in Development. |
| `Cors:AllowedOrigins` | config | Explicit origins; empty means no browser origin is allowed. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | env | Unset means traces and metrics are collected and dropped. |

The double underscore in `Jwt__SigningKey` is how .NET maps an environment variable onto a nested
configuration key — it is not a typo.

## Local setup

```bash
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/WorkBoard.Api
```

User secrets are stored outside the repository, so there is no way to commit one by accident.

## Pointing at a different SQL Server

The committed Development connection is:

```
Server=.\SQLEXPRESS;Database=WorkBoard;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=30
```

In `appsettings.Development.json` it appears as `Server=.\\SQLEXPRESS`, because JSON encodes one
literal backslash as two. A single backslash there is a parse error, and the C# design-time fallback
in `WorkBoardDbContextFactory` uses a verbatim string (`@"..."`) for the same reason.

Override it without editing the file:

```bash
# LocalDB
dotnet user-secrets set "ConnectionStrings:Default" \
  "Server=(localdb)\MSSQLLocalDB;Database=WorkBoard;Integrated Security=True;TrustServerCertificate=True" \
  --project src/WorkBoard.Api

# A TCP instance with SQL authentication, such as the one in docker-compose
dotnet user-secrets set "ConnectionStrings:Default" \
  "Server=localhost,1433;Database=WorkBoard;User Id=sa;Password=...;TrustServerCertificate=True" \
  --project src/WorkBoard.Api
```

`TrustServerCertificate=True` is present because a local instance presents a self-signed
certificate. It disables certificate validation and belongs in development only — a production
connection should validate the chain.

**If the connection times out** with *"O servidor não foi encontrado ou não estava acessível"*, the
instance name is wrong or the service is not running. Check with:

```bash
sqlcmd -S ".\SQLEXPRESS" -E -C -Q "SELECT @@SERVERNAME"
```

## Which port the API listens on

From `Properties/launchSettings.json`, which is a local-development file and is not used when the
application is published or containerised:

| Profile | Addresses |
| --- | --- |
| `https` | `https://localhost:7179` and `http://localhost:5180` |
| `http` | `http://localhost:5180` |

```bash
dotnet run --project src/WorkBoard.Api --launch-profile https
```

The Angular dev server proxies `/api` to `https://localhost:7179`
(`src/WorkBoard.Web/proxy.conf.json`), so use the `https` profile.

**You do not need to trust the development certificate for the app to work.** The proxy runs in
Node, which connects to Kestrel with certificate validation disabled, so the browser never sees that
certificate. Trust it only to reach `https://localhost:7179` directly from a browser — for the API
reference, for instance:

```bash
dotnet dev-certs https --trust
```

Or skip that and open Scalar at <http://localhost:5180/scalar/v1> or Swagger UI at
<http://localhost:5180/swagger>: the `https` profile binds both ports, and the HTTP one needs no
certificate.

In the container neither file applies — Kestrel binds `ASPNETCORE_HTTP_PORTS=8080` over plain HTTP,
and TLS is terminated by whatever sits in front of it.
