# Entity Relationship Diagram

## Visual Schema Overview

```
┌─────────────────┐
│     User        │
├─────────────────┤
│ + Id            │◄──────┐
│ + Username      │       │
│ + Email         │       │
│ + PasswordHash  │       │
│ + PrestigeLevel │       │
└────────┬────────┘       │
         │                │
         │ 1              │
         │                │
         │ *              │
┌────────▼────────┐       │
│     Deck        │       │
├─────────────────┤       │
│ + Id            │       │
│ + UserId        │       │
│ + DeckName      │       │
│ + CommanderName │       │
│ + ColorIdentity │       │
└────────┬────────┘       │
         │                │
         │ 1              │
         │                │
         │ *              │ *
┌────────▼────────┐       │
│     Match       │       │
├─────────────────┤       │
│ + Id            │       │
│ + DeckId        │       │
│ + MatchType     │       │
│ + MatchDate     │       │
│ + SheriffUserId ├───────┘
└────────┬────────┘
         │
         ├──────────┐
         │ 1        │ 1
         │          │
         │ *        │ *
┌────────▼────────┐ ┌──────────────────┐
│  MatchWinner    │ │ MatchParticipant │
├─────────────────┤ ├──────────────────┤
│ + Id            │ │ + Id             │
│ + MatchId       │ │ + MatchId        │
│ + UserId        │ │ + UserId         │
│                 │ │ + DeckId         │
│                 │ │ + HiddenRole     │
└─────────────────┘ └──────────────────┘

┌─────────────────┐       ┌──────────────────┐
│    Season       │       │    UserStats     │
├─────────────────┤       ├──────────────────┤
│ + Id            │◄──┐   │ + Id             │
│ + Name          │ 1 │   │ + UserId         │
│ + Year          │   │ * │ + SeasonId       │
│ + Quarter       │   └───┤ + TotalWins      │
│ + StartDate     │       │ + TotalLosses    │
│ + EndDate       │       │ + ...            │
│ + IsActive      │       └──────────────────┘
└────────┬────────┘
         │
         │ 1
         │
         │ *
┌────────▼────────┐
│ PrestigeLevel   │
├─────────────────┤
│ + Id            │
│ + UserId        │
│ + SeasonId      │
│ + Level         │
│ + AchievedAt    │
└─────────────────┘

┌─────────────────┐
│    Betrayal     │
├─────────────────┤
│ + Id            │
│ + BetrayerUserId│─────► User (Betrayer)
│ + VictimUserId  │─────► User (Victim)
│ + Description   │
│ + BetrayalDate  │
└─────────────────┘

┌─────────────────┐
│  AppSettings    │
├─────────────────┤
│ + Id            │
│ + SettingKey    │
│ + SettingValue  │
│ + UpdatedAt     │
└─────────────────┘
```

## Relationship Summary

### User Relationships
- **User → Deck**: One-to-Many (A user can have multiple decks)
- **User → MatchWinner**: One-to-Many (A user can win multiple matches)
- **User → UserStats**: One-to-Many (A user has stats per season)
- **User → PrestigeLevel**: One-to-Many (A user has prestige history per season)
- **User → Betrayal**: One-to-Many (As betrayer or victim)

### Match Relationships
- **Match → Deck**: Many-to-One (Optional - primary deck for the match)
- **Match → User** (Sheriff): Many-to-One (Optional - the sheriff in Sheriff games)
- **Match → MatchWinner**: One-to-Many (Multiple winners allowed)
- **Match → MatchParticipant**: One-to-Many (All participants)

### Season Relationships
- **Season → UserStats**: One-to-Many (Stats per user per season)
- **Season → PrestigeLevel**: One-to-Many (Prestige achievements per user per season)

## Unique Constraints

1. **User**: Username, Email (unique)
2. **Season**: (Year, Quarter) composite unique
3. **UserStats**: (UserId, SeasonId) composite unique
4. **MatchWinner**: (MatchId, UserId) composite unique
5. **MatchParticipant**: (MatchId, UserId) composite unique
6. **AppSettings**: SettingKey (unique)

## Cascade Delete Behavior

### CASCADE
- User → Decks, Stats, PrestigeLevels
- Match → MatchWinners, MatchParticipants
- Season → UserStats, PrestigeLevels

### SET NULL
- Match → Deck
- Match → SheriffUser
- MatchParticipant → Deck

### RESTRICT
- MatchWinner → User
- MatchParticipant → User
- Betrayal → User (both betrayer and victim)

## Indexes

### User
- Username (unique)
- Email (unique)

### Deck
- (UserId, DeckName) composite

### Match
- MatchDate
- MatchType

### Season
- (Year, Quarter) unique
- IsActive

### UserStats
- (UserId, SeasonId) unique

### PrestigeLevel
- (UserId, SeasonId, Level) composite

### Betrayal
- BetrayerUserId
- VictimUserId
- BetrayalDate

### AppSettings
- SettingKey (unique)

## Enumerations

### MatchType
```csharp
public enum MatchType
{
    OneVsOneVsOne,      // 3 players
    OneVsOneVsOneVsOne, // 4 players
    FivePlayerSheriff,  // 5 players with Sheriff mode
    SixPlayerSheriff    // 6 players with Sheriff mode
}
```

### HiddenRole
```csharp
public enum HiddenRole
{
    Sheriff,  // The sheriff (revealed)
    Deputy,   // Helps the sheriff
    Red       // Outlaws/Renegades
}
```

### Quarter
```csharp
public enum Quarter
{
    Q1,  // Jan-Mar
    Q2,  // Apr-Jun
    Q3,  // Jul-Sep
    Q4   // Oct-Dec
}
```

## Data Flow Example: Recording a Match

1. **Create Match** with MatchType and MatchDate
2. **Add MatchParticipants** for each player (with Deck and HiddenRole if applicable)
3. **Add MatchWinner(s)** for the winning player(s)
4. **Update UserStats** for all participants (wins/losses/role-specific stats)
5. **Check Prestige** - if user reached new prestige level, create PrestigeLevel record
6. **Update User.CurrentPrestigeLevel** if increased

## Query Patterns

### Get All User Matches
```csharp
var userMatches = await context.MatchWinners
    .Where(mw => mw.UserId == userId)
    .Include(mw => mw.Match)
        .ThenInclude(m => m.Participants)
    .ToListAsync();
```

### Get Season Standings
```csharp
var standings = await context.UserStats
    .Where(us => us.SeasonId == seasonId)
    .Include(us => us.User)
    .OrderByDescending(us => us.TotalWins)
    .ToListAsync();
```

### Get User's Deck Performance
```csharp
var deckWins = await context.MatchWinners
    .Where(mw => mw.UserId == userId && mw.Match.DeckId == deckId)
    .CountAsync();
```

### Get Sheriff Game Stats
```csharp
var sheriffStats = await context.MatchParticipants
    .Where(mp => mp.UserId == userId && mp.HiddenRole == HiddenRole.Sheriff)
    .GroupBy(mp => mp.Match.Id)
    .Select(g => new {
        MatchId = g.Key,
        Won = g.Any(mp => context.MatchWinners
            .Any(mw => mw.MatchId == mp.MatchId && mw.UserId == userId))
    })
    .ToListAsync();
```

