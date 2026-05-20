# Quick Start Guide - GarageMagic

## 🚀 Getting Started

### Database is Ready
✅ Database created: `garagemagic.db`
✅ All tables created with schema
✅ Indexes and constraints applied

### Running the Application

```bash
cd C:\Users\Mike\RiderProjects\WebApplication1\GarageMagicCore
dotnet run
```

The application will:
1. Start on https://localhost:xxxx (port shown in console)
2. Automatically seed default settings
3. Create initial season (Q2 2026)
4. Open Swagger UI at `/swagger`

### Database Location
- **File**: `C:\Users\Mike\RiderProjects\WebApplication1\GarageMagicCore\garagemagic.db`
- **Connection String**: `Data Source=garagemagic.db`

## 📊 What's Been Seeded

### AppSettings Table
| SettingKey | SettingValue |
|------------|--------------|
| WinsPerPrestigeLevel | 5 |
| CurrentSeasonId | (auto-generated) |

### Seasons Table
| Name | Year | Quarter | StartDate | EndDate | IsActive |
|------|------|---------|-----------|---------|----------|
| 2026 Q2 | 2026 | Q2 | 2026-04-01 | 2026-06-30 | true |

## 🔧 EF Core Commands

### View current migration status
```bash
dotnet ef migrations list
```

### Create new migration
```bash
dotnet ef migrations add MigrationName
```

### Apply migrations
```bash
dotnet ef database update
```

### Remove last migration (if not applied)
```bash
dotnet ef migrations remove
```

### Reset database (WARNING: deletes all data)
```bash
dotnet ef database drop --force
dotnet ef database update
```

## 📦 What's Implemented

✅ **10 Domain Models** - All entities with proper relationships
✅ **10 EF Core Configurations** - Complete database schema
✅ **Database Context** - With automatic timestamp management
✅ **Database Seeding** - Initial data on startup
✅ **4 DTO Sets** - User, Deck, Match, Season
✅ **3 Validator Sets** - FluentValidation rules
✅ **Initial Migration** - Applied and working

## 🎯 What's Next

### Phase 2: Services (when requested)
- UserService
- DeckService
- MatchService
- SeasonService
- StatsService
- BetrayalService

### Phase 3: Controllers (when requested)
- UsersController
- DecksController
- MatchesController
- SeasonsController
- StatsController
- BetrayalsController

## 🧪 Testing the Database

You can verify the database using any SQLite viewer:
- DB Browser for SQLite (https://sqlitebrowser.org/)
- VS Code SQLite extension
- Azure Data Studio with SQLite extension

### Sample Queries

```sql
-- View all tables
SELECT name FROM sqlite_master WHERE type='table';

-- Check seeded data
SELECT * FROM AppSettings;
SELECT * FROM Seasons;

-- Verify indexes
SELECT name FROM sqlite_master WHERE type='index';
```

## 📝 Key Files

| File | Purpose |
|------|---------|
| `GarageMagicDbContext.cs` | Main database context |
| `DbSeeder.cs` | Seed initial data |
| `Program.cs` | Application configuration |
| `appsettings.json` | Connection strings and settings |
| `README.md` | Full documentation |
| `SCHEMA.md` | Database schema details |
| `IMPLEMENTATION_SUMMARY.md` | What's been completed |

## 🔍 Swagger UI

Once running, navigate to `/swagger` to see:
- No endpoints yet (controllers not implemented)
- API documentation structure
- Schema definitions

## ⚠️ Important Notes

1. **Password Hashing**: Validators require password rules, but hashing not yet implemented. Add BCrypt when implementing UserService.

2. **Authentication**: No auth configured yet. Add JWT when implementing UserService.

3. **Validation**: FluentValidation is configured but controllers aren't created yet.

4. **Timestamps**: All automatically managed in UTC.

5. **Cascade Deletes**: Be careful when deleting users (will cascade to decks, stats, prestige).

## 🎮 Example Data Flow

When you implement services, a match recording will:
1. Create Match entity
2. Add MatchParticipants for each player
3. Add MatchWinner(s) for winner(s)
4. Update UserStats for all participants
5. Check and update prestige levels
6. Auto-update all timestamps

## 🆘 Troubleshooting

### Migration issues
```bash
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Build errors
```bash
dotnet clean
dotnet restore
dotnet build
```

### Database locked
- Close any SQLite viewers
- Stop the application
- Restart

## 📞 Connection String

Default: `Data Source=garagemagic.db`

To use a different location:
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=C:\\path\\to\\garagemagic.db"
}
```

## ✨ Features Ready to Use

- ✅ Seasonal system with automatic season creation
- ✅ Match type support (1v1v1, 1v1v1v1, Sheriff modes)
- ✅ Multiple winners in Sheriff games
- ✅ Hidden role tracking
- ✅ Prestige level system
- ✅ Comprehensive statistics tracking
- ✅ Betrayal tracking
- ✅ Configurable settings

---

**Status**: Database ready, waiting for services and controllers implementation.
**Current Phase**: Models and EF Core ✅ Complete
**Next Phase**: Services and Controllers (awaiting request)

