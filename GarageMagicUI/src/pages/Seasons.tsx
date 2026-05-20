import { useEffect, useState } from 'react'
import { getAllSeasons, getSeasonStandings, rolloverSeason } from '../api'
import type { SeasonDto, SeasonStandingsDto } from '../types'
import { Card, Spinner, ErrorMsg, SectionHeader, Badge, WinRateBar, PrestigeBadge } from '../components/Ui'

export default function Seasons() {
  const [seasons, setSeasons] = useState<SeasonDto[]>([])
  const [selected, setSelected] = useState<number | null>(null)
  const [standings, setStandings] = useState<SeasonStandingsDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [rollingOver, setRollingOver] = useState(false)

  const load = async () => {
    try {
      const all = await getAllSeasons()
      setSeasons(all)
      const active = all.find(s => s.isActive) ?? all[0]
      if (active) {
        setSelected(active.id)
        setStandings(await getSeasonStandings(active.id))
      }
    } catch { setError('Could not load seasons.') }
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [])

  const selectSeason = async (id: number) => {
    setSelected(id)
    setStandings(null)
    try { setStandings(await getSeasonStandings(id)) } catch { /* no standings yet */ }
  }

  const doRollover = async () => {
    if (!confirm('Roll over to the next season? This will deactivate the current season.')) return
    setRollingOver(true)
    try { await rolloverSeason(); await load() }
    catch (err: unknown) { alert((err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Rollover failed') }
    finally { setRollingOver(false) }
  }

  if (loading) return <Spinner />
  if (error) return <ErrorMsg msg={error} />

  const activeSeason = seasons.find(s => s.isActive)

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <SectionHeader title="📅 Seasons" subtitle={`${seasons.length} seasons total`} />
        {activeSeason && (
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

