export interface AuthUser {
  id: number
  username: string
  isAdmin: boolean
}

export interface AuthContextType {
  user: AuthUser | null
  token: string | null
  isLoading: boolean
  login: (token: string, user: AuthUser) => void
  logout: () => void
}

