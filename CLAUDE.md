# DCElectricWebAPI - Project Context for Claude

## Project Overview

This is a **.NET 8 Web API** that serves as a middleware layer between a frontend application and **Quickbase** (a low-code database platform). The API transforms Quickbase's field-ID-based responses into human-readable, strongly-typed models for consumption by the DC Electric Group website.

## Quick Links to Documentation

| Document | Purpose |
|----------|---------|
| `.claude/QUICKBASE_API_REFERENCE.md` | Condensed API reference with examples |
| `.claude/FIELD_MAPPINGS.md` | Table/field ID to name mappings |
| `.claude/QuickBase_RESTful_API_*.json` | Full OpenAPI spec (2MB, use for detailed schemas) |
| `.claude/appsettings.example.json` | Configuration template |

## Architecture

```
Frontend (Angular) → DCElectricWebAPI → Quickbase REST API
                          ↓
                    Google Cloud Storage (PDFs/Reports)
                    PostgreSQL / Cloud SQL (Report metadata/caching)
```

## Key Technologies
- .NET 8 / ASP.NET Core Web API
- Quickbase REST API (JSON-based)
- Google Cloud Storage (report/PDF storage), GCP project `morrisdev-203721`
- PostgreSQL via Npgsql (Cloud SQL in production)
- ABCpdf (WebSupergoo) for PDF generation — uses the Chrome/`ABCChrome123` engine on Linux
- Serilog logging

---

## Deployment
- **NEVER DEPLOY WITHOUT SPECIFIC INSTRUCTIONS TO DO SO.**
- The DCWebUI frontend proxies `/api/` to this API via nginx. Deploying this API does **not** deploy DCWebUI (separate service/repo).
- **How it deploys:** Cloud Run's continuous-deploy trigger builds the `Dockerfile` and deploys on every push to the **`main`** branch. GCP project `morrisdev-203721`, region `us-central1`, service `dcelectric-webapi`.
- The GitHub Actions workflow `.github/workflows/deploy-cloudrun.yml` is a **manual** fallback only (`workflow_dispatch`); it no longer auto-runs.
- Runtime config: the container runs as `ASPNETCORE_ENVIRONMENT=Production` and binds `PORT=8080`.

## Configuration & Secrets

- **`appsettings.json`** is committed and contains **only non-secret** config (Serilog, QuickBase realm/app IDs, GCS bucket/project, etc.). It must exist or the app fails to start.
- **`appsettings.Development.json`** (git-ignored) holds the real secrets for **local** dev. `appsettings.Docker.json` and `.env` are also git-ignored. A reference copy of dev secrets lives in `creds/` (git-ignored).
- **Production secrets** come from **GCP Secret Manager**, injected as environment variables on the Cloud Run service (config keys use `__` as the section separator):

  | Env var | Secret | Config key |
  |---------|--------|-----------|
  | `ConnectionStrings__DefaultConnection` | `dcelectric-db-connection` | DB connection |
  | `quickbase__token` | `dcelectric-qb-token` | QuickBase user token |
  | `Websupergoo__license` | `dcelectric-abcpdf-license` | ABCpdf license |
  | `/app/secrets/gcs-key.json` (file mount) | `dcelectric-service-account-key` | GCS credentials |

  These secrets are configured **on the Cloud Run service** and persist across trigger deploys. Never commit secret values to the repo.


## PDF Reports
**Streetlights Ticket Report** is generated in the StreetlightsInvoiceController.cs  ProcessTicketReportsAsync
  This report has the option to be displayed as pdf from the endpoint, or "stored", meaning it is sent to google storage after generation and a database is updated to notify the user it is complete and can be downloaded
To generate a ticket report for testing:
```
curl --location '{{server}}/api/pdf/streetlights/invoice/tickets?format=pdf' \
--header 'Content-Type: application/json' \
--header 'Authorization: ••••••' \
--data '[{
    "customerName": "City of Berkeley - On Call Electrical",
    "startDate": "2025-05-01",
    "endDate": "2025-05-31"
  }]'
```

## Quickbase Integration

### Authentication
All Quickbase API calls require these headers:
```http
QB-Realm-Hostname: dcelectricgroup.quickbase.com
Authorization: QB-USER-TOKEN {token}
Content-Type: application/json
```

`QuickBaseConnector` reads the token and realm from configuration (`quickbase:token` and `quickbase:domain`) — never hardcoded. In production the token comes from the `quickbase__token` env var (Secret Manager `dcelectric-qb-token`); locally it comes from the git-ignored `appsettings.Development.json`.

### Apps in Use

| App Name | App ID | Config Key | Purpose |
|----------|--------|-----------|---------|
| Street Lights | `bjrvqd33c` | `quickbase:apps:streetlights` | Main ticket/work order system |
| Jobs | `bkykszyj4` | `quickbase:apps:jobs` | Job tracking |
| Safety | `bk2wutv6x` | `quickbase:apps:safety` | Safety records |
| Timesheets | `bhrneweey` | `quickbase:apps:ts` | Time tracking |

### API Base URL
```
https://api.quickbase.com/v1
```

### Core Endpoints Used

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/records/query` | POST | Query records (most used) |
| `/records` | POST | Insert/update records |
| `/records` | DELETE | Delete records |
| `/tables?appId={id}` | GET | List tables in app |
| `/fields?tableId={id}` | GET | List fields in table |
| `/apps/{appId}` | GET | Get app details |

---

## Quickbase Query Language

Queries use a custom syntax with **field IDs** (not names):

```
{fieldId.OPERATOR.'value'}AND{fieldId.OPERATOR.'value'}
```

### Common Operators
| Operator | Meaning |
|----------|---------|
| `EX` | Equals |
| `XEX` | Not equals |
| `CT` | Contains |
| `GTE` | Greater than or equal |
| `LTE` | Less than or equal |

### Example Query in C#
```csharp
var q = new QBQuery();
q.from = "bjrvqd33t";  // Table ID
q.select = new List<int>() { 119, 27, 45 };  // Field IDs to return
q.where = $"{{18.EX.'{customerName}'}}AND{{45.GTE.'{startDate}'}}AND{{45.LTE.'{endDate}'}}";
q.sortBy = new List<QBFieldSet>() { new QBFieldSet { fieldId = 1, order = "ASC" } };

var results = await qb.Query(q);
```

---

## Response Format Challenge

**Quickbase returns data keyed by field ID:**
```json
{
  "data": [
    {
      "119": { "value": "TKT-001" },
      "27": { "value": "Customer A" }
    }
  ],
  "fields": [
    { "id": 119, "label": "Ticket Number", "type": "text" },
    { "id": 27, "label": "Customer Name", "type": "text" }
  ]
}
```

**This API transforms it to strongly-typed models:**
```csharp
public class Ticket
{
    public string TicketNumber { get; set; }
    public string CustomerName { get; set; }
}
```

---

## Key Files

### Models
| File | Purpose |
|------|---------|
| `Models/QuickBaseLibrary.cs` | Core QB types: `QBQuery`, `QBResultSet`, `QBField`, `QbRecord` |
| `Models/Settings.cs` | Configuration classes for QB settings |
| `Models/Customer.cs` | Customer entity |

### Modules (Business Logic)
| File | Purpose |
|------|---------|
| `Modules/QuickBaseConnector.cs` | Low-level QB REST API client |
| `Modules/StreetLightsService.cs` | Street light billing service (fixtures & tickets) |
| `Modules/StreetLights.cs` | Legacy street light ticket/report logic |
| `Modules/FixtureReport.cs` | Fixture reporting |

### Controllers
| File | Purpose |
|------|---------|
| `Controllers/Pdf/StreetlightsFixtureController.cs` | Fixture & ticket billing endpoints with PDF generation |
| `Controllers/QBController.cs` | Report generation endpoints |
| `Controllers/QueryController.cs` | Generic query endpoint |
| `Controllers/TablesController.cs` | Table metadata |

---

## Common Tasks

### Adding a New Quickbase Query

1. **Identify the table ID and field IDs** (check `.claude/FIELD_MAPPINGS.md` or query `/fields`)
2. **Build a `QBQuery` object:**
   ```csharp
   var q = new QBQuery
   {
       from = "tableId",
       select = new List<int> { 3, 6, 7 },
       where = "{6.CT.'search'}",
       options = new QBQueryOptions { skip = 0, top = 100 }
   };
   ```
3. **Call `QuickBaseConnector.Query()`**
4. **Transform `QBResultSet` into your domain model**

### Mapping Field IDs to Properties

```csharp
foreach (var field in retval.fields)
{
    var name = field.label.Replace(" ", "").ToLower();
    var value = dr[field.id.ToString()]["value"].ToString();
    
    switch (name)
    {
        case "customername":
            model.CustomerName = value;
            break;
        // ... more mappings
    }
}
```

### Adding a New Table Mapping

1. Query the fields: `GET /fields?tableId={tableId}`
2. Document in `.claude/FIELD_MAPPINGS.md`
3. Create a C# model class
4. Add transformation logic

---

## StreetLightsService - Billing System

The `Modules/StreetLightsService.cs` handles fixture and ticket billing with PDF generation.

### Key Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/StreetlightsFixture` | POST | Fixture billing data (JSON or PDF) |
| `/api/StreetlightsFixture/tickets` | POST | Ticket billing data (JSON or PDF) |
| `/api/StreetlightsFixture/customers` | GET | List customers with pricing levels |
| `/api/StreetlightsFixture/divisions/{customer}` | GET | Get divisions for a customer |

### Authentication
Use either:
- `Authorization` header with session token
- `apiKey` query parameter: `dcelectric-sl-2025`

### PDF Generation
Add `?format=pdf` to get PDF output instead of JSON.

### Field ID Mappings (Verified 2026-02-02)

**Tickets Table (bjrvqd33t):**
| Field | ID | Type |
|-------|-----|------|
| Record ID | 3 | recordid |
| Ticket ID | 27 | text (e.g., SL-022640) |
| Customer Name | 18 | text |
| Caller Name | 99 | text |
| Caller Type | 21 | text |
| Service Type | 7 | text |
| Problem Type | 8 | text |
| Details | 10 | text-multi-line |
| Analysis | 16 | text-multi-line |
| Street Light # | 151 | text |
| Fixture Type | 105 | lookup |
| Address | 201 | text-formula |
| Cross Street | 150 | text |
| Job # | 119 | text |
| Start Date | 43 | date |
| Start Time | 44 | time-of-day |
| Completion Date | 45 | date |
| Completion Time | 46 | time-of-day |
| Completed By | 56 | user |
| Not Billable | 167 | checkbox |
| Billable Override | 204 | checkbox |
| Do Not Report | 210 | checkbox |
| Division ID | 212 | numeric |

**Equipment Line Items Table (bjrvqd34w):**
| Field | ID | Type |
|-------|-----|------|
| Record ID | 3 | recordid |
| Hours | 6 | numeric |
| Equipment | 9 | text (lookup - equipment name) |
| Related Ticket | 11 | text (lookup - ticket ID) |

**Labor Line Items Table (bjrvqd34z):**
| Field | ID | Type |
|-------|-----|------|
| Record ID | 3 | recordid |
| Date | 6 | date |
| Hours | 7 | numeric |
| Type of Hours | 8 | text |
| Team Member | 9 | user |
| Related Ticket | 10 | numeric |
| Ticket ID | 11 | text (lookup) |
| Type of Labor | 12 | text |

---

## Development Notes

### Known Issues / Tech Debt

1. **Generic exception handling** - Code throws `Exception()` on QuickBase API failures. Consider specific exception types, proper HTTP status codes, and detailed logging.

2. **No pagination handling** - Large queries may hit limits. Check `metadata.totalRecords`.

### Pagination Pattern
```csharp
var allRecords = new List<JObject>();
int skip = 0;
int top = 100;

do
{
    q.options = new QBQueryOptions { skip = skip, top = top };
    var batch = await qb.Query(q);
    allRecords.AddRange(batch.data);
    skip += top;
} while (allRecords.Count < batch.metadata.totalRecords);
```

### Rate Limiting
- Quickbase has rate limits per realm
- Implement exponential backoff on 429 responses
- Cache metadata (tables, fields) that rarely changes

---

## External Services

| Service | Purpose | Config Key |
|---------|---------|------------|
| Google Cloud Storage | PDF/report storage | `GoogleCloudStorage:*` (credential file mounted at `/app/secrets/gcs-key.json`) |
| PostgreSQL (Cloud SQL) | Report metadata/caching | `ConnectionStrings:DefaultConnection` |
| QuickBase | Source data | `quickbase:token`, `quickbase:domain` |

---

## Testing

### Test a Quickbase Query
```bash
curl -X POST "https://api.quickbase.com/v1/records/query" \
  -H "QB-Realm-Hostname: dcelectricgroup.quickbase.com" \
  -H "Authorization: QB-USER-TOKEN YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"from":"bjrvqd33t","select":[3,119],"options":{"top":5}}'
```

### Swagger UI
Available at `/swagger` when running locally.
