import { Link, useLocation } from 'react-router-dom'

const nav = [
  { to: '/', label: '🏆 Dashboard' },
  { to: '/players', label: '👤 Players' },
  { to: '/matches', label: '⚔️ Matches' },
  { to: '/betrayals', label: '🗡️ Betrayals' },
  { to: '/seasons', label: '📅 Seasons' },
]

export default function Layout({ children }: { children: React.ReactNode }) {
  const { pathname } = useLocation()

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
          </nav>
        </div>
      </header>
      <main className="max-w-6xl mx-auto px-4 py-8">{children}</main>
    </div>
  )
}

