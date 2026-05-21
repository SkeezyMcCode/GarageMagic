import { useEffect, useState } from 'react'
import { getAllSeasons, getAllUsers, getSeasonStandings, rolloverSeason, updateSeason, upsertSeasonRecord } from '../api'
import { useAuth } from '../context/useAuth'
import type { SeasonDto, SeasonStandingsDto, UserDto } from '../types'
import { Card, Spinner, ErrorMsg, SectionHeader, Badge, WinRateBar, PrestigeBadge } from '../components/Ui'

export default function Seasons() {
  const { user } = useAuth()
  const [seasons, setSeasons] = useState<SeasonDto[]>([])
  const [users, setUsers] = useState<UserDto[]>([])
  const [selected, setSelected] = useState<number | null>(null)
  const [standings, setStandings] = useState<SeasonStandingsDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [rollingOver, setRollingOver] = useState(false)
  const [savingSeason, setSavingSeason] = useState(false)
  const [savingRecord, setSavingRecord] = useState(false)
  const [adminError, setAdminError] = useState('')
  const [adminSuccess, setAdminSuccess] = useState('')
  const [seasonForm, setSeasonForm] = useState({ name: '', startDate: '', endDate: '' })
  const [recordForm, setRecordForm] = useState({ userId: '', totalWins: '0', totalLosses: '0' })

  const loadSeasons = async () => {
    const all = await getAllSeasons()
    const active = all.find(s => s.isActive) ?? all[0]
    const activeStandings = active ? await getSeasonStandings(active.id) : null
    return { all, active, activeStandings }
  }

  const applySeasons = (all: SeasonDto[], active: SeasonDto | undefined, activeStandings: SeasonStandingsDto | null) => {
    setSeasons(all)
    if (active) setSelected(active.id)
    setStandings(activeStandings)
  }

  const toDateInputValue = (iso: string) => {
    const parsed = new Date(iso)
    if (Number.isNaN(parsed.getTime())) return ''
    return parsed.toISOString().slice(0, 10)
  }

  useEffect(() => {
    let active = true

    void (async () => {
      try {
        const data = await loadSeasons()
        if (!active) return
        applySeasons(data.all, data.active, data.activeStandings)
        if (user?.isAdmin) {
          const allUsers = await getAllUsers()
          if (!active) return
          setUsers(allUsers.filter(u => u.isApproved || u.isGuest))
        }
      } catch {
        if (active) setError('Could not load seasons.')
      } finally {
        if (active) setLoading(false)
      }
    })()

    return () => { active = false }
  }, [user?.isAdmin])

  useEffect(() => {
    if (!selected) return
    const season = seasons.find(s => s.id === selected)
    if (!season) return
    setSeasonForm({
      name: season.name,
      startDate: toDateInputValue(season.startDate),
      endDate: toDateInputValue(season.endDate),
    })
  }, [selected, seasons])

  const selectSeason = async (id: number) => {
    setAdminError('')
    setAdminSuccess('')
    setSelected(id)
    setStandings(null)
    try { setStandings(await getSeasonStandings(id)) } catch { /* no standings yet */ }
  }

  const doRollover = async () => {
    if (!confirm('Roll over to the next season? This will deactivate the current season.')) return
    setRollingOver(true)
    try {
      await rolloverSeason()
      const data = await loadSeasons()
      applySeasons(data.all, data.active, data.activeStandings)
    }
    catch (err: unknown) { alert((err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Rollover failed') }
    finally { setRollingOver(false) }
  }

  const saveSeasonDetails = async () => {
    if (!selected) return
    if (!seasonForm.name.trim() || !seasonForm.startDate || !seasonForm.endDate) {
      setAdminError('Season name, start date, and end date are required.')
      return
    }
    if (seasonForm.startDate > seasonForm.endDate) {
      setAdminError('Season start date must be on or before end date.')
      return
    }

    setAdminError('')
    setAdminSuccess('')
    setSavingSeason(true)
    try {
      await updateSeason(selected, {
        name: seasonForm.name.trim(),
        startDate: new Date(seasonForm.startDate).toISOString(),
        endDate: new Date(seasonForm.endDate).toISOString(),
      })
      const data = await loadSeasons()
      applySeasons(data.all, data.active, data.activeStandings)
      setSelected(selected)
      try { setStandings(await getSeasonStandings(selected)) } catch { setStandings(null) }
      setAdminSuccess('Season details updated.')
    }
    catch {
      setAdminError('Could not update season details.')
    }
    finally {
      setSavingSeason(false)
    }
  }

  const saveSeasonRecord = async () => {
    if (!selected) return
    const userId = Number(recordForm.userId)
    const totalWins = Number(recordForm.totalWins)
    const totalLosses = Number(recordForm.totalLosses)

    if (!userId || !Number.isInteger(userId)) {
      setAdminError('Choose a player or guest to seed records for this season.')
      return
    }
    if (!Number.isInteger(totalWins) || totalWins < 0 || !Number.isInteger(totalLosses) || totalLosses < 0) {
      setAdminError('Wins and losses must be whole numbers 0 or greater.')
      return
    }

    setAdminError('')
    setAdminSuccess('')
    setSavingRecord(true)
    try {
      await upsertSeasonRecord(selected, { userId, totalWins, totalLosses })
      try { setStandings(await getSeasonStandings(selected)) } catch { setStandings(null) }
      setRecordForm({ userId: '', totalWins: '0', totalLosses: '0' })
      setAdminSuccess('Season record saved.')
    }
    catch {
      setAdminError('Could not save season record. Ensure the backend supports season record upserts.')
    }
    finally {
      setSavingRecord(false)
    }
  }

  if (loading) return <Spinner />
  if (error) return <ErrorMsg msg={error} />

  const activeSeason = seasons.find(s => s.isActive)
  const selectedSeason = selected ? seasons.find(s => s.id === selected) : null

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <SectionHeader title="📅 Seasons" subtitle={`${seasons.length} seasons total`} />
        {activeSeason && user?.isAdmin && (
          <button onClick={doRollover} disabled={rollingOver}
            className="bg-yellow-700 hover:bg-yellow-600 disabled:opacity-50 text-white font-semibold px-4 py-2 rounded-lg text-sm transition-colors">
            {rollingOver ? 'Rolling over…' : '🔄 Rollover Season'}
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Season list */}
        <div className="space-y-2">
          {seasons.map(s => (
            <button key={s.id} onClick={() => selectSeason(s.id)}
              className={`w-full text-left px-4 py-3 rounded-xl border transition-colors ${selected === s.id ? 'bg-purple-900/40 border-purple-700 text-white' : 'bg-gray-900 border-gray-800 text-gray-400 hover:border-gray-700'}`}>
              <div className="flex items-center justify-between">
                <span className="font-medium">{s.name}</span>
                {s.isActive && <Badge color="green">Active</Badge>}
              </div>
              <p className="text-xs text-gray-600 mt-0.5">
                {new Date(s.startDate).toLocaleDateString()} – {new Date(s.endDate).toLocaleDateString()}
              </p>
            </button>
          ))}
        </div>

        {/* Standings */}
        <div className="lg:col-span-3">
          {user?.isAdmin && selectedSeason && (
            <Card className="mb-6">
              <h3 className="font-semibold text-white mb-4">Season Management</h3>
              <p className="text-gray-400 text-sm mb-4">
                Use this to correct mid-season setup: update dates/name and seed wins/losses for current players or guests.
              </p>

              {adminError && <div className="mb-3"><ErrorMsg msg={adminError} /></div>}
              {adminSuccess && <p className="text-green-400 text-sm mb-3">{adminSuccess}</p>}

              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-6">
                <input
                  className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                  value={seasonForm.name}
                  onChange={e => setSeasonForm(current => ({ ...current, name: e.target.value }))}
                  placeholder="Season name"
                />
                <input
                  type="date"
                  className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                  value={seasonForm.startDate}
                  onChange={e => setSeasonForm(current => ({ ...current, startDate: e.target.value }))}
                />
                <input
                  type="date"
                  className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                  value={seasonForm.endDate}
                  onChange={e => setSeasonForm(current => ({ ...current, endDate: e.target.value }))}
                />
              </div>

              <div className="flex justify-end mb-6">
                <button
                  onClick={saveSeasonDetails}
                  disabled={savingSeason}
                  className="bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white font-semibold px-4 py-2 rounded-lg text-sm transition-colors"
                >
                  {savingSeason ? 'Saving season…' : 'Save Season Details'}
                </button>
              </div>

              <h4 className="text-white font-medium mb-3">Seed or Correct Player Records</h4>
              <div className="grid grid-cols-1 sm:grid-cols-4 gap-3">
                <select
                  value={recordForm.userId}
                  onChange={e => setRecordForm(current => ({ ...current, userId: e.target.value }))}
                  className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                >
                  <option value="">Choose player/guest…</option>
                  {users.map(u => (
                    <option key={u.id} value={String(u.id)}>{u.username}{u.isGuest ? ' (Guest)' : ''}</option>
                  ))}
                </select>
                <input
                  type="number"
                  min={0}
                  step={1}
                  className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                  value={recordForm.totalWins}
                  onChange={e => setRecordForm(current => ({ ...current, totalWins: e.target.value }))}
                  placeholder="Wins"
                />
                <input
                  type="number"
                  min={0}
                  step={1}
                  className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                  value={recordForm.totalLosses}
                  onChange={e => setRecordForm(current => ({ ...current, totalLosses: e.target.value }))}
                  placeholder="Losses"
                />
                <button
                  onClick={saveSeasonRecord}
                  disabled={savingRecord}
                  className="bg-green-700 hover:bg-green-600 disabled:opacity-50 text-white font-semibold px-4 py-2 rounded-lg text-sm transition-colors"
                >
                  {savingRecord ? 'Saving…' : 'Save Record'}
                </button>
              </div>
            </Card>
          )}

          {standings ? (
            <Card>
              <h3 className="font-semibold text-white mb-4">
                🏆 {standings.season.name} Final Standings
              </h3>
              {standings.standings.length === 0 ? (
                <p className="text-gray-500 text-sm">No matches recorded this season.</p>
              ) : (
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-gray-500 border-b border-gray-800">
                      <th className="text-left pb-2">#</th>
                      <th className="text-left pb-2">Player</th>
                      <th className="text-right pb-2">Wins</th>
                      <th className="text-right pb-2">Losses</th>
                      <th className="text-right pb-2">Matches</th>
                      <th className="text-right pb-2">Win %</th>
                      <th className="text-right pb-2">Prestige</th>
                    </tr>
                  </thead>
                  <tbody>
                    {standings.standings.map((p, i) => (
                      <tr key={p.userId} className="border-b border-gray-800/50">
                        <td className="py-2.5 text-gray-600 font-bold">
                          {i === 0 ? '🥇' : i === 1 ? '🥈' : i === 2 ? '🥉' : i + 1}
                        </td>
                        <td className="py-2.5">
                          <div>
                            <span className="text-white font-medium">{p.username}</span>
                            <WinRateBar winRate={p.winRate} />
                          </div>
                        </td>
                        <td className="py-2.5 text-right text-green-400 font-semibold">{p.totalWins}</td>
                        <td className="py-2.5 text-right text-red-400">{p.totalLosses}</td>
                        <td className="py-2.5 text-right text-gray-400">{p.totalMatches}</td>
                        <td className="py-2.5 text-right text-purple-400">{p.winRate.toFixed(1)}%</td>
                        <td className="py-2.5 text-right">
                          {p.prestigeLevel > 0 ? <PrestigeBadge level={p.prestigeLevel} /> : <span className="text-gray-700">—</span>}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </Card>
          ) : (
            <Card><p className="text-gray-500 text-sm">Select a season to view standings.</p></Card>
          )}
        </div>
      </div>
    </div>
  )
}

