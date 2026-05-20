import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

const nav = [
  { to: '/', label: '🏆 Dashboard' },
  { to: '/players', label: '👤 Players' },
  { to: '/matches', label: '⚔️ Matches' },
  { to: '/betrayals', label: '🗡️ Betrayals' },
  { to: '/seasons', label: '📅 Seasons' },
]

export default function Layout({ children }: { children: React.ReactNode }) {
  const { pathname } = useLocation()
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      <header className="bg-gray-900 border-b border-gray-800 sticky top-0 z-50">
        <div className="max-w-6xl mx-auto px-4 flex items-center justify-between h-16">
          <Link to="/" className="text-xl font-bold text-purple-400 flex items-center gap-2">
            🃏 GarageMagic
          </Link>
          <nav className="flex gap-1">
            {nav.map(n => (
              <Link
                key={n.to}
                to={n.to}
                className={`px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                  pathname === n.to
                    ? 'bg-purple-600 text-white'
                    : 'text-gray-400 hover:text-white hover:bg-gray-800'
                }`}
              >
                {n.label}
              </Link>
            ))}
            {user?.isAdmin && (
              <Link to="/admin"
                className={`px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                  pathname === '/admin'
                    ? 'bg-yellow-600 text-white'
                    : 'text-yellow-500 hover:text-white hover:bg-gray-800'
                }`}>
                ⚙️ Admin
              </Link>
            )}
          </nav>
          <div className="flex items-center gap-3">
            <span className="text-gray-400 text-sm hidden sm:block">👤 {user?.username}</span>
            <button onClick={handleLogout}
              className="text-gray-500 hover:text-white text-sm px-3 py-1.5 rounded-lg hover:bg-gray-800 transition-colors">
              Sign out
            </button>
          </div>
        </div>
      </header>
      <main className="max-w-6xl mx-auto px-4 py-8">{children}</main>
    </div>
  )
}
