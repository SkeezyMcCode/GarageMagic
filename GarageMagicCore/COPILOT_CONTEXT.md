# GarageMagic Copilot Context (Shared)

Use this file as the single source of truth for AI-assisted work across frontend and backend.
Copy the "Prompt Starter" section into a new chat before starting any task.

Last updated: 2026-05-23

---

## 1) Workspace Topology

### Repositories
- **Frontend**
  - Name: `GarageMagicUI`
  - Path (default): `C:\Users\Mike\RiderProjects\GarageMagicUI`
  - Stack: React + TypeScript + Vite
- **Backend**
  - Name: `GarageMagicCore`
  - Path (default): `C:\Users\Mike\RiderProjects\GarageMagicCore`
  - Stack: .NET 8 + ASP.NET Core Web API + EF Core + SQLite

### Active Branches (fill per session)
- Frontend branch: `<branch-name>`
- Backend branch: `<branch-name>`

---

## 2) Backend System Facts (GarageMagicCore)

### Infrastructure
- Runtime: .NET 8
- Framework: ASP.NET Core Web API
- ORM: Entity Framework Core
- Database: SQLite (`garagemagic.db`)
- Validation: FluentValidation
- Auth: JWT Bearer tokens
- Roles:
  - `Admin` — restricted operations (delete, patch-restricted endpoints)
  - Standard authenticated users — general gameplay endpoints
- Reverse proxy: nginx (`nginx.conf`)
- Hosting: https://garage-mtg.fun

### Backend Project Structure
| Folder | Purpose |
|--------|---------|
| `Controllers/` | API endpoints — Auth, Betrayals, Decks, Matches, Scryfall, Seasons, Stats, Users |
| `Services/` | Business logic — interface + implementation pairs |
| `Models/` | EF Core entity models |
| `DTOs/` | Request/response contracts, organized by feature |
| `Data/` | `GarageMagicDbContext`, `DbSeeder`, entity configurations |
| `Migrations/` | EF Core migration history |
| `Validators/` | FluentValidation validators for Deck, Match, User |

### Domain Models
- `User` — players/guests + auth fields
- `Season` — date-ranged seasons with standings/records
- `Match` — game results with `MatchParticipant` and `MatchWinner`
- `Deck` — player decks with Scryfall metadata
- `Betrayal` — tracks betrayals between players (`betrayer -> victim`)
- `AppSettings` — key/value config stored in DB
- `UserStats` — aggregated stats per user
- `PrestigeLevel` — prestige tier definitions

### Key API Routes
| Method | Route | Auth |
|--------|-------|------|
| `POST` | `/api/auth/login` | Public |
| `GET` | `/api/seasons/current` | Authenticated |
| `PATCH` | `/api/seasons/{id}` | Admin |
| `PUT` | `/api/seasons/{seasonId}/records/{userId}` | Admin |
| `POST` | `/api/seasons/rollover` | Authenticated |
| `POST` | `/api/matches` | Authenticated |
| `DELETE` | `/api/matches/{id}` | Authenticated |
| `POST` | `/api/betrayals` | Authenticated |
| `DELETE` | `/api/betrayals/{id}` | Admin |
| `GET` | `/api/matches/sheriff-roles` | Public |
| `GET`/`POST` | `/api/decks` | Authenticated |
| `GET` | `/api/stats` | Authenticated |
| `GET` | `/api/scryfall/...` | Authenticated |

### Game Constants
- Sheriff roles: `Sheriff`, `Deputy`, `Outlaw` (×2), `Renegade` (6-player only), `Matriarch`

---

## 3) Frontend System Facts (GarageMagicUI)

### Key Files
| File | Purpose |
|------|---------|
| `src/api.ts` | All API calls to backend |
| `src/types.ts` | Shared TypeScript contract types |
| `src/context/AuthContext.tsx` | Auth context provider |
| `src/context/useAuth.ts` | Auth hook |
| `src/context/authTypes.ts` | Auth-related TypeScript types |
| `src/context/AuthContextValue.ts` | Auth context value shape |

### Important Pages
| File | Purpose |
|------|---------|
| `src/pages/RecordMatch.tsx` | Submit new match results |
| `src/pages/Matches.tsx` | Match history view |
| `src/pages/Seasons.tsx` | Season management |
| `src/pages/Players.tsx` | Player list |
| `src/pages/PlayerDetail.tsx` | Individual player stats |
| `src/pages/AdminPanel.tsx` | Admin-only management |
| `src/pages/Dashboard.tsx` | Main landing dashboard |
| `src/pages/Betrayals.tsx` | Betrayal tracking |
| `src/pages/Login.tsx` | Login page |

### Frontend/Backend Contract Rule
- Backend DTOs and FluentValidation are authoritative for shape and validation rules.
- `src/types.ts` must stay aligned with backend DTOs.
- `src/api.ts` must stay aligned with backend routes, HTTP methods, and auth requirements.
- When a backend DTO or route changes, assume both `src/api.ts` and `src/types.ts` need updates.

---

## 4) Integration Rules

1. **Default to full-stack** — treat tasks as touching both repos unless explicitly told otherwise.
2. **For any route/DTO/auth change** in backend: update FE `src/api.ts` + `src/types.ts` together.
3. **Do not weaken role-based access** — preserve `Admin` vs authenticated enforcement.
4. **Prefer backward-compatible API changes** unless a breaking change is explicitly intended.
5. **Surface backend validation errors** as field-level errors in the UI where possible.
6. **Log contract drift** in the Delta Log below — don't let route/DTO changes go undocumented.

---

## 5) Environment (fill current values)

- Frontend dev URL: `http://localhost:5173`
- Backend dev URL: `<e.g., https://localhost:5001>`
- API base URL used by frontend (in `src/api.ts` or `.env`): `<value>`
- Auth header format: `Authorization: Bearer <token>`
- CORS notes: `<origins, credentials: true/false>`

---

## 6) Prompt Starter (copy into chat at the start of every task)

```md
Use `COPILOT_CONTEXT.md` as source of truth for both repos.

Task:
- Scope: <frontend | backend | full-stack>
- Goal: <one sentence>
- Affected files (known):
  - FE: <paths>
  - BE: <paths>
- Constraints:
  - Preserve auth/roles
  - Keep API backward compatible: <yes/no>
- Done when:
  - [ ] <criterion 1>
  - [ ] <criterion 2>

Instructions to assistant:
1. Identify all FE + BE files affected.
2. Flag any contract mismatches explicitly.
3. Propose coordinated changes in both repos.
4. Preserve all auth/role enforcement.
5. Provide a post-change verification checklist.
```

---

## 7) Delta Log (append as changes happen — never delete old entries)

| Date | What changed |
|------|-------------|
| 2026-05-23 | Context pack V1 created |

---

## 8) Local Override

For machine-specific values (ports, local paths, dev tokens), create a local file:
- `COPILOT_CONTEXT.local.md` in either repo root

This file is gitignored (`*.local` pattern) and will never be committed.
Use it to override Section 5 values without touching this shared file.

