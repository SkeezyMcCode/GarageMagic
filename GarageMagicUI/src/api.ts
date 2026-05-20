import axios from 'axios'
import type {
  UserDto, UserWithStatsDto, DeckDto, MatchDto, SeasonDto,
  SeasonStandingsDto, UserStandingDto, UserStatsDto, BetrayalDto,
  CreateUserDto, CreateDeckDto, CreateMatchDto, CreateBetrayalDto
} from './types'

const api = axios.create({ baseURL: '/api' })

// Users
export const registerUser = (dto: CreateUserDto) =>
  api.post<UserDto>('/users/register', dto).then(r => r.data)
export const getUser = (id: number) =>
  api.get<UserDto>(`/users/${id}`).then(r => r.data)
export const getUserWithStats = (id: number) =>
  api.get<UserWithStatsDto>(`/users/${id}/stats`).then(r => r.data)
export const getAllUsers = () =>
  api.get<UserDto[]>('/users').then(r => r.data)

// Decks
export const createDeck = (userId: number, dto: CreateDeckDto) =>
  api.post<DeckDto>(`/decks?userId=${userId}`, dto).then(r => r.data)
export const getDecksByUser = (userId: number) =>
  api.get<DeckDto[]>(`/decks/user/${userId}`).then(r => r.data)
export const deleteDeck = (id: number) =>
  api.delete(`/decks/${id}`)

// Matches
export const createMatch = (dto: CreateMatchDto) =>
  api.post<MatchDto>('/matches', dto).then(r => r.data)
export const getMatchesByUser = (userId: number) =>
  api.get<MatchDto[]>(`/matches/user/${userId}`).then(r => r.data)
export const getMatchesBySeason = (seasonId: number) =>
  api.get<MatchDto[]>(`/matches/season/${seasonId}`).then(r => r.data)

// Seasons
export const getCurrentSeason = () =>
  api.get<SeasonDto>('/seasons/current').then(r => r.data)
export const getAllSeasons = () =>
  api.get<SeasonDto[]>('/seasons').then(r => r.data)
export const getSeasonStandings = (seasonId: number) =>
  api.get<SeasonStandingsDto>(`/seasons/${seasonId}/standings`).then(r => r.data)
export const rolloverSeason = () =>
  api.post<SeasonDto>('/seasons/rollover').then(r => r.data)

// Stats
export const getLeaderboard = (seasonId?: number) =>
  api.get<UserStandingDto[]>('/stats/leaderboard', { params: seasonId ? { seasonId } : {} }).then(r => r.data)
export const getUserStats = (userId: number, seasonId: number) =>
  api.get<UserStatsDto>(`/stats/user/${userId}/season/${seasonId}`).then(r => r.data)

// Betrayals
export const createBetrayal = (dto: CreateBetrayalDto) =>
  api.post<BetrayalDto>('/betrayals', dto).then(r => r.data)
export const getRecentBetrayals = (count = 10) =>
  api.get<BetrayalDto[]>(`/betrayals/recent?count=${count}`).then(r => r.data)
export const getBetrayalsByUser = (userId: number) =>
  api.get<BetrayalDto[]>(`/betrayals/user/${userId}`).then(r => r.data)

