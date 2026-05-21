import { useEffect, useMemo, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { getUserWithStats, getDecksByUser, getMatchesByUser, getCurrentSeason, getUserStats, createDeck, updateDeck, getBetrayalsByUser } from '../api'
import type { UserWithStatsDto, DeckDto, MatchDto, UserStatsDto, BetrayalDto } from '../types'
import { Card, Spinner, ErrorMsg, Badge, PrestigeBadge, ColorPips } from '../components/Ui'
import CommanderAutocompleteInput from '../components/CommanderAutocompleteInput'

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
  const [createColorOverridden, setCreateColorOverridden] = useState(false)
  const [deckSubmitting, setDeckSubmitting] = useState(false)
  const [editingDeckId, setEditingDeckId] = useState<number | null>(null)
  const [editDeckForm, setEditDeckForm] = useState({ deckName: '', commanderName: '', colorIdentity: '' })
  const [editColorOverridden, setEditColorOverridden] = useState(false)
  const [editSubmitting, setEditSubmitting] = useState(false)
  const [previewDeck, setPreviewDeck] = useState<DeckDto | null>(null)

  const deckStats = useMemo(() => {
    const stats = new Map<number, { wins: number; losses: number; matches: number }>()
    for (const deck of decks) {
      stats.set(deck.id, { wins: 0, losses: 0, matches: 0 })
    }

    for (const match of matches) {
      const participant = match.participants.find(p => p.userId === userId && p.deckId)
      if (!participant?.deckId) continue
      const current = stats.get(participant.deckId)
      if (!current) continue
      const won = match.winners.some(w => w.userId === userId)
      current.matches += 1
      if (won) current.wins += 1
      else current.losses += 1
    }

    return stats
  }, [decks, matches, userId])

  useEffect(() => {
    let active = true

    void (async () => {
      try {
        const season = await getCurrentSeason()
        const [u, d, m, b] = await Promise.all([
          getUserWithStats(userId),
          getDecksByUser(userId),
          getMatchesByUser(userId),
          getBetrayalsByUser(userId),
        ])
        const seasonStats = await getUserStats(userId, season.id).catch(() => null)
        if (!active) return
        setUser(u)
        setDecks(d)
        setMatches(m)
        setBetrayals(b)
        setStats(seasonStats)
      } catch {
        if (active) setError('Player not found')
      } finally {
        if (active) setLoading(false)
      }
    })()

    return () => { active = false }
  }, [userId])

  const submitDeck = async (e: React.FormEvent) => {
    e.preventDefault()
    setDeckSubmitting(true)
    try {
      await createDeck(userId, { ...deckForm, colorIdentity: deckForm.colorIdentity || undefined })
      setShowDeckForm(false)
      setDeckForm({ deckName: '', commanderName: '', colorIdentity: '' })
      setCreateColorOverridden(false)
      setDecks(await getDecksByUser(userId))
    } finally { setDeckSubmitting(false) }
  }

  const startEditDeck = (deck: DeckDto) => {
    setEditingDeckId(deck.id)
    setEditDeckForm({
      deckName: deck.deckName,
      commanderName: deck.commanderName,
      colorIdentity: deck.colorIdentity ?? '',
    })
    setEditColorOverridden(false)
  }

  const cancelEditDeck = () => {
    setEditingDeckId(null)
    setEditDeckForm({ deckName: '', commanderName: '', colorIdentity: '' })
    setEditColorOverridden(false)
  }

  const submitEditDeck = async (deckId: number) => {
    setEditSubmitting(true)
    try {
      await updateDeck(deckId, {
        deckName: editDeckForm.deckName,
        commanderName: editDeckForm.commanderName,
        colorIdentity: editDeckForm.colorIdentity || undefined,
      })
      setDecks(await getDecksByUser(userId))
      cancelEditDeck()
    } finally {
      setEditSubmitting(false)
    }
  }

  if (loading) return <Spinner />
  if (error || !user) return <ErrorMsg msg={error || 'Player not found'} />

  return (
    <div className="space-y-6">
      {/* Header */}
      <Card>
        <div className="flex flex-col gap-4">
          <div>
            <Link to="/players" className="text-gray-500 hover:text-gray-300 text-sm mb-2 block">← Players</Link>
            <div className="flex items-start justify-between gap-2">
              <h1 className="text-3xl font-bold text-white">{user.username}</h1>
              {user.currentPrestigeLevel > 0 && <PrestigeBadge level={user.currentPrestigeLevel} />}
            </div>
            {user.email && <span className="text-gray-500 text-sm mt-1 block">{user.email}</span>}
          </div>
          <div className="grid grid-cols-3 gap-3 text-center">
            <div className="bg-gray-800 rounded-lg px-3 py-3">
              <p className="text-green-400 text-2xl font-bold">{user.totalWins}</p>
              <p className="text-gray-500 text-xs">All-time Wins</p>
            </div>
            <div className="bg-gray-800 rounded-lg px-3 py-3">
              <p className="text-red-400 text-2xl font-bold">{user.totalLosses}</p>
              <p className="text-gray-500 text-xs">All-time Losses</p>
            </div>
            <div className="bg-gray-800 rounded-lg px-3 py-3">
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
              <form onSubmit={submitDeck} className="space-y-2 mb-3">
                <input className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500" placeholder="Deck Name" value={deckForm.deckName} onChange={e => setDeckForm(f => ({ ...f, deckName: e.target.value }))} required />
                <CommanderAutocompleteInput
                  value={deckForm.commanderName}
                  onChange={commanderName => setDeckForm(f => ({ ...f, commanderName }))}
                  onCardResolved={card => {
                    if (card && !createColorOverridden) {
                      setDeckForm(f => ({ ...f, colorIdentity: card.colorIdentity.join('') }))
                    }
                  }}
                />
                <input className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500" placeholder="Colors (e.g. WUBG)" maxLength={6} value={deckForm.colorIdentity} onChange={e => { setCreateColorOverridden(true); setDeckForm(f => ({ ...f, colorIdentity: e.target.value.toUpperCase() })) }} />
                <button type="submit" disabled={deckSubmitting} className="w-full bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-colors">
                  {deckSubmitting ? 'Adding...' : 'Add Deck'}
                </button>
              </form>
            )}
            {decks.length === 0 ? <p className="text-gray-500 text-sm">No decks yet.</p> : (
              <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-3">
                {decks.map(d => (
                  <div
                    key={d.id}
                    className={`${editingDeckId === d.id ? 'sm:col-span-2 xl:col-span-3' : ''} bg-gray-800/50 rounded-lg p-3 transition-all ${editingDeckId === d.id ? '' : 'hover:bg-gray-800/70 hover:shadow-lg hover:shadow-black/30 hover:-translate-y-0.5'}`}
                  >
                    {editingDeckId === d.id ? (
                      <div className="space-y-2">
                        <input
                          className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                          value={editDeckForm.deckName}
                          onChange={e => setEditDeckForm(f => ({ ...f, deckName: e.target.value }))}
                          placeholder="Deck Name"
                          required
                        />
                        <CommanderAutocompleteInput
                          value={editDeckForm.commanderName}
                          onChange={commanderName => setEditDeckForm(f => ({ ...f, commanderName }))}
                          onCardResolved={card => {
                            if (card && !editColorOverridden) {
                              setEditDeckForm(f => ({ ...f, colorIdentity: card.colorIdentity.join('') }))
                            }
                          }}
                          initialImageUri={d.commanderImageUri ?? null}
                        />
                        <input
                          className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                          value={editDeckForm.colorIdentity}
                          onChange={e => { setEditColorOverridden(true); setEditDeckForm(f => ({ ...f, colorIdentity: e.target.value.toUpperCase() })) }}
                          placeholder="Colors (e.g. WUBG)"
                          maxLength={6}
                        />
                        <div className="flex gap-2">
                          <button
                            type="button"
                            onClick={() => void submitEditDeck(d.id)}
                            disabled={editSubmitting}
                            className="flex-1 bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-colors"
                          >
                            {editSubmitting ? 'Saving...' : 'Save'}
                          </button>
                          <button
                            type="button"
                            onClick={cancelEditDeck}
                            className="flex-1 bg-gray-700 hover:bg-gray-600 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-colors"
                          >
                            Cancel
                          </button>
                        </div>
                      </div>
                    ) : (
                      <div className="flex flex-col items-center text-center gap-3">
                        <div className="flex flex-col items-center gap-2 min-w-0 w-full">
                          <button
                            type="button"
                            onClick={() => setPreviewDeck(d)}
                            className="rounded-md overflow-hidden border border-gray-700 focus:outline-none focus:ring-2 focus:ring-purple-500"
                            aria-label={`Preview ${d.commanderName}`}
                          >
                            <img
                              src={d.commanderImageUri ?? '/commander-placeholder.svg'}
                              alt={`${d.commanderName} card art`}
                              className="w-36 h-52 rounded object-cover shrink-0 transition-transform hover:scale-[1.02]"
                            />
                          </button>
                          <div className="min-w-0 w-full">
                            <p className="text-white text-sm font-medium truncate">{d.deckName}</p>
                            <p className="text-gray-500 text-xs truncate mt-0.5">{d.commanderName}</p>
                            {(() => {
                              const stats = deckStats.get(d.id)
                              if (!stats || stats.matches === 0) {
                                return <p className="text-gray-600 text-[11px] mt-1">No recorded matches</p>
                              }
                              const winRate = (stats.wins / stats.matches) * 100
                              return (
                                <p className="text-gray-400 text-[11px] mt-1">
                                  {stats.wins}W-{stats.losses}L · {stats.matches} matches · {winRate.toFixed(0)}%
                                </p>
                              )
                            })()}
                          </div>
                        </div>
                        <div className="flex items-center gap-2 shrink-0 flex-wrap justify-center">
                          <ColorPips colors={d.colorIdentity} />
                          {!d.isActive && <Badge color="gray">Retired</Badge>}
                          <button
                            type="button"
                            onClick={() => startEditDeck(d)}
                            className="text-xs text-purple-400 hover:text-purple-300"
                          >
                            Edit
                          </button>
                        </div>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </Card>
        </div>
      </div>

      {previewDeck && (
        <div
          className="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4"
          onClick={() => setPreviewDeck(null)}
        >
          <div
            className="relative bg-gray-900 border border-gray-700 rounded-xl p-3 max-w-[95vw] max-h-[90vh]"
            onClick={e => e.stopPropagation()}
          >
            <button
              type="button"
              onClick={() => setPreviewDeck(null)}
              className="absolute top-2 right-2 bg-gray-800 hover:bg-gray-700 text-white text-xs px-2 py-1 rounded"
            >
              Close
            </button>
            <img
              src={previewDeck.commanderImageUri ?? '/commander-placeholder.svg'}
              alt={`${previewDeck.commanderName} full preview`}
              className="max-h-[78vh] w-auto rounded-lg object-contain"
            />
            <div className="mt-2 text-center">
              <p className="text-white font-semibold text-sm">{previewDeck.commanderName}</p>
              <p className="text-gray-400 text-xs">{previewDeck.deckName}</p>
            </div>
          </div>
        </div>
      )}

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

