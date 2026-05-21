export type Quarter = 'Q1' | 'Q2' | 'Q3' | 'Q4'
export type MatchType = 'OneVsOneVsOne' | 'OneVsOneVsOneVsOne' | 'FivePlayerSheriff' | 'SixPlayerSheriff'
export type HiddenRole = 'Sheriff' | 'Deputy' | 'Red' | 'Renegade' | 'Matriarch' | 'Outlaw'

// Auth
export interface AuthUser {
  id: number
  username: string
  isAdmin: boolean
}

export interface LoginDto {
  username: string
  password: string
}

export interface AuthResponseDto {
  token: string
  user: AuthUser
}

export interface PendingUserDto {
  id: number
  username: string
  email: string
  createdAt: string
}

export interface UserDto {
  id: number
  username: string
  email: string
  currentPrestigeLevel: number
  createdAt: string
  isApproved: boolean
  isAdmin: boolean
  isGuest: boolean
}

export interface UserWithStatsDto extends UserDto {
  totalDecks: number
  totalWins: number
  totalLosses: number
  winRate: number
}

export interface DeckDto {
  id: number
  userId: number
  deckName: string
  commanderName: string
  commanderImageUri?: string | null
  scryfallId?: string | null
  colorIdentity?: string
  isActive: boolean
  createdAt: string
}

export interface ScryfallAutocompleteDto {
  names: string[]
  totalValues: number
}

export interface ScryfallCardDto {
  scryfallId: string
  name: string
  imageUri: string
  manaCost?: string
  colorIdentity: string[]
  typeLine?: string
  oracleText?: string
}

export interface ScryfallSymbolDto {
  symbol: string
  svgUri?: string
  svg_uri?: string
  description?: string
  cmc?: number
  appearsInManaCosts?: boolean
  colors?: string[]
}

export interface ScryfallSymbologyDto {
  symbols: ScryfallSymbolDto[]
}

export interface MatchWinnerDto {
  userId: number
  username: string
}

export interface MatchParticipantDetailDto {
  userId: number
  username: string
  deckId?: number
  deckName?: string
  hiddenRole?: HiddenRole
}

export interface MatchDto {
  id: number
  matchType: MatchType
  matchDate: string
  sheriffUserId?: number
  winners: MatchWinnerDto[]
  participants: MatchParticipantDetailDto[]
}

export interface SeasonDto {
  id: number
  name: string
  year: number
  quarter: Quarter
  startDate: string
  endDate: string
  isActive: boolean
}

export interface UserStandingDto {
  userId: number
  username: string
  prestigeLevel: number
  totalWins: number
  totalLosses: number
  totalMatches: number
  winRate: number
  isGuest?: boolean
}

export interface SeasonStandingsDto {
  season: SeasonDto
  standings: UserStandingDto[]
}

export interface UpdateSeasonDto {
  name: string
  startDate: string
  endDate: string
}

export interface UpsertSeasonRecordDto {
  userId: number
  totalWins: number
  totalLosses: number
}

export interface UserStatsDto {
  userId: number
  username: string
  seasonId: number
  seasonName: string
  totalWins: number
  totalLosses: number
  totalMatches: number
  winRate: number
  wins1v1v1: number
  wins1v1v1v1: number
  winsSheriff: number
  sheriffGamesPlayed: number
  sheriffGamesWon: number
  deputyGamesPlayed: number
  deputyGamesWon: number
  redGamesPlayed: number
  redGamesWon: number
  prestigeLevel: number
}

export interface BetrayalDto {
  id: number
  betrayerUserId: number
  betrayerUsername: string
  victimUserId: number
  victimUsername: string
  description: string
  betrayalDate: string
  createdAt: string
}

export interface CreateUserDto {
  username: string
  email: string
  password: string
}

export interface CreateDeckDto {
  deckName: string
  commanderName: string
  colorIdentity?: string
}

export interface UpdateDeckDto {
  deckName: string
  commanderName: string
  colorIdentity?: string
}

export interface MatchParticipantInput {
  userId: number
  deckId?: number
  hiddenRole?: number
}

export interface CreateMatchDto {
  matchType: number
  matchDate: string
  participants: MatchParticipantInput[]
  winnerUserIds: number[]
}

export interface CreateBetrayalDto {
  betrayerUserId: number
  victimUserId: number
  description: string
  betrayalDate: string
}

export interface CreateGuestDto {
  displayName: string
}

