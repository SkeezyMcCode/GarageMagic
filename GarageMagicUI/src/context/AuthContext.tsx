import { useState } from 'react'
import { AuthContext } from './AuthContextValue'
import type { AuthUser } from './authTypes'

function readStoredAuth() {
  const storedToken = localStorage.getItem('gm_token')
  const storedUser = localStorage.getItem('gm_user')

  if (!storedToken || !storedUser) return { token: null, user: null as AuthUser | null }

  try {
    return { token: storedToken, user: JSON.parse(storedUser) as AuthUser }
  } catch {
    localStorage.removeItem('gm_token')
    localStorage.removeItem('gm_user')
    return { token: null, user: null as AuthUser | null }
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const initialAuth = readStoredAuth()
  const [user, setUser] = useState<AuthUser | null>(initialAuth.user)
  const [token, setToken] = useState<string | null>(initialAuth.token)
  const [isLoading] = useState(false)

  const login = (newToken: string, newUser: AuthUser) => {
    localStorage.setItem('gm_token', newToken)
    localStorage.setItem('gm_user', JSON.stringify(newUser))
    setToken(newToken)
    setUser(newUser)
  }

  const logout = () => {
    localStorage.removeItem('gm_token')
    localStorage.removeItem('gm_user')
    setToken(null)
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, token, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}


