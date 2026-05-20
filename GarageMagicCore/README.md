# GarageMagic - MTG Garage League Tracker

## Project Overview
ASP.NET Core 8 web application for tracking Magic the Gathering games in a garage league with seasonal prestige system, deck tracking, and Sheriff mode support.

## Technology Stack
- **Framework**: ASP.NET Core 8.0
- **Database**: SQLite with Entity Framework Core 8.0
- **Validation**: FluentValidation 11.3
- **API Documentation**: Swagger/OpenAPI

## Database Schema

### Core Entities

#### User
Represents a player in the league.
- **Fields**: Username, Email, PasswordHash, CurrentPrestigeLevel, CreatedAt, UpdatedAt
- **Relationships**:
  - One-to-Many: Decks, Stats, PrestigeLevels
  - Many-to-Many: Matches (via MatchWinner junction table)
  - One-to-Many (self): Betrayals (as betrayer or victim)

#### Deck
Represents a Commander deck owned by a user.
- **Fields**: DeckName, CommanderName, ColorIdentity, IsActive, CreatedAt, UpdatedAt
- **Relationships**:
  - Many-to-One: User
  - One-to-Many: Matches

#### Match
Represents a game played.
- **Fields**: DeckId, MatchType (enum), MatchDate, SheriffUserId, CreatedAt, UpdatedAt
- **Match Types**: 1v1v1, 1v1v1v1, 5-Player Sheriff, 6-Player Sheriff
- **Relationships**:
  - Many-to-One: Deck (optional)
  - Many-to-Many: Winners (Users via MatchWinner)
  - One-to-Many: Participants (via MatchParticipant)

#### MatchWinner
Junction table for multiple winners in Sheriff games.
- **Fields**: MatchId, UserId, CreatedAt
- **Unique Constraint**: (MatchId, UserId)

#### MatchParticipant
Tracks all participants with hidden roles for Sheriff games.
- **Fields**: MatchId, UserId, DeckId, HiddenRole (enum: Sheriff/Deputy/Red)
- **Unique Constraint**: (MatchId, UserId)

#### Season
Represents a quarterly season (4 per year).
- **Fields**: Name, Year, Quarter (Q1-Q4), StartDate, EndDate, IsActive, CreatedAt
- **Unique Constraint**: (Year, Quarter)
- **Relationships**:
  - One-to-Many: UserStats, PrestigeLevels

#### UserStats
Season-specific statistics per user.
- **Fields**: 
  - Overall: TotalWins, TotalLosses, TotalMatches
  - By Match Type: Wins1v1v1, Wins1v1v1v1, WinsSheriff
  - Sheriff Roles: SheriffGamesPlayed/Won, DeputyGamesPlayed/Won, RedGamesPlayed/Won
  - WinsPerDeckJson (JSON field for deck performance)
- **Unique Constraint**: (UserId, SeasonId)

#### PrestigeLevel
Tracks prestige level achievements per season.
- **Fields**: UserId, SeasonId, Level, AchievedAt
- **Note**: Prestige increments every X wins (configurable via AppSettings)

#### Betrayal
Records notable betrayals during games.
- **Fields**: BetrayerUserId, VictimUserId, Description, BetrayalDate, CreatedAt

#### AppSettings
Stores application configuration.
- **Fields**: SettingKey, SettingValue, UpdatedAt
- **Default Settings**:
  - `WinsPerPrestigeLevel`: Number of wins needed per prestige level (default: 5)
  - `CurrentSeasonId`: Active season ID

## Key Features Implemented

### 1. Seasonal System
- 4 seasons per year (Q1-Q4)
- Each season has start/end dates
- Only one active season at a time
- Stats and prestige are per-season

### 2. Prestige System
- Configurable wins per prestige level
- Tracks prestige level history
- Resets at season rollover
- Current prestige level stored on User entity

### 3. Sheriff Mode Support
- Multiple winners allowed (via MatchWinner junction table)
- Hidden roles tracked (Sheriff, Deputy, Red)
- Sheriff-specific statistics
- Role-based performance tracking

### 4. Statistics Tracking
- Total wins/losses per season
- Match type breakdown
- Sheriff game performance by role
- Deck performance (JSON field for flexibility)

### 5. Match Tracking
- Supports 3-6 player games
- Optional deck tracking
- Multiple match types
- Participant tracking with roles

## Database Seeding

The application automatically seeds initial data on startup:
- Default AppSettings (WinsPerPrestigeLevel = 5)
- Initial season based on current date

## EF Core Migrations

### Initial Migration Created
Run `dotnet ef database update` to apply migrations to the database.

### Migration Commands
```bash
# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove
```

## Project Structure

```
GarageMagicCore/
├── Models/                    # Domain entities
│   ├── User.cs
│   ├── Deck.cs
│   ├── Match.cs
│   ├── MatchWinner.cs
│   ├── MatchParticipant.cs
│   ├── Season.cs
│   ├── UserStats.cs
│   ├── PrestigeLevel.cs
│   ├── Betrayal.cs
│   └── AppSettings.cs
├── Data/                      # Database context and configurations
│   ├── GarageMagicDbContext.cs
│   ├── DbSeeder.cs
│   └── EntityConfigurations/
│       ├── UserConfiguration.cs
│       ├── DeckConfiguration.cs
│       ├── MatchConfiguration.cs
│       ├── MatchWinnerConfiguration.cs
│       ├── MatchParticipantConfiguration.cs
│       ├── SeasonConfiguration.cs
│       ├── UserStatsConfiguration.cs
│       ├── PrestigeLevelConfiguration.cs
│       ├── BetrayalConfiguration.cs
│       └── AppSettingsConfiguration.cs
├── Migrations/                # EF Core migrations
└── appsettings.json          # Configuration
```

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=garagemagic.db"
  },
  "AppSettings": {
    "WinsPerPrestigeLevel": 5
  }
}
```

## Next Steps

### Services to Implement
1. **UserService**: User registration, authentication, profile management
2. **DeckService**: CRUD operations for decks
3. **MatchService**: Record matches, update stats, calculate prestige
4. **SeasonService**: Season management, rollover logic
5. **StatsService**: Query and aggregate statistics
6. **BetrayalService**: Record betrayals

### Controllers to Implement
1. **UsersController**: `/api/users`
2. **DecksController**: `/api/decks`
3. **MatchesController**: `/api/matches`
4. **SeasonsController**: `/api/seasons`
5. **StatsController**: `/api/stats`
6. **BetrayalsController**: `/api/betrayals`

### DTOs to Create
- User: CreateUserDto, UpdateUserDto, UserDto, UserStatsDto
- Deck: CreateDeckDto, UpdateDeckDto, DeckDto
- Match: CreateMatchDto, MatchDto, MatchResultDto
- Season: CreateSeasonDto, SeasonDto, SeasonStandingsDto
- Stats: UserStatsDto, DeckStatsDto
- Betrayal: CreateBetrayalDto, BetrayalDto

### Validation to Add
- FluentValidation rules for all DTOs
- Business logic validation (e.g., season dates, match participants)

## Development Workflow

1. **Run the application**:
   ```bash
   dotnet run
   ```

2. **Access Swagger UI**: https://localhost:xxxx/swagger

3. **Database file**: `garagemagic.db` (created in project root)

## Design Decisions

### Why Junction Tables?
- **MatchWinner**: Explicit junction table allows for Sheriff games with multiple winners and provides flexibility for future metadata (e.g., points earned).
- **MatchParticipant**: Needed to track hidden roles and individual deck usage per participant.

### Why Separate UserStats?
- Season-based stats allow archival and historical comparison
- Denormalized for query performance
- JSON field for flexible deck performance tracking

### Why Prestige on User Entity?
- CurrentPrestigeLevel stored on User for quick access
- PrestigeLevel entity maintains historical achievement timestamps

### Timestamps
- CreatedAt/UpdatedAt automatically managed by DbContext.SaveChangesAsync override
- All timestamps stored as UTC

## Admin Features (To Implement)

- Edit match history
- Trigger season resets
- Manage app settings
- View audit logs

## Authentication (To Implement)

Consider implementing:
- JWT authentication
- Role-based authorization (Admin, Player)
- Password hashing (BCrypt recommended)

