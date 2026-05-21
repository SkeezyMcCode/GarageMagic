import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getSelectableUsers, getDecksByUser, createMatch, getSheriffRolesMetadata } from '../api'
import type { UserDto, DeckDto, SheriffRoleDto } from '../types'
import ManaCostSymbols from '../components/ManaCostSymbols'
import { Card, Spinner, ErrorMsg, SectionHeader } from '../components/Ui'

const MATCH_TYPES = [
  { value: 0, label: '1v1v1 (3 players)', count: 3, sheriff: false },
  { value: 1, label: '1v1v1v1 (4 players)', count: 4, sheriff: false },
  { value: 2, label: '5-Player Sheriff', count: 5, sheriff: true },
  { value: 3, label: '6-Player Sheriff', count: 6, sheriff: true },
]

const DEFAULT_ROLE_OPTIONS: SheriffRoleDto[] = [
  { value: 0, role: 'Sheriff', label: 'Sheriff', color: '#ffffff', manaSymbol: '{W}', allowMultiple: false },
  { value: 1, role: 'Deputy', label: 'Deputy', color: '#60a5fa', manaSymbol: '{U}', allowMultiple: false },
  { value: 2, role: 'Renegade', label: 'Renegade', color: '#f87171', manaSymbol: '{R}', allowMultiple: true },
]

interface Participant { userId: number; deckId?: number; hiddenRole?: number }

function normalizeManaSymbol(symbol?: string) {
  if (!symbol) return ''
  const trimmed = symbol.trim()
  if (!trimmed) return ''
  return trimmed.startsWith('{') ? trimmed : `{${trimmed}}`
}

function roleAllowsMultiple(role: SheriffRoleDto) {
  if (typeof role.allowMultiple === 'boolean') return role.allowMultiple
  return role.role.toLowerCase().includes('outlaw') || role.label.toLowerCase().includes('outlaw')
}

function colorToRgba(input: string, alpha: number) {
  const hex = input.replace('#', '')
  if (!/^[0-9a-fA-F]{6}$/.test(hex)) return `rgba(124, 58, 237, ${alpha})`
  const int = Number.parseInt(hex, 16)
  const r = (int >> 16) & 255
  const g = (int >> 8) & 255
  const b = int & 255
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

function resolveRoleColor(input: string) {
  const named: Record<string, string> = {
    white: '#ffffff',
    blue: '#60a5fa',
    red: '#f87171',
    green: '#4ade80',
    black: '#111827',
  }
  const key = input.trim().toLowerCase()
  return named[key] ?? input
}

function roleButtonStyle(color: string, selected: boolean) {
  const resolved = resolveRoleColor(color)

  // Strong defaults when backend sends unexpected color values.
  if (!resolved.startsWith('#') || resolved.length !== 7) {
    return selected
      ? { backgroundColor: '#7c3aed', borderColor: '#7c3aed', color: '#ffffff' }
      : { backgroundColor: 'rgba(55, 65, 81, 0.95)', borderColor: '#6b7280', color: '#e5e7eb' }
  }

  // Renegade red gets extra contrast in both states.
  if (resolved.toLowerCase() === '#f87171') {
    return selected
      ? { backgroundColor: '#ef4444', borderColor: '#ef4444', color: '#ffffff' }
      : { backgroundColor: '#1f1b1b', borderColor: '#ef4444', color: '#fca5a5' }
  }

  // White role needs dark text when selected.
  if (resolved.toLowerCase() === '#ffffff') {
    return selected
      ? { backgroundColor: '#ffffff', borderColor: '#d1d5db', color: '#111827' }
      : { backgroundColor: '#1f2937', borderColor: '#9ca3af', color: '#f9fafb' }
  }

  return selected
    ? { backgroundColor: resolved, borderColor: resolved, color: '#ffffff' }
    : { backgroundColor: colorToRgba(resolved, 0.25), borderColor: colorToRgba(resolved, 0.65), color: '#e5e7eb' }
}


export default function RecordMatch() {
  const nav = useNavigate()
  const [players, setPlayers] = useState<UserDto[]>([])
  const [decksByUser, setDecksByUser] = useState<Record<number, DeckDto[]>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [matchTypeIdx, setMatchTypeIdx] = useState(0)
  const [matchDate, setMatchDate] = useState(new Date().toISOString().slice(0, 16))
  const [participants, setParticipants] = useState<Participant[]>([{ userId: 0 }, { userId: 0 }, { userId: 0 }])
  const [winners, setWinners] = useState<number[]>([])
  const [roleOptions, setRoleOptions] = useState<SheriffRoleDto[]>(DEFAULT_ROLE_OPTIONS)
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState('')

  useEffect(() => {
    Promise.all([
      getSelectableUsers(),
      getSheriffRolesMetadata().catch(() => null),
    ])
      .then(([selectableUsers, sheriffRoles]) => {
        setPlayers(selectableUsers)

        const roles = Array.isArray(sheriffRoles)
          ? sheriffRoles
          : sheriffRoles?.roles

        if (roles && roles.length > 0) {
          const normalized = roles.map((role, index) => ({
            value: Number.isInteger(role.value) ? role.value : index,
            role: role.role || role.label || `Role${index + 1}`,
            label: role.label || role.role || `Role ${index + 1}`,
            color: role.color || '#7c3aed',
            manaSymbol: role.manaSymbol,
            winCondition: role.winCondition,
            allowMultiple: role.allowMultiple,
          }))
          setRoleOptions(normalized)
        }
      })
      .catch(() => setError('Could not load players.'))
      .finally(() => setLoading(false))
  }, [])

  const matchType = MATCH_TYPES[matchTypeIdx]

  const setMatchType = (idx: number) => {
    setMatchTypeIdx(idx)
    const count = MATCH_TYPES[idx].count
    setParticipants(Array.from({ length: count }, () => ({ userId: 0 })))
    setWinners([])
    setSubmitError('')
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
      next[i] = { ...next[i], userId, deckId: undefined, hiddenRole: undefined }
      return next
    })
    if (userId) loadDecks(userId)
  }

  const setParticipantDeck = (i: number, deckId: number) =>
    setParticipants(prev => { const n = [...prev]; n[i] = { ...n[i], deckId: deckId || undefined }; return n })

  const setParticipantRole = (i: number, role: SheriffRoleDto) => {
    if (!roleAllowsMultiple(role)) {
      const usedElsewhere = participants.some((p, idx) => idx !== i && p.hiddenRole === role.value)
      if (usedElsewhere) return
    }

    setParticipants(prev => {
      const n = [...prev]
      n[i] = { ...n[i], hiddenRole: n[i].hiddenRole === role.value ? undefined : role.value }
      return n
    })
  }

  const toggleWinner = (userId: number) =>
    setWinners(prev => prev.includes(userId) ? prev.filter(id => id !== userId) : [...prev, userId])

  const usedUserIds = participants.map(p => p.userId).filter(Boolean)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitError('')
    if (participants.some(p => !p.userId)) { setSubmitError('All participant slots must be filled'); return }
    if (new Set(participants.map(p => p.userId)).size !== participants.length) { setSubmitError('Each player can only appear once'); return }
    if (matchType.sheriff && participants.some(p => p.hiddenRole === undefined)) { setSubmitError('All sheriff match roles must be assigned.'); return }
    if (matchType.sheriff) {
      for (const role of roleOptions) {
        if (roleAllowsMultiple(role)) continue
        const count = participants.filter(p => p.hiddenRole === role.value).length
        if (count > 1) { setSubmitError(`Only one player can be ${role.label}.`); return }
      }
    }
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
                  <span className="text-gray-500 text-sm w-6 text-center shrink-0">{i + 1}</span>
                  <select
                    className="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-3 py-2.5 text-white text-sm focus:outline-none focus:border-purple-500"
                    value={p.userId || ''}
                    onChange={e => setParticipantUser(i, Number(e.target.value))}
                  >
                    <option value="">Select player…</option>
                    {players.map(pl => (
                      <option key={pl.id} value={pl.id} disabled={usedUserIds.includes(pl.id) && pl.id !== p.userId}>
                        {pl.username}
                      </option>
                    ))}
                  </select>
                </div>

                {p.userId > 0 && (decksByUser[p.userId]?.length ?? 0) > 0 && (
                  <div className="ml-8">
                    <select
                      className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2.5 text-gray-300 text-sm focus:outline-none focus:border-purple-500"
                      value={p.deckId ?? ''}
                      onChange={e => setParticipantDeck(i, Number(e.target.value))}
                    >
                      <option value="">No deck selected</option>
                      {decksByUser[p.userId].map(d => <option key={d.id} value={d.id}>{d.deckName}</option>)}
                    </select>
                  </div>
                )}

                {matchType.sheriff && p.userId > 0 && (
                  <div className="flex gap-2 ml-8">
                    {roleOptions.map(role => {
                      const selected = p.hiddenRole === role.value
                      const disabled = !selected && !roleAllowsMultiple(role) && participants.some((other, idx) => idx !== i && other.hiddenRole === role.value)
                      const buttonStyle = roleButtonStyle(role.color || '#7c3aed', selected)
                      return (
                      <button
                        key={role.value}
                        type="button"
                        onClick={() => setParticipantRole(i, role)}
                        disabled={disabled}
                        title={role.winCondition ?? role.label}
                        style={buttonStyle}
                        className={`flex-1 text-sm py-2 rounded-lg border transition-colors ${disabled ? 'opacity-35 cursor-not-allowed' : 'hover:brightness-110'}`}
                      >
                        <span className="inline-flex items-center justify-center gap-1.5 w-full">
                          {role.manaSymbol && <ManaCostSymbols manaCost={normalizeManaSymbol(role.manaSymbol)} />}
                          <span className="leading-none">{role.label}</span>
                        </span>
                      </button>
                      )
                    })}
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
              const player = players.find(pl => pl.id === p.userId)
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
            <p className="text-green-400 text-xs mt-2">Winner{winners.length > 1 ? 's' : ''}: {winners.map(id => players.find(p => p.id === id)?.username).join(', ')}</p>
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

