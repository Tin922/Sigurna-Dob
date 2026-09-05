# Sigurna dob — finalni projekt

Blazor Server aplikacija za upravljanje domom za starije osobe: korisnici, sobe, zadaci skrbi, posjeti, aktivnosti, korisnički računi i nadzorna ploča.

**Stack:** .NET 10, EF Core (SQLite), JWT, MudBlazor, Swagger

---

## Struktura rješenja

```
SigurnaDob.slnx
├── SigurnaDob.Api      — REST API, autentifikacija, migracije
├── SigurnaDob.App      — Blazor Server UI (MudBlazor)
└── SigurnaDob.Shared   — modeli, DTO-ovi, konstante
```

---

## Preduvjeti

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- (Preporučeno) JetBrains Rider ili Visual Studio 2022
- EF alat (jednom po stroju):

```bash
dotnet tool restore
```

---

## Pokretanje

### Rider

1. Otvori `SigurnaDob.slnx`
2. Pokreni **Multi-Launch** ili oba projekta s profilom **https**
3. Otvori Blazor aplikaciju: `https://localhost:7096`
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

| Servis | HTTPS | HTTP |
|--------|-------|------|
| **API** | `7210` | `5088` |
| **App** | `7096` | `5064` |

Pri prvom pokretanju API automatski primjenjuje migracije i seed podatke (`DemoDataSeeder`, `AppUserSeeder`).

---

## Demo korisnici

| Uloga | Email | Lozinka |
|-------|-------|---------|
| Admin | `admin@sigurna-dob.local` | `Admin123!` |
| Koordinator | `coordinator@sigurna-dob.local` | `Coordinator123!` |
| Njegovatelj | `caregiver@sigurna-dob.local` | `Caregiver123!` |
| Član obitelji | `family@sigurna-dob.local` | `Family123!` |

Login u aplikaciji: `/login`  
Login u Swaggeru: `POST /api/auth/login` → kopiraj `accessToken` u **Authorize**.

---

## Moduli

| Modul | API | Blazor stranice | Tko vidi / uređuje |
|-------|-----|-----------------|---------------------|
| Autentifikacija | `AuthController` | `/login` | Svi (login); `/api/auth/me` za prijavljenog |
| Korisnici doma | `ResidentsController` | `/residents`, profil, create/edit | Staff pregled; koordinator/admin uređuje |
| Sobe | `RoomsController` | `/rooms`, create/edit | Staff pregled; koordinator/admin uređuje |
| Obiteljski kontakti | `FamilyContactsController` | create/edit s profila korisnika | Staff pregled; koordinator/admin uređuje |
| Zadaci skrbi | `CareTasksController` | `/care-tasks`, `/my-care-tasks` | Staff/koordinator upravlja; njegovatelj vidi svoje |
| Zahtjevi za posjet | `VisitRequestsController` | `/visit-requests`, `/my-visit-requests` | Koordinator odlučuje; obitelj šalje svoje |
| Aktivnosti | `ActivitiesController` | `/activities`, create/edit | Koordinator/admin |
| Korisnički računi | `UsersController` | `/users`, create/edit | Samo admin |
| Mediji korisnika | `ResidentMediaController` | upload na profilu | Staff pregled; koordinator/admin upload/brisanje |
| Nadzorna ploča | `DashboardController` | `/` (Home) | Operativno: admin/koordinator; osobno: njegovatelj/obitelj |
| Kalendar | `CalendarController` | `/calendar` | Staff i obitelj (obitelj vidi svoje posjete) |
| Popunjenost soba | `RoomsController` | `/room-occupancy` | Staff |
| Opterećenje njegovatelja | `CareTasksController` | `/caregiver-workload` | Koordinator/admin (svi); njegovatelj (svoje) |
| AI pomoćnik | `AiController` | `/` (sažetak), `/care-tasks/create` (prijedlog) | Koordinator/admin |
| Lookups | `LookupsController` | — (koriste forme) | Staff |

---

## Autorizacijska pravila (sažetak)

- **Staff** — Admin, Coordinator, Caregiver
- **CoordinatorOrAdmin** — Admin ili Coordinator
- **AdminOnly** — samo Admin
- **FamilyMember** — za `/api/VisitRequests/mine`
- **Caregiver** — za `/api/CareTasks/mine` i izvršavanje zadataka
- Većina API ruta zahtijeva JWT (`FallbackPolicy`)

---

## Autorizacijski testovi

Testirano na `http://localhost:5088` (Development, rujan 2026).

| # | URL | Korisnik | Očekivano | Stvarno | ✓ |
|---|-----|----------|-----------|---------|---|
| 1 | `GET /api/health` | — (anonimno) | 200 | 200 | ✓ |
| 2 | `GET /api/residents` | — (anonimno) | 401 | 401 | ✓ |
| 3 | `GET /api/residents` | admin | 200 | 200 | ✓ |
| 4 | `GET /api/users` | admin | 200 | 200 | ✓ |
| 5 | `GET /api/users` | caregiver | 403 | 403 | ✓ |
| 6 | `GET /api/CareTasks/mine` | family | 403 | 403 | ✓ |
| 7 | `GET /api/CareTasks/mine` | caregiver | 200 | 200 | ✓ |
| 8 | `GET /api/dashboard` | family | 200 | 200 | ✓ |
| 9 | `POST /api/rooms` | caregiver | 403 | 403 | ✓ |
| 10 | `POST /api/VisitRequests/mine` | family | 201 | 201 | ✓ |
| 11 | `GET /api/residents` | nevažeći token | 401 | 401 | ✓ |

**Pokriveno:** 200, 201, 401, 403 (više od 8 testova, kako traži završni zadatak).

### AI testovi (rujan 2026)

| # | Test | Očekivano | Stvarno | ✓ |
|---|------|-----------|---------|---|
| 1 | `GET /api/ai/summary` bez tokena | 401 | 401 | ✓ |
| 2 | `GET /api/ai/summary` kao caregiver | 403 | 403 | ✓ |
| 3 | `GET /api/ai/summary` kao family | 403 | 403 | ✓ |
| 4 | `GET /api/ai/summary` kao coordinator | 200, Provider: Mock | 200 | ✓ |
| 5 | `POST /api/ai/care-task-suggestion` prazna bilješka | 400 | 400 | ✓ |
| 6 | Prijedlog: terapija / Mara / Ivan | Terapija, dueAt | 200 | ✓ |
| 7 | Prijedlog: prehrana | Prehrana | 200 | ✓ |
| 8 | Prijedlog: higijena | Higijena | 200 | ✓ |
| 9 | Spremi prijedlog kroz `POST /api/caretasks` | 201 | 201 | ✓ |

**Primjer — login i zaštićeni poziv:**

```bash
TOKEN=$(curl -s -X POST http://localhost:5088/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@sigurna-dob.local","password":"Admin123!"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])")

curl -s -o /dev/null -w "%{http_code}\n" \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5088/api/residents
```

**Primjer — AI sažetak:**

```bash
TOKEN=$(curl -sk -X POST https://localhost:7210/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"coordinator@sigurna-dob.local","password":"Coordinator123!"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])")

curl -sk https://localhost:7210/api/ai/summary \
  -H "Authorization: Bearer $TOKEN"
```

---

## Migracije baze

Alat:

```bash
dotnet tool restore
dotnet ef database update --project SigurnaDob.Api --startup-project SigurnaDob.Api
```

### Test migracija (rujan 2026)

| Korak | Rezultat |
|-------|----------|
| **Rebuild solution** | ✓ 0 grešaka, 0 upozorenja |
| **`database update 0`** | ✗ SQLite FK greška pri rollbacku seed podataka (`SeedLookupData`) — očekivano ograničenje SQLite-a |
| **Svježa baza iz migracija** | ✓ Obriši `SigurnaDob.db` (+ `-shm`, `-wal`), pokreni `database update` → obje migracije primijenjene |
| **Seed nakon pokretanja API-ja** | ✓ Demo korisnici i poslovni podaci dostupni nakon prvog `dotnet run` |

**Preporučeni postupak za provjeru:**

```bash
cd SigurnaDob.Api
rm -f SigurnaDob.db SigurnaDob.db-shm SigurnaDob.db-wal
dotnet ef database update
dotnet run --launch-profile http
# Provjeri login admin@sigurna-dob.local / Admin123!
```

Lookup tablice dolaze iz migracije `SeedLookupData`. Korisnički računi i demo poslovni podaci seedaju se pri startu API-ja (`Program.cs`).

---

## Build

```bash
dotnet clean
dotnet build
# Očekivano: 0 Error(s), 0 Warning(s)
```

---

## Git repozitorij

Remote: `git@github.com:Tin922/Sigurna-Dob.git`

---

## Napomene za predaju

- Lokalna SQLite baza (`*.db`) i upload datoteke **nisu** u repozitoriju (`.gitignore`)
- DBML model: `sigurna-dob.dbml` — export slike za dokumentaciju s [dbdiagram.io](https://dbdiagram.io)
- Upute projekta: `Instrucitons.md` (lokalno, nije u gitu)

---

## Bonus funkcionalnosti

### 1. Povijest promjena

- **Povijest statusa korisnika** — `GET /api/residents/{id}/status-history`, prikaz na profilu korisnika
- **Povijest zadatka** — status i dodjela njegovatelja; `GET /api/caretasks/{id}/history`, prikaz na stranici uređivanja zadatka
- Promjene se bilježe pri kreiranju, uređivanju, pokretanju i završetku zadatka te pri promjeni statusa korisnika doma

### 2. Kalendar posjeta i aktivnosti

- `GET /api/calendar?from=...&to=...`
- Stranica `/calendar` — mjesečni prikaz posjeta i aktivnosti
- Staff vidi posjete i aktivnosti; član obitelji vidi svoje planirane posjete
- Klik na dan prikazuje detalje i link na pripadajući modul

### 3. Popunjenost soba i opterećenje njegovatelja

- `GET /api/rooms/occupancy` → `/room-occupancy`
- `GET /api/caretasks/workload` → `/caregiver-workload`
- Koordinator/admin vidi sve njegovatelje; njegovatelj vidi vlastito opterećenje

### 4. Mobilni prikaz i pristupačnost

- Responzivni drawer s hamburger izbornikom (mobitel) i trajno otvorenim izbornikom (desktop)
- Skip link na sadržaj, `theme-color`, `:focus-visible` stilovi
- Tablice ostaju u normalnom prikazu na malim ekranima (`Breakpoint.None`) s horizontalnim scrollom
- Kalendar se horizontalno scrolla na uskim ekranima

### 5. AI funkcionalnosti

**Arhitektura**

- `IAiService` u API-ju — odvojen od Blazor aplikacije
- **`MockAiService`** (zadano) — radi bez vanjskog API ključa
- **`OpenAiAiService`** (opcionalno) — biranje konfiguracijom
- Tajni ključ **nije** u Blazor projektu ni Git repozitoriju

**Konfiguracija** (`SigurnaDob.Api/appsettings.json`):

```json
"Ai": {
  "Provider": "Mock",
  "Model": "gpt-4o-mini",
  "BaseUrl": "https://api.openai.com/v1"
}
```

Za stvarni OpenAI provider (opcionalno):

```bash
dotnet user-secrets set "Ai:Provider" "OpenAI" --project SigurnaDob.Api
dotnet user-secrets set "Ai:ApiKey" "sk-..." --project SigurnaDob.Api
```

**API**

| Endpoint | Opis | Tko |
|----------|------|-----|
| `GET /api/ai/summary` | Tekstualni sažetak otvorenih zadataka i posjeta na odluku | Koordinator/admin |
| `POST /api/ai/care-task-suggestion` | Strukturirani prijedlog zadatka iz kratke bilješke | Koordinator/admin |

**UI**

- **Home** (`/`) — panel „AI sažetak” s gumbom „Generiraj sažetak”
- **Novi zadatak** (`/care-tasks/create`) — AI pomoćnik: unos bilješke → prijedlog → pregled → primijeni/odbaci → ručno uređivanje → spremi kroz postojeći `POST /api/caretasks`

**Primjer bilješke za prijedlog:**

```
Mara treba terapiju sutra ujutro, dodijeli Ivanu
```

Mock AI prepoznaje korisnika (Mara), vrstu (Terapija), njegovatelja (Ivan) i rok (sutra).
