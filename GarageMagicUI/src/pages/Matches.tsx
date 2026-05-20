import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getMatchesBySeason, getCurrentSeason, getAllSeasons } from '../api'
import type { MatchDto, SeasonDto } from '../types'
import { Card, Spinner, ErrorMsg, SectionHeader, Badge } from '../components/Ui'

const MATCH_TYPE_LABEL: Record<string, string> = {
  OneVsOneVsOne: '1v1v1', OneVsOneVsOneVsOne: '1v1v1v1',
  FivePlayerSheriff: '5P Sheriff', SixPlayerSheriff: '6P Sheriff',
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
      <div className="flex items-center justify-between mb-6">
        <SectionHeader title="⚔️ Matches" subtitle={`${matches.length} matches recorded`} />
        <div className="flex items-center gap-3">
          <select
            className="bg-gray-800 border border-gray-700 text-white text-sm rounded-lg px-3 py-2 focus:outline-none focus:border-purple-500"
            value={selectedSeason ?? ''}
            onChange={e => changeSeason(Number(e.target.value))}
          >
            {seasons.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
          <Link to="/matches/new" className="bg-purple-600 hover:bg-purple-500 text-white font-semibold px-4 py-2 rounded-lg text-sm transition-colors">
            + Record Match
          </Link>
        </div>
      </div>

      {matches.length === 0 ? (
        <Card><p className="text-gray-500 text-sm">No matches this season. <Link to="/matches/new" className="text-purple-400 hover:underline">Record one!</Link></p></Card>
      ) : (
        <div className="space-y-3">
          {matches.map(m => (
            <Card key={m.id} className="hover:border-gray-700 transition-colors">
              <div className="flex items-start gap-4">
                <div className="shrink-0">
                  <Badge color="purple">{MATCH_TYPE_LABEL[m.matchType]}</Badge>
                  <p className="text-gray-600 text-xs mt-1">{new Date(m.matchDate).toLocaleDateString()}</p>
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex flex-wrap gap-2 mb-2">
                    {m.participants.map(p => {
                      const won = m.winners.some(w => w.userId === p.userId)
                      return (
                        <Link key={p.userId} to={`/players/${p.userId}`}>
                          <span className={`inline-flex items-center gap-1 text-sm px-2 py-0.5 rounded-full border ${won ? 'border-green-700 bg-green-900/20 text-green-300' : 'border-gray-700 bg-gray-800/50 text-gray-400'}`}>
                            {won && '👑 '}{p.username}
                            {p.deckName && <span className="text-xs opacity-60">({p.deckName})</span>}
                            {p.hiddenRole && <Badge color={p.hiddenRole === 'Sheriff' ? 'yellow' : p.hiddenRole === 'Deputy' ? 'blue' : 'red'}>{p.hiddenRole}</Badge>}
                          </span>
                        </Link>
                      )
                    })}
                  </div>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}

