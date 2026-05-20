import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getLeaderboard, getCurrentSeason, registerUser } from '../api'
import type { UserStandingDto, SeasonDto } from '../types'
import { Card, Spinner, ErrorMsg, SectionHeader, PrestigeBadge, WinRateBar } from '../components/Ui'

export default function Players() {
  const [players, setPlayers] = useState<UserStandingDto[]>([])
  const [season, setSeason] = useState<SeasonDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({ username: '', email: '', password: '' })
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const load = async () => {
    try {
      const s = await getCurrentSeason()
      setSeason(s)
      setPlayers(await getLeaderboard(s.id))
    } catch {
      setError('Could not load players. Is the API running?')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError('')
    setSubmitting(true)
    try {
      await registerUser(form)
      setShowForm(false)
      setForm({ username: '', email: '', password: '' })
      await load()
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setFormError(msg ?? 'Registration failed')
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <Spinner />
  if (error) return <ErrorMsg msg={error} />

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <SectionHeader title="👤 Players" subtitle={season ? `Rankings for ${season.name}` : ''} />
        <button
          onClick={() => setShowForm(v => !v)}
          className="bg-purple-600 hover:bg-purple-500 text-white font-semibold px-4 py-2 rounded-lg text-sm transition-colors"
        >
          {showForm ? 'Cancel' : '+ Add Player'}
        </button>
      </div>

      {showForm && (
        <Card className="mb-6">
          <h3 className="font-semibold text-white mb-4">Register New Player</h3>
          <form onSubmit={submit} className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <input
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
              placeholder="Username"
              value={form.username}
              onChange={e => setForm(f => ({ ...f, username: e.target.value }))}
              required
            />
            <input
              type="email"
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
              placeholder="Email"
              value={form.email}
              onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
              required
            />
            <input
              type="password"
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
              placeholder="Password (min 8 chars)"
              value={form.password}
              onChange={e => setForm(f => ({ ...f, password: e.target.value }))}
              required
            />
            {formError && <div className="sm:col-span-3"><ErrorMsg msg={formError} /></div>}
            <div className="sm:col-span-3">
              <button
                type="submit"
                disabled={submitting}
                className="bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white font-semibold px-6 py-2 rounded-lg text-sm transition-colors"
              >
                {submitting ? 'Registering...' : 'Register Player'}
              </button>
            </div>
          </form>
        </Card>
      )}

      {players.length === 0 ? (
        <Card><p className="text-gray-500 text-sm">No players yet. Add one above!</p></Card>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {players.map((p, i) => (
            <Link key={p.userId} to={`/players/${p.userId}`}>
              <Card className="hover:border-purple-700 transition-colors cursor-pointer h-full">
                <div className="flex items-start justify-between mb-3">
                  <div>
                    <span className="text-2xl font-bold text-gray-600 mr-2">#{i + 1}</span>
                    <span className="text-white font-semibold text-lg">{p.username}</span>
                  </div>
                  {p.prestigeLevel > 0 && <PrestigeBadge level={p.prestigeLevel} />}
                </div>
                <div className="grid grid-cols-3 gap-2 text-center text-sm mb-3">
                  <div className="bg-gray-800 rounded-lg py-2">
                    <p className="text-green-400 font-bold text-lg">{p.totalWins}</p>
                    <p className="text-gray-500 text-xs">Wins</p>
                  </div>
                  <div className="bg-gray-800 rounded-lg py-2">
                    <p className="text-red-400 font-bold text-lg">{p.totalLosses}</p>
                    <p className="text-gray-500 text-xs">Losses</p>
                  </div>
                  <div className="bg-gray-800 rounded-lg py-2">
                    <p className="text-purple-400 font-bold text-lg">{p.winRate.toFixed(0)}%</p>
                    <p className="text-gray-500 text-xs">Win Rate</p>
                  </div>
                </div>
                <WinRateBar winRate={p.winRate} />
                <p className="text-gray-600 text-xs mt-2">{p.totalMatches} matches played</p>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}

