import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getMatchesBySeason, getCurrentSeason, getAllSeasons } from '../api'
import type { MatchDto, SeasonDto } from '../types'
import { Card, Spinner, ErrorMsg, SectionHeader, Badge } from '../components/Ui'

const MATCH_TYPE_LABEL: Record<string, string> = {
  OneVsOneVsOne: '1v1v1', OneVsOneVsOneVsOne: '1v1v1v1',
  FivePlayerSheriff: '5P Sheriff', SixPlayerSheriff: '6P Sheriff',
}

const ROLE_STYLE: Record<string, { symbol: string; text: string; className: string }> = {
  Sheriff: { symbol: '●', text: 'Sheriff', className: 'text-white' },
  Deputy: { symbol: '●', text: 'Deputy', className: 'text-blue-400' },
  Red: { symbol: '●', text: 'Renegade', className: 'text-red-400' },
  Renegade: { symbol: '●', text: 'Renegade', className: 'text-red-400' },
  Matriarch: { symbol: '●', text: 'Matriarch', className: 'text-green-400' },
  Outlaw: { symbol: '●', text: 'Outlaw', className: 'text-gray-900 bg-gray-200 rounded-full px-1' },
}

export default function Matches() {
  const [matches, setMatches] = useState<MatchDto[]>([])
  const [seasons, setSeasons] = useState<SeasonDto[]>([])
  const [selectedSeason, setSelectedSeason] = useState<number | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    Promise.all([getCurrentSeason(), getAllSeasons()])
      .then(async ([current, all]) => {
        setSeasons(all)
        setSelectedSeason(current.id)
        setMatches(await getMatchesBySeason(current.id))
      })
      .catch(() => setError('Could not load matches.'))
      .finally(() => setLoading(false))
  }, [])

  const changeSeason = async (id: number) => {
    setSelectedSeason(id)
    setLoading(true)
    try { setMatches(await getMatchesBySeason(id)) }
    finally { setLoading(false) }
  }

  if (loading) return <Spinner />
  if (error) return <ErrorMsg msg={error} />

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-6">
        <SectionHeader title="⚔️ Matches" subtitle={`${matches.length} matches recorded`} />
        <div className="flex items-center gap-2">
          <select
            className="flex-1 sm:flex-none bg-gray-800 border border-gray-700 text-white text-sm rounded-lg px-3 py-2.5 focus:outline-none focus:border-purple-500"
            value={selectedSeason ?? ''}
            onChange={e => changeSeason(Number(e.target.value))}
          >
            {seasons.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
          <Link to="/matches/new" className="bg-purple-600 hover:bg-purple-500 text-white font-semibold px-4 py-2.5 rounded-lg text-sm transition-colors whitespace-nowrap">
            + Record
          </Link>
        </div>
      </div>

      {matches.length === 0 ? (
        <Card><p className="text-gray-500 text-sm">No matches this season. <Link to="/matches/new" className="text-purple-400 hover:underline">Record one!</Link></p></Card>
      ) : (
        <div className="space-y-3">
          {matches.map(m => (
            <Card key={m.id} className="hover:border-gray-700 transition-colors">
              {(() => {
                const winnerIds = new Set(m.winners.map(w => w.userId))
                const orderedParticipants = [...m.participants].sort((a, b) => {
                  const aWon = winnerIds.has(a.userId)
                  const bWon = winnerIds.has(b.userId)
                  if (aWon === bWon) return 0
                  return aWon ? -1 : 1
                })

                return (
              <div className="flex items-start gap-4">
                <div className="shrink-0">
                  <Badge color="purple">{MATCH_TYPE_LABEL[m.matchType]}</Badge>
                  <p className="text-gray-600 text-xs mt-1">{new Date(m.matchDate).toLocaleDateString()}</p>
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-xs text-gray-500 mb-2">Type: {MATCH_TYPE_LABEL[m.matchType]}</p>
                  <div className="flex flex-wrap gap-2 mb-2">
                    {orderedParticipants.map(p => {
                      const won = m.winners.some(w => w.userId === p.userId)
                      const role = p.hiddenRole ? (ROLE_STYLE[p.hiddenRole] ?? { symbol: '●', text: p.hiddenRole, className: 'text-black bg-gray-200 rounded-full px-1' }) : null
                      return (
                        <Link key={p.userId} to={`/players/${p.userId}`}>
                          <span className={`inline-flex items-center gap-1 text-sm px-2 py-0.5 rounded-full border ${won ? 'border-green-700 bg-green-900/20 text-green-300' : 'border-gray-700 bg-gray-800/50 text-gray-400'}`}>
                            {won && '👑 '}{p.username}
                            {p.deckName && <span className="text-xs opacity-60">({p.deckName})</span>}
                            {role && (
                              <span title={role.text} className={`text-xs font-semibold ${role.className}`}>
                                {role.symbol}
                              </span>
                            )}
                          </span>
                        </Link>
                      )
                    })}
                  </div>
                </div>
              </div>
                )
              })()}
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}

