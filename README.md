# Sigurna dob — final project

Blazor Server application for managing a nursing home: residents, rooms, care tasks, visits, activities, user accounts, and a dashboard.

**Stack:** .NET 10, EF Core (SQLite), JWT, MudBlazor, Swagger

---

## Solution structure

```
SigurnaDob.slnx
├── SigurnaDob.Api      — REST API, authentication, migrations
├── SigurnaDob.App      — Blazor Server UI (MudBlazor)
└── SigurnaDob.Shared   — models, DTOs, constants
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- (Recommended) JetBrains Rider or Visual Studio 2022
- EF tools (once per machine):

```bash
dotnet tool restore
```

---

## Running the application

### Rider

1. Open `SigurnaDob.slnx`
2. Run **Multi-Launch** or both projects with the **https** profile
3. Open the Blazor app: `https://localhost:7096`
4. Swagger (API): `https://localhost:7210/swagger`

### CLI

```bash
# Terminal 1 — API
cd SigurnaDob.Api
dotnet run --launch-profile https

# Terminal 2 — App
cd SigurnaDob.App
dotnet run --launch-profile https
```

| Service | HTTPS | HTTP |
|---------|-------|------|
| **API** | `7210` | `5088` |
| **App** | `7096` | `5064` |

On first startup, the API automatically applies migrations and seeds demo data (`DemoDataSeeder`, `AppUserSeeder`).

---

## Demo users

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@sigurna-dob.local` | `Admin123!` |
| Coordinator | `coordinator@sigurna-dob.local` | `Coordinator123!` |
| Caregiver | `caregiver@sigurna-dob.local` | `Caregiver123!` |
| Family member | `family@sigurna-dob.local` | `Family123!` |

Login in the app: `/login`  
Login in Swagger: `POST /api/auth/login` → copy `accessToken` into **Authorize**.

---

## Modules

| Module | API | Blazor pages | Who can view / edit |
|--------|-----|--------------|---------------------|
| Authentication | `AuthController` | `/login` | Everyone (login); `/api/auth/me` for signed-in user |
| Residents | `ResidentsController` | `/residents`, profile, create/edit | Staff view; coordinator/admin edit |
| Rooms | `RoomsController` | `/rooms`, create/edit | Staff view; coordinator/admin edit |
| Family contacts | `FamilyContactsController` | create/edit from resident profile | Staff view; coordinator/admin edit |
| Care tasks | `CareTasksController` | `/care-tasks`, `/my-care-tasks` | Staff/coordinator manage; caregiver sees own tasks |
| Visit requests | `VisitRequestsController` | `/visit-requests`, `/my-visit-requests` | Coordinator decides; family submits own requests |
| Activities | `ActivitiesController` | `/activities`, create/edit | Coordinator/admin |
| User accounts | `UsersController` | `/users`, create/edit | Admin only |
| Resident media | `ResidentMediaController` | upload on profile | Staff view; coordinator/admin upload/delete |
| Dashboard | `DashboardController` | `/` (Home) | Operational: admin/coordinator; personal: caregiver/family |
| Calendar | `CalendarController` | `/calendar` | Staff and family (family sees own visits) |
| Room occupancy | `RoomsController` | `/room-occupancy` | Staff |
| Caregiver workload | `CareTasksController` | `/caregiver-workload` | Coordinator/admin (all); caregiver (own) |
| AI assistant | `AiController` | `/` (summary), `/care-tasks/create` (suggestion) | Coordinator/admin |
| Lookups | `LookupsController` | — (used by forms) | Staff |

---

## Authorization rules (summary)

- **Staff** — Admin, Coordinator, Caregiver
- **CoordinatorOrAdmin** — Admin or Coordinator
- **AdminOnly** — Admin only
- **FamilyMember** — for `/api/VisitRequests/mine`
- **Caregiver** — for `/api/CareTasks/mine` and task execution
- Most API routes require JWT (`FallbackPolicy`)

---

## Authorization tests

Tested on `http://localhost:5088` (Development, September 2026).

| # | URL | User | Expected | Actual | ✓ |
|---|-----|------|----------|--------|---|
| 1 | `GET /api/health` | — (anonymous) | 200 | 200 | ✓ |
| 2 | `GET /api/residents` | — (anonymous) | 401 | 401 | ✓ |
| 3 | `GET /api/residents` | admin | 200 | 200 | ✓ |
| 4 | `GET /api/users` | admin | 200 | 200 | ✓ |
| 5 | `GET /api/users` | caregiver | 403 | 403 | ✓ |
| 6 | `GET /api/CareTasks/mine` | family | 403 | 403 | ✓ |
| 7 | `GET /api/CareTasks/mine` | caregiver | 200 | 200 | ✓ |
| 8 | `GET /api/dashboard` | family | 200 | 200 | ✓ |
| 9 | `POST /api/rooms` | caregiver | 403 | 403 | ✓ |
| 10 | `POST /api/VisitRequests/mine` | family | 201 | 201 | ✓ |
| 11 | `GET /api/residents` | invalid token | 401 | 401 | ✓ |

**Covered:** 200, 201, 401, 403 (more than 8 tests, as required by the final project spec).

### AI tests (September 2026)

| # | Test | Expected | Actual | ✓ |
|---|------|----------|--------|---|
| 1 | `GET /api/ai/summary` without token | 401 | 401 | ✓ |
| 2 | `GET /api/ai/summary` as caregiver | 403 | 403 | ✓ |
| 3 | `GET /api/ai/summary` as family | 403 | 403 | ✓ |
| 4 | `GET /api/ai/summary` as coordinator | 200, Provider: Mock | 200 | ✓ |
| 5 | `POST /api/ai/care-task-suggestion` empty note | 400 | 400 | ✓ |
| 6 | Suggestion: therapy / Mara / Ivan | Terapija, dueAt | 200 | ✓ |
| 7 | Suggestion: nutrition | Prehrana | 200 | ✓ |
| 8 | Suggestion: hygiene | Higijena | 200 | ✓ |
| 9 | Save suggestion via `POST /api/caretasks` | 201 | 201 | ✓ |

**Example — login and protected request:**

```bash
TOKEN=$(curl -s -X POST http://localhost:5088/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@sigurna-dob.local","password":"Admin123!"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])")

curl -s -o /dev/null -w "%{http_code}\n" \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5088/api/residents
```

**Example — AI summary:**

```bash
TOKEN=$(curl -sk -X POST https://localhost:7210/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"coordinator@sigurna-dob.local","password":"Coordinator123!"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])")

curl -sk https://localhost:7210/api/ai/summary \
  -H "Authorization: Bearer $TOKEN"
```

---

## Database migrations

Tooling:

```bash
dotnet tool restore
dotnet ef database update --project SigurnaDob.Api --startup-project SigurnaDob.Api
```

### Migration test (September 2026)

| Step | Result |
|------|--------|
| **Rebuild solution** | ✓ 0 errors, 0 warnings |
| **`database update 0`** | ✗ SQLite FK error when rolling back seed data (`SeedLookupData`) — expected SQLite limitation |
| **Fresh database from migrations** | ✓ Delete `SigurnaDob.db` (+ `-shm`, `-wal`), run `database update` → all migrations applied |
| **Seed after API startup** | ✓ Demo users and business data available after first `dotnet run` |

**Recommended verification procedure:**

```bash
cd SigurnaDob.Api
rm -f SigurnaDob.db SigurnaDob.db-shm SigurnaDob.db-wal
dotnet ef database update
dotnet run --launch-profile http
# Verify login: admin@sigurna-dob.local / Admin123!
```

Lookup tables come from the `SeedLookupData` migration. User accounts and demo business data are seeded on API startup (`Program.cs`).

Current migrations:

1. `InitialCreate`
2. `SeedLookupData`
3. `AddChangeHistory` — adds `ResidentStatusHistories` and `CareTaskChangeHistories`

---

## Build

```bash
dotnet clean
dotnet build
# Expected: 0 Error(s), 0 Warning(s)
```

---

## Git repository

Remote: `git@github.com:Tin922/Sigurna-Dob.git`

---

## Submission notes

- Local SQLite database (`*.db`) and uploaded files are **not** in the repository (`.gitignore`)
- DBML model: `sigurna-dob.dbml` — export a diagram image from [dbdiagram.io](https://dbdiagram.io)

---

## Bonus features

### 1. Change history

- **Resident status history** — `GET /api/residents/{id}/status-history`, shown on the resident profile
- **Care task history** — status and caregiver assignment; `GET /api/caretasks/{id}/history`, shown on the task edit page
- Changes are recorded when tasks are created, updated, started, or completed, and when a resident status changes

### 2. Calendar of visits and activities

- `GET /api/calendar?from=...&to=...`
- Page `/calendar` — monthly view of visits and activities
- Staff see visits and activities; family members see their own planned visits
- Clicking a day shows details and a link to the related module

### 3. Room occupancy and caregiver workload

- `GET /api/rooms/occupancy` → `/room-occupancy`
- `GET /api/caretasks/workload` → `/caregiver-workload`
- Coordinator/admin see all caregivers; a caregiver sees their own workload

### 4. Mobile layout and accessibility

- Responsive drawer with hamburger menu (mobile) and persistent sidebar (desktop)
- Skip link to content, `theme-color`, `:focus-visible` styles
- Tables stay in normal layout on small screens (`Breakpoint.None`) with horizontal scroll
- Calendar scrolls horizontally on narrow screens

### 5. AI features

**Architecture**

- `IAiService` in the API — separated from the Blazor app
- **`MockAiService`** (default) — works without an external API key
- **`OpenAiAiService`** (optional) — selected via configuration
- Secret key is **not** stored in the Blazor project or Git repository

**Configuration** (`SigurnaDob.Api/appsettings.json`):

```json
"Ai": {
  "Provider": "Mock",
  "Model": "gpt-4o-mini",
  "BaseUrl": "https://api.openai.com/v1"
}
```

For the real OpenAI provider (optional):

```bash
dotnet user-secrets set "Ai:Provider" "OpenAI" --project SigurnaDob.Api
dotnet user-secrets set "Ai:ApiKey" "sk-..." --project SigurnaDob.Api
```

**API**

| Endpoint | Description | Who |
|----------|-------------|-----|
| `GET /api/ai/summary` | Text summary of open tasks and pending visit requests | Coordinator/admin |
| `POST /api/ai/care-task-suggestion` | Structured care task suggestion from a short note | Coordinator/admin |

**UI**

- **Home** (`/`) — “AI summary” panel with a “Generate summary” button
- **New care task** (`/care-tasks/create`) — AI assistant: enter note → generate → review → apply/discard → edit manually → save through existing `POST /api/caretasks`

**Example note for a suggestion:**

```
Mara treba terapiju sutra ujutro, dodijeli Ivanu
```

Mock AI recognizes the resident (Mara), task type (Terapija), caregiver (Ivan), and due date (tomorrow).
