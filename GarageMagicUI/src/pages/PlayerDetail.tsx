import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { getUserWithStats, getDecksByUser, getMatchesByUser, getCurrentSeason, getUserStats, createDeck, getBetrayalsByUser } from '../api'
import type { UserWithStatsDto, DeckDto, MatchDto, UserStatsDto, BetrayalDto } from '../types'
import { Card, Spinner, ErrorMsg, Badge, PrestigeBadge, ColorPips } from '../components/Ui'

const MATCH_TYPE_LABEL: Record<string, string> = {
  OneVsOneVsOne: '1v1v1', OneVsOneVsOneVsOne: '1v1v1v1',
  FivePlayerSheriff: '5P Sheriff', SixPlayerSheriff: '6P Sheriff',
}

export default function PlayerDetail() {
  const { id } = useParams<{ id: string }>()
  const userId = Number(id)

  const [user, setUser] = useState<UserWithStatsDto | null>(null)
  const [decks, setDecks] = useState<DeckDto[]>([])
  const [matches, setMatches] = useState<MatchDto[]>([])
  const [stats, setStats] = useState<UserStatsDto | null>(null)
  const [betrayals, setBetrayals] = useState<BetrayalDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showDeckForm, setShowDeckForm] = useState(false)
  const [deckForm, setDeckForm] = useState({ deckName: '', commanderName: '', colorIdentity: '' })
  const [deckSubmitting, setDeckSubmitting] = useState(false)

  const load = async () => {
    try {
      const season = await getCurrentSeason()
      const [u, d, m, b] = await Promise.all([
        getUserWithStats(userId),
        getDecksByUser(userId),
        getMatchesByUser(userId),
        getBetrayalsByUser(userId),
      ])
      setUser(u); setDecks(d); setMatches(m); setBetrayals(b)
      try { setStats(await getUserStats(userId, season.id)) } catch { /* no stats yet */ }
    } catch { setError('Player not found') }
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [userId])

  const submitDeck = async (e: React.FormEvent) => {
    e.preventDefault()
    setDeckSubmitting(true)
    try {
      await createDeck(userId, { ...deckForm, colorIdentity: deckForm.colorIdentity || undefined })
      setShowDeckForm(false)
      setDeckForm({ deckName: '', commanderName: '', colorIdentity: '' })
      setDecks(await getDecksByUser(userId))
    } finally { setDeckSubmitting(false) }
  }

  if (loading) return <Spinner />
  if (error || !user) return <ErrorMsg msg={error || 'Player not found'} />

  return (
    <div className="space-y-6">
      {/* Header */}
      <Card>
        <div className="flex items-start justify-between">
          <div>
            <Link to="/players" className="text-gray-500 hover:text-gray-300 text-sm mb-2 block">← Players</Link>
            <h1 className="text-3xl font-bold text-white">{user.username}</h1>
            <div className="flex items-center gap-2 mt-2">
              {user.currentPrestigeLevel > 0 && <PrestigeBadge level={user.currentPrestigeLevel} />}
              <span className="text-gray-500 text-sm">{user.email}</span>
            </div>
          </div>
          <div className="grid grid-cols-3 gap-3 text-center">
            <div className="bg-gray-800 rounded-lg px-4 py-3">
              <p className="text-green-400 text-2xl font-bold">{user.totalWins}</p>
              <p className="text-gray-500 text-xs">All-time Wins</p>
            </div>
            <div className="bg-gray-800 rounded-lg px-4 py-3">
              <p className="text-red-400 text-2xl font-bold">{user.totalLosses}</p>
              <p className="text-gray-500 text-xs">All-time Losses</p>
            </div>
            <div className="bg-gray-800 rounded-lg px-4 py-3">
              <p className="text-purple-400 text-2xl font-bold">{user.winRate.toFixed(0)}%</p>
              <p className="text-gray-500 text-xs">Win Rate</p>
            </div>
          </div>
        </div>
      </Card>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Season stats */}
        {stats && (
          <Card>
            <h3 className="font-semibold text-white mb-3">📊 {stats.seasonName} Stats</h3>
            <div className="space-y-2 text-sm">
              <Row label="Wins" value={stats.totalWins} color="text-green-400" />
              <Row label="Losses" value={stats.totalLosses} color="text-red-400" />
              <Row label="1v1v1 Wins" value={stats.wins1v1v1} />
              <Row label="1v1v1v1 Wins" value={stats.wins1v1v1v1} />
              <Row label="Sheriff Wins" value={stats.winsSheriff} />
              {stats.sheriffGamesPlayed > 0 && <>
                <hr className="border-gray-800" />
                <Row label="As Sheriff" value={`${stats.sheriffGamesWon}/${stats.sheriffGamesPlayed}`} />
                <Row label="As Deputy" value={`${stats.deputyGamesWon}/${stats.deputyGamesPlayed}`} />
                <Row label="As Red" value={`${stats.redGamesWon}/${stats.redGamesPlayed}`} />
              </>}
            </div>
          </Card>
        )}

        {/* Decks */}
        <div className={stats ? 'lg:col-span-2' : 'lg:col-span-3'}>
          <Card>
            <div className="flex items-center justify-between mb-3">
              <h3 className="font-semibold text-white">🃏 Decks ({decks.length})</h3>
              <button onClick={() => setShowDeckForm(v => !v)} className="text-xs text-purple-400 hover:text-purple-300">
                {showDeckForm ? 'Cancel' : '+ Add Deck'}
              </button>
            </div>
            {showDeckForm && (
              <form onSubmit={submitDeck} className="grid grid-cols-3 gap-2 mb-3">
                <input className="bg-gray-800 border border-gray-700 rounded px-2 py-1.5 text-white text-xs focus:outline-none focus:border-purple-500" placeholder="Deck Name" value={deckForm.deckName} onChange={e => setDeckForm(f => ({ ...f, deckName: e.target.value }))} required />
                <input className="bg-gray-800 border border-gray-700 rounded px-2 py-1.5 text-white text-xs focus:outline-none focus:border-purple-500" placeholder="Commander" value={deckForm.commanderName} onChange={e => setDeckForm(f => ({ ...f, commanderName: e.target.value }))} required />
                <input className="bg-gray-800 border border-gray-700 rounded px-2 py-1.5 text-white text-xs focus:outline-none focus:border-purple-500" placeholder="Colors (e.g. WUBG)" maxLength={6} value={deckForm.colorIdentity} onChange={e => setDeckForm(f => ({ ...f, colorIdentity: e.target.value.toUpperCase() }))} />
                <div className="col-span-3">
                  <button type="submit" disabled={deckSubmitting} className="bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white text-xs px-4 py-1.5 rounded transition-colors">
                    {deckSubmitting ? 'Adding...' : 'Add Deck'}
                  </button>
                </div>
              </form>
            )}
            {decks.length === 0 ? <p className="text-gray-500 text-sm">No decks yet.</p> : (
              <div className="space-y-2">
                {decks.map(d => (
                  <div key={d.id} className="flex items-center justify-between bg-gray-800/50 rounded-lg px-3 py-2">
                    <div>
                      <p className="text-white text-sm font-medium">{d.deckName}</p>
                      <p className="text-gray-500 text-xs">{d.commanderName}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <ColorPips colors={d.colorIdentity} />
                      {!d.isActive && <Badge color="gray">Retired</Badge>}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </Card>
        </div>
      </div>

      {/* Recent matches */}
      <Card>
        <h3 className="font-semibold text-white mb-3">⚔️ Match History ({matches.length})</h3>
        {matches.length === 0 ? <p className="text-gray-500 text-sm">No matches yet.</p> : (
          <div className="space-y-2">
            {matches.slice(0, 10).map(m => {
              const won = m.winners.some(w => w.userId === userId)
              return (
                <div key={m.id} className={`flex items-center gap-3 rounded-lg px-3 py-2 border-l-4 ${won ? 'border-green-600 bg-green-900/10' : 'border-red-800 bg-red-900/10'}`}>
                  <Badge color={won ? 'green' : 'red'}>{won ? 'WIN' : 'LOSS'}</Badge>
                  <Badge color="purple">{MATCH_TYPE_LABEL[m.matchType]}</Badge>
                  <span className="text-gray-500 text-sm">{new Date(m.matchDate).toLocaleDateString()}</span>
                  <span className="text-gray-400 text-sm flex-1 truncate">{m.participants.map(p => p.username).join(', ')}</span>
                </div>
              )
            })}
          </div>
        )}
      </Card>

      {/* Betrayals */}
      {betrayals.length > 0 && (
        <Card>
          <h3 className="font-semibold text-white mb-3">🗡️ Betrayal History</h3>
          <div className="space-y-2">
            {betrayals.map(b => (
              <div key={b.id} className="text-sm bg-gray-800/50 rounded-lg px-3 py-2">
                <span className="text-red-400 font-medium">{b.betrayerUsername}</span>
                <span className="text-gray-500"> betrayed </span>
                <span className="text-purple-400 font-medium">{b.victimUsername}</span>
                <span className="text-gray-600 mx-2">·</span>
                <span className="text-gray-400">{b.description}</span>
              </div>
            ))}
          </div>
        </Card>
      )}
    </div>
  )
}

function Row({ label, value, color = 'text-white' }: { label: string; value: string | number; color?: string }) {
  return (
    <div className="flex justify-between">
      <span className="text-gray-500">{label}</span>
      <span className={`font-semibold ${color}`}>{value}</span>
    </div>
  )
}

