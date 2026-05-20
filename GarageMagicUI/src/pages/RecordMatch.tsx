import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getLeaderboard, getDecksByUser, createMatch, getCurrentSeason } from '../api'
import type { UserStandingDto, DeckDto } from '../types'
import { Card, Spinner, ErrorMsg, SectionHeader } from '../components/Ui'

const MATCH_TYPES = [
  { value: 0, label: '1v1v1 (3 players)', count: 3, sheriff: false },
  { value: 1, label: '1v1v1v1 (4 players)', count: 4, sheriff: false },
  { value: 2, label: '5-Player Sheriff', count: 5, sheriff: true },
  { value: 3, label: '6-Player Sheriff', count: 6, sheriff: true },
]

const ROLE_OPTIONS = [
  { value: 0, label: 'Sheriff', color: 'yellow' },
  { value: 1, label: 'Deputy', color: 'blue' },
  { value: 2, label: 'Red', color: 'red' },
]

interface Participant { userId: number; deckId?: number; hiddenRole?: number }

export default function RecordMatch() {
  const nav = useNavigate()
  const [players, setPlayers] = useState<UserStandingDto[]>([])
  const [decksByUser, setDecksByUser] = useState<Record<number, DeckDto[]>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [matchTypeIdx, setMatchTypeIdx] = useState(0)
  const [matchDate, setMatchDate] = useState(new Date().toISOString().slice(0, 16))
  const [participants, setParticipants] = useState<Participant[]>([{ userId: 0 }, { userId: 0 }, { userId: 0 }])
  const [winners, setWinners] = useState<number[]>([])
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState('')

  useEffect(() => {
    Promise.all([getCurrentSeason(), getLeaderboard()])
      .then(([, lb]) => setPlayers(lb))
      .catch(() => setError('Could not load players.'))
      .finally(() => setLoading(false))
  }, [])

  const matchType = MATCH_TYPES[matchTypeIdx]

  const setMatchType = (idx: number) => {
    setMatchTypeIdx(idx)
    const count = MATCH_TYPES[idx].count
    setParticipants(Array.from({ length: count }, () => ({ userId: 0 })))
    setWinners([])
  }

  const loadDecks = async (userId: number) => {
    if (!userId || decksByUser[userId]) return
    try {
      const d = await getDecksByUser(userId)
      setDecksByUser(prev => ({ ...prev, [userId]: d.filter(dk => dk.isActive) }))
    } catch { /* ignore */ }
  }

  const setParticipantUser = (i: number, userId: number) => {
    setParticipants(prev => {
      const next = [...prev]
      next[i] = { ...next[i], userId, deckId: undefined }
      return next
    })
    if (userId) loadDecks(userId)
  }

  const setParticipantDeck = (i: number, deckId: number) =>
    setParticipants(prev => { const n = [...prev]; n[i] = { ...n[i], deckId: deckId || undefined }; return n })

  const setParticipantRole = (i: number, role: number) =>
    setParticipants(prev => { const n = [...prev]; n[i] = { ...n[i], hiddenRole: role }; return n })

  const toggleWinner = (userId: number) =>
    setWinners(prev => prev.includes(userId) ? prev.filter(id => id !== userId) : [...prev, userId])

  const usedUserIds = participants.map(p => p.userId).filter(Boolean)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitError('')
    if (participants.some(p => !p.userId)) { setSubmitError('All participant slots must be filled'); return }
    if (new Set(participants.map(p => p.userId)).size !== participants.length) { setSubmitError('Each player can only appear once'); return }
    if (winners.length === 0) { setSubmitError('Select at least one winner'); return }

    setSubmitting(true)
    try {
      await createMatch({
        matchType: matchType.value,
        matchDate: new Date(matchDate).toISOString(),
        participants: participants.map(p => ({
          userId: p.userId,
          deckId: p.deckId,
          hiddenRole: matchType.sheriff ? p.hiddenRole : undefined,
        })),
        winnerUserIds: winners,
      })
      nav('/matches')
    } catch (err: unknown) {
      const msgs = (err as { response?: { data?: string[] | { error?: string } } })?.response?.data
      if (Array.isArray(msgs)) setSubmitError(msgs.join(', '))
      else setSubmitError((msgs as { error?: string })?.error ?? 'Failed to record match')
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <Spinner />
  if (error) return <ErrorMsg msg={error} />

  return (
    <div className="max-w-2xl mx-auto">
      <SectionHeader title="⚔️ Record Match" subtitle="Log the results of your game" />

      <form onSubmit={submit} className="space-y-6">
        {/* Match type */}
        <Card>
          <h3 className="font-semibold text-white mb-3">Match Type</h3>
          <div className="grid grid-cols-2 gap-2">
            {MATCH_TYPES.map((t, i) => (
              <button key={i} type="button" onClick={() => setMatchType(i)}
                className={`px-3 py-2.5 rounded-lg text-sm font-medium border transition-colors text-left ${matchTypeIdx === i ? 'bg-purple-600 border-purple-500 text-white' : 'bg-gray-800 border-gray-700 text-gray-400 hover:border-gray-600'}`}>
                {t.label}
              </button>
            ))}
          </div>
        </Card>

        {/* Date */}
        <Card>
          <h3 className="font-semibold text-white mb-3">Match Date</h3>
          <input
            type="datetime-local"
            className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500 w-full"
            value={matchDate}
            onChange={e => setMatchDate(e.target.value)}
          />
        </Card>

        {/* Participants */}
        <Card>
          <h3 className="font-semibold text-white mb-3">Players</h3>
          <div className="space-y-3">
            {participants.map((p, i) => (
              <div key={i} className="space-y-2">
                <div className="flex items-center gap-2">
                  <span className="text-gray-500 text-sm w-6 text-center">{i + 1}</span>
                  <select
                    className="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                    value={p.userId || ''}
                    onChange={e => setParticipantUser(i, Number(e.target.value))}
                  >
                    <option value="">Select player…</option>
                    {players.map(pl => (
                      <option key={pl.userId} value={pl.userId} disabled={usedUserIds.includes(pl.userId) && pl.userId !== p.userId}>
                        {pl.username}
                      </option>
                    ))}
                  </select>

                  {p.userId > 0 && (decksByUser[p.userId]?.length ?? 0) > 0 && (
                    <select
                      className="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-gray-300 text-sm focus:outline-none focus:border-purple-500"
                      value={p.deckId ?? ''}
                      onChange={e => setParticipantDeck(i, Number(e.target.value))}
                    >
                      <option value="">No deck</option>
                      {decksByUser[p.userId].map(d => <option key={d.id} value={d.id}>{d.deckName}</option>)}
                    </select>
                  )}
                </div>

                {matchType.sheriff && p.userId > 0 && (
                  <div className="flex gap-2 ml-8">
                    {ROLE_OPTIONS.map(r => (
                      <button key={r.value} type="button" onClick={() => setParticipantRole(i, r.value)}
                        className={`text-xs px-2 py-1 rounded border transition-colors ${p.hiddenRole === r.value ? 'bg-purple-600 border-purple-500 text-white' : 'bg-gray-800 border-gray-700 text-gray-400 hover:border-gray-500'}`}>
                        {r.label}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        </Card>

        {/* Winners */}
        <Card>
          <h3 className="font-semibold text-white mb-3">
            Winners {matchType.sheriff ? <span className="text-gray-500 text-xs font-normal">(Sheriff + Deputies)</span> : ''}
          </h3>
          <div className="flex flex-wrap gap-2">
            {participants.filter(p => p.userId > 0).map(p => {
              const player = players.find(pl => pl.userId === p.userId)
              if (!player) return null
              const selected = winners.includes(p.userId)
              return (
                <button key={p.userId} type="button" onClick={() => toggleWinner(p.userId)}
                  className={`px-3 py-2 rounded-lg text-sm font-medium border transition-colors ${selected ? 'bg-green-700 border-green-600 text-white' : 'bg-gray-800 border-gray-700 text-gray-400 hover:border-gray-500'}`}>
                  {selected ? '👑 ' : ''}{player.username}
                </button>
              )
            })}
          </div>
          {winners.length > 0 && (
            <p className="text-green-400 text-xs mt-2">Winner{winners.length > 1 ? 's' : ''}: {winners.map(id => players.find(p => p.userId === id)?.username).join(', ')}</p>
          )}
        </Card>

        {submitError && <ErrorMsg msg={submitError} />}

        <button
          type="submit"
          disabled={submitting}
          className="w-full bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white font-semibold py-3 rounded-xl text-base transition-colors"
        >
          {submitting ? 'Recording…' : '⚔️ Record Match'}
        </button>
      </form>
    </div>
  )
}

