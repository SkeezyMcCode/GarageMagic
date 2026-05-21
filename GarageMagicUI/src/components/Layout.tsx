import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/useAuth'

const nav = [
  { to: '/', label: 'Dashboard', emoji: '🏆' },
  { to: '/players', label: 'Players', emoji: '👤' },
  { to: '/matches', label: 'Matches', emoji: '⚔️' },
  { to: '/betrayals', label: 'Betrayals', emoji: '🗡️' },
  { to: '/seasons', label: 'Seasons', emoji: '📅' },
]

export default function Layout({ children }: { children: React.ReactNode }) {
  const { pathname } = useLocation()
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [menuOpen, setMenuOpen] = useState(false)

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      {/* ── Top header ── */}
      <header className="bg-gray-900 border-b border-gray-800 sticky top-0 z-50">
        <div className="max-w-6xl mx-auto px-4 flex items-center justify-between h-14">
          <Link to="/" className="text-lg font-bold text-purple-400 flex items-center gap-1.5">
            🃏 GarageMagic
          </Link>

          {/* Desktop nav */}
          <nav className="hidden md:flex gap-1">
            {nav.map(n => (
              <Link key={n.to} to={n.to}
                className={`px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                  pathname === n.to ? 'bg-purple-600 text-white' : 'text-gray-400 hover:text-white hover:bg-gray-800'
                }`}>
                {n.emoji} {n.label}
              </Link>
            ))}
            {user?.isAdmin && (
              <Link to="/admin"
                className={`px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                  pathname === '/admin' ? 'bg-yellow-600 text-white' : 'text-yellow-500 hover:text-white hover:bg-gray-800'
                }`}>
                ⚙️ Admin
              </Link>
            )}
          </nav>

          {/* Desktop: user + sign out */}
          <div className="hidden md:flex items-center gap-3">
            <span className="text-gray-400 text-sm">👤 {user?.username}</span>
            <button onClick={handleLogout}
              className="text-gray-500 hover:text-white text-sm px-3 py-1.5 rounded-lg hover:bg-gray-800 transition-colors">
              Sign out
            </button>
          </div>

          {/* Mobile: user + menu toggle */}
          <div className="flex md:hidden items-center gap-2">
            <span className="text-gray-400 text-sm truncate max-w-[120px]">👤 {user?.username}</span>
            <button onClick={() => setMenuOpen(v => !v)}
              className="p-2 rounded-lg text-gray-400 hover:text-white hover:bg-gray-800 transition-colors">
              {menuOpen ? '✕' : '☰'}
            </button>
          </div>
        </div>

        {/* Mobile dropdown (for sign out + admin) */}
        {menuOpen && (
          <div className="md:hidden bg-gray-900 border-t border-gray-800 px-4 py-3 space-y-2">
            {user?.isAdmin && (
              <Link to="/admin" onClick={() => setMenuOpen(false)}
                className={`flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                  pathname === '/admin' ? 'bg-yellow-600 text-white' : 'text-yellow-400 hover:bg-gray-800'
                }`}>
                ⚙️ Admin Panel
              </Link>
            )}
            <button onClick={() => { handleLogout(); setMenuOpen(false) }}
              className="w-full text-left flex items-center gap-2 px-3 py-2 rounded-lg text-sm text-gray-400 hover:text-white hover:bg-gray-800 transition-colors">
              🚪 Sign out
            </button>
          </div>
        )}
      </header>

      {/* ── Page content ── */}
      <main className="max-w-6xl mx-auto px-4 py-6 pb-24 md:pb-8">
        {children}
      </main>

      {/* ── Mobile bottom nav ── */}
      <nav className="md:hidden fixed bottom-0 left-0 right-0 z-50 bg-gray-900 border-t border-gray-800
                      flex items-stretch safe-bottom">
        {nav.map(n => (
          <Link key={n.to} to={n.to}
            className={`flex-1 flex flex-col items-center justify-center gap-0.5 py-2 text-xs font-medium transition-colors ${
              pathname === n.to ? 'text-purple-400' : 'text-gray-500 hover:text-gray-300'
            }`}>
            <span className="text-lg leading-none">{n.emoji}</span>
            <span className="leading-none">{n.label}</span>
          </Link>
        ))}
      </nav>
    </div>
  )
}
