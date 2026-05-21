import axios from 'axios'
import type {
  UserDto, UserWithStatsDto, DeckDto, MatchDto, SeasonDto,
  SeasonStandingsDto, UserStandingDto, UserStatsDto, BetrayalDto,
  CreateUserDto, CreateDeckDto, CreateMatchDto, CreateBetrayalDto,
  LoginDto, AuthResponseDto, PendingUserDto, CreateGuestDto
} from './types'

const apiBaseUrl = (() => {
  const configuredUrl = import.meta.env.VITE_API_URL?.trim()
  if (!configuredUrl) {
    if (import.meta.env.PROD) {
      console.warn('VITE_API_URL is not set. The UI will fall back to /api, which usually requires a matching reverse proxy or rewrite.')
    }
    return '/api'
  }

  try {
    const parsed = new URL(configuredUrl, window.location.origin)
    if (parsed.pathname === '/' || parsed.pathname === '') {
      parsed.pathname = '/api'
      return parsed.toString().replace(/\/$/, '')
    }
    return configuredUrl
  } catch {
    return configuredUrl.startsWith('/') ? configuredUrl : `/${configuredUrl.replace(/^\/+/, '')}`
  }
})()

const api = axios.create({ baseURL: apiBaseUrl })

// Attach JWT to every request
api.interceptors.request.use(config => {
  const token = localStorage.getItem('gm_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Redirect to login on 401
api.interceptors.response.use(
  r => r,
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('gm_token')
      localStorage.removeItem('gm_user')
      window.location.href = '/login'
    }
    return Promise.reject(err)
  }
)

// Auth
export const loginUser = (dto: LoginDto) =>
  api.post<AuthResponseDto>('/auth/login', dto).then(r => r.data)

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

// Admin
export const getPendingUsers = () =>
  api.get<PendingUserDto[]>('/users/pending').then(r => r.data)
export const approveUser = (id: number) =>
  api.post<UserDto>(`/users/${id}/approve`).then(r => r.data)
export const rejectUser = (id: number) =>
  api.delete(`/users/${id}/reject`).then(r => r.data)
export const createGuest = (dto: CreateGuestDto) =>
  api.post<UserDto>('/users/guest', dto).then(r => r.data)
export const getGuests = () =>
  api.get<UserDto[]>('/users/guests').then(r => r.data)
export const deleteUser = (id: number) =>
  api.delete(`/users/${id}`).then(r => r.data)

