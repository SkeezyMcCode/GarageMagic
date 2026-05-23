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

type RecordRow = {
  userId: string
  totalWins: string
  totalLosses: string
}

function createEmptyRecordRow(): RecordRow {
  return { userId: '', totalWins: '0', totalLosses: '0' }
}

function buildRecordRows(standings: SeasonStandingsDto | null): RecordRow[] {
  const rows = standings?.standings.map(player => ({
    userId: String(player.userId),
    totalWins: String(player.totalWins),
    totalLosses: String(player.totalLosses),
  })) ?? []

  return [...rows, createEmptyRecordRow()]
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

  // Record editor state
  const [recordRows, setRecordRows] = useState<RecordRow[]>([createEmptyRecordRow()])
  const [savingRecords, setSavingRecords] = useState(false)
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

  const refreshEditableRecords = (standings: SeasonStandingsDto | null) => {
    setRecordRows(buildRecordRows(standings))
  }

  useEffect(() => {
    let mounted = true
    void (async () => {
      try {
        const { all, active, activeStandings } = await load()
        if (!mounted) return
        setSeasons(all)
        if (activeStandings) setStandingsMap({ [activeStandings.season.id]: activeStandings })
        refreshEditableRecords(activeStandings)
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
      refreshEditableRecords(activeStandings)
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
      refreshEditableRecords(activeStandings)
      if (active) setSeasonForm({ name: active.name, startDate: toDateInput(active.startDate), endDate: toDateInput(active.endDate) })
      setEditing(false)
    } catch {
      setSeasonFormError('Could not save season details.')
    } finally { setSavingSeason(false) }
  }

  const updateRecordRow = (index: number, patch: Partial<RecordRow>) => {
    setRecordRows(prev => prev.map((row, rowIndex) => rowIndex === index ? { ...row, ...patch } : row))
  }

  const addRecordRow = () => {
    setRecordRows(prev => [...prev, createEmptyRecordRow()])
  }

  const removeRecordRow = (index: number) => {
    setRecordRows(prev => {
      const next = prev.filter((_, rowIndex) => rowIndex !== index)
      return next.length > 0 ? next : [createEmptyRecordRow()]
    })
  }

  const saveRecords = async (seasonId: number) => {
    setRecordError('')
    setRecordSuccess('')
    const rowsToSave = recordRows
      .map(row => ({
        userId: Number(row.userId),
        totalWins: Number(row.totalWins),
        totalLosses: Number(row.totalLosses),
      }))
      .filter(row => row.userId > 0)

    if (rowsToSave.length === 0) {
      setRecordError('Add at least one player record.')
      return
    }

    if (new Set(rowsToSave.map(row => row.userId)).size !== rowsToSave.length) {
      setRecordError('Each player can only appear once.')
      return
    }

    const invalidRow = rowsToSave.find(row =>
      !Number.isInteger(row.totalWins) || row.totalWins < 0 ||
      !Number.isInteger(row.totalLosses) || row.totalLosses < 0,
    )
    if (invalidRow) {
      setRecordError('Wins and losses must be whole numbers ≥ 0.')
      return
    }

    setSavingRecords(true)
    try {
      for (const row of rowsToSave) {
        await upsertSeasonRecord(seasonId, row)
      }
      const updated = await getSeasonStandings(seasonId).catch(() => null)
      if (updated) setStandingsMap(prev => ({ ...prev, [seasonId]: updated }))
      refreshEditableRecords(updated)
      setRecordSuccess(`Saved ${rowsToSave.length} record${rowsToSave.length === 1 ? '' : 's'}.`)
    } catch {
      setRecordError('Could not save standings.')
    } finally { setSavingRecords(false) }
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

      {/* ── Active season: public view ─────────────────────────────── */}
      {activeSeason ? (
        <Card>
          <div className="flex items-start justify-between gap-3 mb-3">
            <div>
              <h2 className="text-2xl font-bold text-purple-400">{activeSeason.name}</h2>
              <p className="text-gray-500 text-sm mt-0.5">
                {new Date(activeSeason.startDate).toLocaleDateString()} – {new Date(activeSeason.endDate).toLocaleDateString()}
              </p>
            </div>

            <div className="flex items-center gap-2 shrink-0">
              <Badge color="green">Active</Badge>
              {user?.isAdmin && <Badge color="yellow">Admin management below</Badge>}
            </div>
          </div>

          <div className="border-t border-gray-800 pt-4">
            <p className="text-xs text-gray-500 uppercase tracking-widest mb-3">Current Standings</p>
            {!activeStandings || activeStandings.standings.length === 0 ? (
              <p className="text-gray-500 text-sm">No matches recorded yet this season.</p>
            ) : (
              activeStandings.standings.map((p, i) => <StandingRow key={p.userId} player={p} rank={i} />)
            )}
          </div>
        </Card>
      ) : (
        <Card><p className="text-gray-500 text-sm">No active season. Use rollover to start one.</p></Card>
      )}

      {/* ── Active season: admin view ──────────────────────────────── */}
      {activeSeason && user?.isAdmin && (
        <Card>
          <div className="flex items-start justify-between gap-3 mb-3">
            <div>
              <h3 className="text-lg font-semibold text-white">Manage Season</h3>
              <p className="text-gray-500 text-sm mt-0.5">Update season details, correct standings, or roll over to the next season.</p>
            </div>
            <button
              type="button"
              onClick={() => {
                if (editing) {
                  setEditing(false)
                  setSeasonFormError('')
                  setSeasonForm({ name: activeSeason.name, startDate: toDateInput(activeSeason.startDate), endDate: toDateInput(activeSeason.endDate) })
                } else {
                  setEditing(true)
                  setSeasonFormError('')
                }
              }}
              className="text-gray-400 hover:text-white text-xs px-3 py-1.5 rounded-lg bg-gray-800 hover:bg-gray-700 transition-colors"
            >
              {editing ? '✕ Close Editor' : '✏️ Edit Season'}
            </button>
          </div>

          <div className="space-y-4">
            <div className="space-y-2">
              <p className="text-gray-400 text-sm font-medium">Season Details</p>
              {editing ? (
                <>
                  <input
                    className="bg-gray-800 border border-purple-600 rounded-lg px-3 py-2 text-white font-semibold text-base focus:outline-none w-full"
                    value={seasonForm.name}
                    onChange={e => setSeasonForm(f => ({ ...f, name: e.target.value }))}
                    placeholder="Season name"
                  />
                  <div className="flex flex-col sm:flex-row gap-2">
                    <input type="date"
                      className="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                      value={seasonForm.startDate}
                      onChange={e => setSeasonForm(f => ({ ...f, startDate: e.target.value }))}
                    />
                    <input type="date"
                      className="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                      value={seasonForm.endDate}
                      onChange={e => setSeasonForm(f => ({ ...f, endDate: e.target.value }))}
                    />
                  </div>
                  {seasonFormError && <ErrorMsg msg={seasonFormError} />}
                  <div className="flex gap-2">
                    <button type="button" onClick={() => saveSeasonDetails(activeSeason.id)} disabled={savingSeason}
                      className="bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-colors">
                      {savingSeason ? 'Saving…' : 'Save Changes'}
                    </button>
                    <button type="button" onClick={() => {
                      setEditing(false)
                      setSeasonFormError('')
                      setSeasonForm({ name: activeSeason.name, startDate: toDateInput(activeSeason.startDate), endDate: toDateInput(activeSeason.endDate) })
                    }}
                      className="text-gray-400 hover:text-white text-sm px-4 py-2 rounded-lg bg-gray-800 hover:bg-gray-700 transition-colors">
                      Cancel
                    </button>
                  </div>
                </>
              ) : (
                <p className="text-gray-500 text-sm">Use the edit button to adjust the season name or date range.</p>
              )}
            </div>

            <div className="border-t border-gray-800 pt-4 space-y-3">
              <div>
                <p className="text-gray-400 text-sm font-medium">Edit Current Season Standings</p>
                <p className="text-gray-500 text-xs mt-1">Update or seed totals for the active season, then save all changes in one go.</p>
              </div>

              <div className="space-y-2">
                {recordRows.map((row, index) => {
                  const selectedUserId = Number(row.userId)
                  return (
                    <div key={`${row.userId || 'blank'}-${index}`} className="grid grid-cols-[minmax(0,2fr)_repeat(2,minmax(0,1fr))_auto] gap-2 items-center">
                      <select
                        value={row.userId}
                        onChange={e => updateRecordRow(index, { userId: e.target.value })}
                        className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                      >
                        <option value="">Choose player…</option>
                        {users.map(u => {
                          const takenElsewhere = recordRows.some((other, otherIndex) => otherIndex !== index && Number(other.userId) === u.id)
                          return (
                            <option key={u.id} value={String(u.id)} disabled={takenElsewhere && selectedUserId !== u.id}>
                              {u.username}{u.isGuest ? ' (Guest)' : ''}
                            </option>
                          )
                        })}
                      </select>

                      <input
                        type="number"
                        min={0}
                        step={1}
                        className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                        value={row.totalWins}
                        onChange={e => updateRecordRow(index, { totalWins: e.target.value })}
                        placeholder="Wins"
                      />

                      <input
                        type="number"
                        min={0}
                        step={1}
                        className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                        value={row.totalLosses}
                        onChange={e => updateRecordRow(index, { totalLosses: e.target.value })}
                        placeholder="Losses"
                      />

                      <button
                        type="button"
                        onClick={() => removeRecordRow(index)}
                        className="text-red-400 hover:text-red-300 text-sm px-2 py-2 rounded-lg bg-gray-800 hover:bg-gray-700 transition-colors"
                        title="Remove row"
                      >
                        ✕
                      </button>
                    </div>
                  )
                })}
              </div>

              {recordError && <ErrorMsg msg={recordError} />}
              {recordSuccess && <p className="text-green-400 text-sm">{recordSuccess}</p>}

              <div className="flex flex-col sm:flex-row gap-2">
                <button
                  type="button"
                  onClick={addRecordRow}
                  className="flex-1 bg-gray-800 hover:bg-gray-700 text-white font-semibold py-2.5 rounded-lg text-sm transition-colors"
                >
                  + Add Player
                </button>
                <button
                  type="button"
                  onClick={() => saveRecords(activeSeason.id)}
                  disabled={savingRecords}
                  className="flex-1 bg-green-700 hover:bg-green-600 disabled:opacity-50 text-white font-semibold py-2.5 rounded-lg text-sm transition-colors"
                >
                  {savingRecords ? 'Saving…' : 'Save Standings'}
                </button>
              </div>
            </div>

            <div className="border-t border-gray-800 pt-4 flex justify-end">
              <button type="button" onClick={doRollover} disabled={rollingOver}
                className="bg-yellow-700 hover:bg-yellow-600 disabled:opacity-50 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-colors">
                {rollingOver ? 'Rolling over…' : '🔄 Rollover Season'}
              </button>
            </div>
          </div>
        </Card>
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

