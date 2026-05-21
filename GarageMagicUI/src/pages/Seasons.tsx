import { useEffect, useState } from 'react'
import { getAllSeasons, getAllUsers, getSeasonStandings, rolloverSeason, updateSeason, upsertSeasonRecord } from '../api'
import { useAuth } from '../context/useAuth'
import type { SeasonDto, SeasonStandingsDto, UserDto, UserStandingDto } from '../types'
import { Card, Spinner, ErrorMsg, Badge, WinRateBar, PrestigeBadge } from '../components/Ui'

// ─── Medal row ───────────────────────────────────────────────────────────────
function StandingRow({ player, rank }: { player: UserStandingDto; rank: number }) {
  const medal = rank === 0 ? '🥇' : rank === 1 ? '🥈' : rank === 2 ? '🥉' : null
  return (
    <div className="flex items-center gap-3 py-2 border-b border-gray-800/50 last:border-0">
      <span className="w-7 text-center text-lg shrink-0">{medal ?? <span className="text-gray-600 font-bold text-sm">{rank + 1}</span>}</span>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-1.5">
          <span className="text-white font-medium truncate">{player.username}</span>
          {player.isGuest && <span className="text-gray-600 text-xs">(Guest)</span>}
          {player.prestigeLevel > 0 && <PrestigeBadge level={player.prestigeLevel} />}
        </div>
        <WinRateBar winRate={player.winRate} />
      </div>
      <div className="text-right shrink-0">
        <p className="text-white text-sm font-semibold">
          <span className="text-green-400">{player.totalWins}W</span>
          <span className="text-gray-500 mx-1">/</span>
          <span className="text-red-400">{player.totalLosses}L</span>
        </p>
        <p className="text-gray-500 text-xs">{player.winRate.toFixed(1)}%</p>
      </div>
    </div>
  )
}

// ─── Previous season card ─────────────────────────────────────────────────────
function PastSeasonCard({ season, standings, onLoad }: {
  season: SeasonDto
  standings: SeasonStandingsDto | null
  onLoad: (id: number) => void
}) {
  const [expanded, setExpanded] = useState(false)
  const top3 = standings?.standings.slice(0, 3) ?? []
  const rest = standings?.standings.slice(3) ?? []

  return (
    <Card>
      {/* Header */}
      <div className="flex items-start justify-between gap-2 mb-3">
        <div>
          <h3 className="text-white font-semibold">{season.name}</h3>
          <p className="text-gray-500 text-xs mt-0.5">
            {new Date(season.startDate).toLocaleDateString()} – {new Date(season.endDate).toLocaleDateString()}
          </p>
        </div>
        <Badge color="gray">Ended</Badge>
      </div>

      {/* Trigger load if not yet loaded */}
      {!standings && (
        <button
          onClick={() => onLoad(season.id)}
          className="text-purple-400 hover:text-purple-300 text-sm transition-colors"
        >
          View standings →
        </button>
      )}

      {standings && standings.standings.length === 0 && (
        <p className="text-gray-600 text-sm">No matches recorded.</p>
      )}

      {standings && standings.standings.length > 0 && (
        <>
          <div>
            {top3.map((p, i) => <StandingRow key={p.userId} player={p} rank={i} />)}
          </div>

          {rest.length > 0 && (
            <>
              {expanded && rest.map((p, i) => <StandingRow key={p.userId} player={p} rank={i + 3} />)}
              <button
                onClick={() => setExpanded(v => !v)}
                className="mt-3 text-xs text-purple-400 hover:text-purple-300 transition-colors"
              >
                {expanded ? '▲ Show less' : `▼ Show all ${standings.standings.length} players`}
              </button>
            </>
          )}
        </>
      )}
    </Card>
  )
}

// ─── Active season card ───────────────────────────────────────────────────────
export default function Seasons() {
  const { user } = useAuth()
  const [seasons, setSeasons] = useState<SeasonDto[]>([])
  const [users, setUsers] = useState<UserDto[]>([])
  const [standingsMap, setStandingsMap] = useState<Record<number, SeasonStandingsDto>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  // Edit mode state
  const [editing, setEditing] = useState(false)
  const [seasonForm, setSeasonForm] = useState({ name: '', startDate: '', endDate: '' })
  const [savingSeason, setSavingSeason] = useState(false)
  const [seasonFormError, setSeasonFormError] = useState('')

  // Record seeding state
  const [recordForm, setRecordForm] = useState({ userId: '', totalWins: '0', totalLosses: '0' })
  const [savingRecord, setSavingRecord] = useState(false)
  const [recordError, setRecordError] = useState('')
  const [recordSuccess, setRecordSuccess] = useState('')

  // Rollover
  const [rollingOver, setRollingOver] = useState(false)

  const toDateInput = (iso: string) => {
    const d = new Date(iso)
    return Number.isNaN(d.getTime()) ? '' : d.toISOString().slice(0, 10)
  }

  const load = async () => {
    const all = await getAllSeasons()
    const active = all.find(s => s.isActive) ?? all[0]
    const activeStandings = active ? await getSeasonStandings(active.id) : null
    return { all, active, activeStandings }
  }

  useEffect(() => {
    let mounted = true
    void (async () => {
      try {
        const { all, active, activeStandings } = await load()
        if (!mounted) return
        setSeasons(all)
        if (activeStandings) setStandingsMap({ [activeStandings.season.id]: activeStandings })
        if (active) setSeasonForm({ name: active.name, startDate: toDateInput(active.startDate), endDate: toDateInput(active.endDate) })
        if (user?.isAdmin) {
          const allUsers = await getAllUsers()
          if (!mounted) return
          setUsers(allUsers.filter(u => u.isApproved || u.isGuest))
        }
      } catch {
        if (mounted) setError('Could not load seasons.')
      } finally {
        if (mounted) setLoading(false)
      }
    })()
    return () => { mounted = false }
  }, [user?.isAdmin])

  const loadPastStandings = async (id: number) => {
    if (standingsMap[id]) return
    try {
      const s = await getSeasonStandings(id)
      setStandingsMap(prev => ({ ...prev, [id]: s }))
    } catch { /* no matches */ }
  }

  const doRollover = async () => {
    if (!confirm('Roll over to the next season? This will deactivate the current season.')) return
    setRollingOver(true)
    try {
      await rolloverSeason()
      const { all, active, activeStandings } = await load()
      setSeasons(all)
      if (activeStandings) setStandingsMap(prev => ({ ...prev, [activeStandings.season.id]: activeStandings }))
      if (active) setSeasonForm({ name: active.name, startDate: toDateInput(active.startDate), endDate: toDateInput(active.endDate) })
    } catch (err: unknown) {
      alert((err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Rollover failed')
    } finally { setRollingOver(false) }
  }

  const saveSeasonDetails = async (seasonId: number) => {
    setSeasonFormError('')
    if (!seasonForm.name.trim() || !seasonForm.startDate || !seasonForm.endDate) {
      setSeasonFormError('Name, start date and end date are required.')
      return
    }
    if (seasonForm.startDate > seasonForm.endDate) {
      setSeasonFormError('Start date must be on or before end date.')
      return
    }
    setSavingSeason(true)
    try {
      await updateSeason(seasonId, {
        name: seasonForm.name.trim(),
        startDate: new Date(seasonForm.startDate).toISOString(),
        endDate: new Date(seasonForm.endDate).toISOString(),
      })
      const { all, active, activeStandings } = await load()
      setSeasons(all)
      if (activeStandings) setStandingsMap(prev => ({ ...prev, [activeStandings.season.id]: activeStandings }))
      if (active) setSeasonForm({ name: active.name, startDate: toDateInput(active.startDate), endDate: toDateInput(active.endDate) })
      setEditing(false)
    } catch {
      setSeasonFormError('Could not save season details.')
    } finally { setSavingSeason(false) }
  }

  const saveRecord = async (seasonId: number) => {
    setRecordError('')
    setRecordSuccess('')
    const userId = Number(recordForm.userId)
    const totalWins = Number(recordForm.totalWins)
    const totalLosses = Number(recordForm.totalLosses)
    if (!userId) { setRecordError('Choose a player.'); return }
    if (!Number.isInteger(totalWins) || totalWins < 0 || !Number.isInteger(totalLosses) || totalLosses < 0) {
      setRecordError('Wins and losses must be whole numbers ≥ 0.')
      return
    }
    setSavingRecord(true)
    try {
      await upsertSeasonRecord(seasonId, { userId, totalWins, totalLosses })
      const updated = await getSeasonStandings(seasonId).catch(() => null)
      if (updated) setStandingsMap(prev => ({ ...prev, [seasonId]: updated }))
      setRecordForm({ userId: '', totalWins: '0', totalLosses: '0' })
      setRecordSuccess(`Record saved for ${users.find(u => u.id === userId)?.username ?? 'player'}.`)
    } catch {
      setRecordError('Could not save record.')
    } finally { setSavingRecord(false) }
  }

  if (loading) return <Spinner />
  if (error) return <ErrorMsg msg={error} />

  const activeSeason = seasons.find(s => s.isActive)
  const pastSeasons = seasons.filter(s => !s.isActive).sort((a, b) =>
    new Date(b.startDate).getTime() - new Date(a.startDate).getTime()
  )
  const activeStandings = activeSeason ? standingsMap[activeSeason.id] : null

  return (
    <div className="space-y-6">

      {/* ── Active season ─────────────────────────────────────────── */}
      {activeSeason ? (
        <Card>
          {/* Header row */}
          <div className="flex items-start justify-between gap-3 mb-1">
            <div>
              {editing ? (
                <input
                  className="bg-gray-800 border border-purple-600 rounded-lg px-3 py-1.5 text-white font-bold text-xl focus:outline-none w-full mb-1"
                  value={seasonForm.name}
                  onChange={e => setSeasonForm(f => ({ ...f, name: e.target.value }))}
                  placeholder="Season name"
                />
              ) : (
                <h2 className="text-2xl font-bold text-purple-400">{activeSeason.name}</h2>
              )}

              {editing ? (
                <div className="flex gap-2 mt-1">
                  <input type="date"
                    className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-1.5 text-white text-sm focus:outline-none focus:border-purple-500"
                    value={seasonForm.startDate}
                    onChange={e => setSeasonForm(f => ({ ...f, startDate: e.target.value }))}
                  />
                  <span className="text-gray-600 self-center">–</span>
                  <input type="date"
                    className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-1.5 text-white text-sm focus:outline-none focus:border-purple-500"
                    value={seasonForm.endDate}
                    onChange={e => setSeasonForm(f => ({ ...f, endDate: e.target.value }))}
                  />
                </div>
              ) : (
                <p className="text-gray-500 text-sm mt-0.5">
                  {new Date(activeSeason.startDate).toLocaleDateString()} – {new Date(activeSeason.endDate).toLocaleDateString()}
                </p>
              )}
            </div>

            <div className="flex items-center gap-2 shrink-0">
              <Badge color="green">Active</Badge>
              {user?.isAdmin && !editing && (
                <button onClick={() => { setEditing(true); setSeasonFormError('') }}
                  className="text-gray-400 hover:text-white text-xs px-3 py-1.5 rounded-lg bg-gray-800 hover:bg-gray-700 transition-colors">
                  ✏️ Edit
                </button>
              )}
            </div>
          </div>

          {/* Edit mode: save/cancel + errors */}
          {editing && (
            <div className="mt-3 mb-4 space-y-2">
              {seasonFormError && <ErrorMsg msg={seasonFormError} />}
              <div className="flex gap-2">
                <button onClick={() => saveSeasonDetails(activeSeason.id)} disabled={savingSeason}
                  className="bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-colors">
                  {savingSeason ? 'Saving…' : 'Save Changes'}
                </button>
                <button onClick={() => {
                  setEditing(false)
                  setSeasonFormError('')
                  setSeasonForm({ name: activeSeason.name, startDate: toDateInput(activeSeason.startDate), endDate: toDateInput(activeSeason.endDate) })
                }}
                  className="text-gray-400 hover:text-white text-sm px-4 py-2 rounded-lg bg-gray-800 hover:bg-gray-700 transition-colors">
                  Cancel
                </button>
              </div>
            </div>
          )}

          {/* Admin: seed records */}
          {user?.isAdmin && editing && (
            <div className="border-t border-gray-800 pt-4 mt-2 space-y-3">
              <p className="text-gray-400 text-sm font-medium">Seed / Correct Player Records</p>
              <select
                value={recordForm.userId}
                onChange={e => setRecordForm(f => ({ ...f, userId: e.target.value }))}
                className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
              >
                <option value="">Choose player or guest…</option>
                {users.map(u => (
                  <option key={u.id} value={String(u.id)}>{u.username}{u.isGuest ? ' (Guest)' : ''}</option>
                ))}
              </select>
              <div className="grid grid-cols-2 gap-2">
                <input type="number" min={0} step={1}
                  className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                  value={recordForm.totalWins}
                  onChange={e => setRecordForm(f => ({ ...f, totalWins: e.target.value }))}
                  placeholder="Wins"
                />
                <input type="number" min={0} step={1}
                  className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                  value={recordForm.totalLosses}
                  onChange={e => setRecordForm(f => ({ ...f, totalLosses: e.target.value }))}
                  placeholder="Losses"
                />
              </div>
              {recordError && <ErrorMsg msg={recordError} />}
              {recordSuccess && <p className="text-green-400 text-sm">{recordSuccess}</p>}
              <button onClick={() => saveRecord(activeSeason.id)} disabled={savingRecord}
                className="w-full bg-green-700 hover:bg-green-600 disabled:opacity-50 text-white font-semibold py-2.5 rounded-lg text-sm transition-colors">
                {savingRecord ? 'Saving…' : 'Save Record'}
              </button>
            </div>
          )}

          {/* Divider */}
          <div className="border-t border-gray-800 mt-4 pt-4">
            <p className="text-xs text-gray-500 uppercase tracking-widest mb-3">Current Standings</p>
            {!activeStandings || activeStandings.standings.length === 0 ? (
              <p className="text-gray-500 text-sm">No matches recorded yet this season.</p>
            ) : (
              activeStandings.standings.map((p, i) => <StandingRow key={p.userId} player={p} rank={i} />)
            )}
          </div>

          {/* Admin rollover */}
          {user?.isAdmin && !editing && (
            <div className="mt-4 pt-4 border-t border-gray-800 flex justify-end">
              <button onClick={doRollover} disabled={rollingOver}
                className="bg-yellow-700 hover:bg-yellow-600 disabled:opacity-50 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-colors">
                {rollingOver ? 'Rolling over…' : '🔄 Rollover Season'}
              </button>
            </div>
          )}
        </Card>
      ) : (
        <Card><p className="text-gray-500 text-sm">No active season. Use rollover to start one.</p></Card>
      )}

      {/* ── Past seasons ──────────────────────────────────────────── */}
      {pastSeasons.length > 0 && (
        <div>
          <p className="text-xs text-gray-500 uppercase tracking-widest mb-3 px-1">Past Seasons</p>
          <div className="space-y-4">
            {pastSeasons.map(s => (
              <PastSeasonCard
                key={s.id}
                season={s}
                standings={standingsMap[s.id] ?? null}
                onLoad={loadPastStandings}
              />
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

