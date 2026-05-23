import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getLeaderboard, getCurrentSeason, getMatchesBySeason, getRecentBetrayals } from '../api'
import type { UserStandingDto, SeasonDto, MatchDto, BetrayalDto } from '../types'
import { Card, Spinner, ErrorMsg, PrestigeBadge, WinRateBar, Badge } from '../components/Ui'
import InlineMarkdown from '../components/InlineMarkdown'

const MATCH_TYPE_LABEL: Record<string, string> = {
  OneVsOneVsOne: '1v1v1',
  OneVsOneVsOneVsOne: '1v1v1v1',
  FivePlayerSheriff: '5P Sheriff',
  SixPlayerSheriff: '6P Sheriff',
}

export default function Dashboard() {
  const [season, setSeason] = useState<SeasonDto | null>(null)
  const [leaderboard, setLeaderboard] = useState<UserStandingDto[]>([])
  const [matches, setMatches] = useState<MatchDto[]>([])
  const [betrayals, setBetrayals] = useState<BetrayalDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    Promise.all([getCurrentSeason(), getRecentBetrayals(20)])
      .then(async ([s, b]) => {
        setSeason(s)
        setBetrayals(b)
        const [lb, m] = await Promise.all([
          getLeaderboard(s.id),
          getMatchesBySeason(s.id),
        ])
        setLeaderboard(lb)
        setMatches(m.slice(0, 5))
      })
      .catch(() => setError('Could not load data. Is the API running?'))
      .finally(() => setLoading(false))
  }, [])

  if (loading) return <Spinner />
  if (error) return <ErrorMsg msg={error} />

  return (
    <div className="space-y-6">
      {/* Season banner */}
      {season && (
        <Card className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
          <div>
            <p className="text-xs text-gray-500 uppercase tracking-widest">Active Season</p>
            <h2 className="text-2xl font-bold text-purple-400">{season.name}</h2>
            <p className="text-gray-500 text-sm mt-0.5">
              {new Date(season.startDate).toLocaleDateString()} – {new Date(season.endDate).toLocaleDateString()}
            </p>
          </div>
          <Link to="/matches/new" className="bg-purple-600 hover:bg-purple-500 text-white font-semibold px-5 py-3 rounded-lg transition-colors text-sm text-center">
            ⚔️ Record Match
          </Link>
        </Card>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Leaderboard */}
        <div className="lg:col-span-2">
          <Card>
            <h3 className="font-semibold text-white mb-4 flex items-center gap-2">🏆 Season Standings</h3>
            {leaderboard.length === 0 ? (
              <p className="text-gray-500 text-sm">No matches recorded yet.</p>
            ) : (
              <div className="space-y-3">
                {leaderboard.map((p, i) => (
                  <Link key={p.userId} to={`/players/${p.userId}`} className="flex items-center gap-3 group">
                    <span className={`text-lg font-bold w-7 text-center ${i === 0 ? 'text-yellow-400' : i === 1 ? 'text-gray-300' : i === 2 ? 'text-amber-600' : 'text-gray-600'}`}>
                      {i === 0 ? '🥇' : i === 1 ? '🥈' : i === 2 ? '🥉' : `${i + 1}`}
                    </span>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="font-medium text-white group-hover:text-purple-400 transition-colors truncate">{p.username}</span>
                        {p.prestigeLevel > 0 && <PrestigeBadge level={p.prestigeLevel} />}
                      </div>
                      <WinRateBar winRate={p.winRate} />
                    </div>
                    <div className="text-right shrink-0">
                      <p className="text-white font-semibold">{p.totalWins}W <span className="text-gray-500">{p.totalLosses}L</span></p>
                      <p className="text-gray-500 text-xs">{p.winRate.toFixed(1)}% win</p>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </Card>
        </div>

        {/* Recent betrayals */}
        <Card className="lg:flex lg:flex-col lg:h-full">
          <h3 className="font-semibold text-white mb-4 flex items-center gap-2">🗡️ Recent Betrayals</h3>
          {betrayals.length === 0 ? (
            <p className="text-gray-500 text-sm">No betrayals recorded.</p>
          ) : (
            <div className="space-y-3 max-h-48 overflow-hidden lg:max-h-none lg:overflow-y-auto lg:flex-1 lg:min-h-0 lg:pr-1">
              {betrayals.map(b => (
                <div key={b.id} className="text-sm border-l-2 border-red-700 pl-3">
                  <p className="text-white">
                    <span className="text-red-400 font-medium">{b.betrayerUsername}</span>
                    <span className="text-gray-500"> stabbed </span>
                    <span className="text-purple-400 font-medium">{b.victimUsername}</span>
                  </p>
                  <InlineMarkdown text={b.description} className="text-gray-500 text-xs mt-0.5 line-clamp-2 block" />
                </div>
              ))}
            </div>
          )}
          <Link to="/betrayals" className="block mt-4 text-xs text-purple-400 hover:text-purple-300 lg:mt-auto lg:pt-3 shrink-0">View all →</Link>
        </Card>
      </div>

      {/* Recent matches */}
      <Card>
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-semibold text-white">⚔️ Recent Matches</h3>
          <Link to="/matches" className="text-xs text-purple-400 hover:text-purple-300">View all →</Link>
        </div>
        {matches.length === 0 ? (
          <p className="text-gray-500 text-sm">No matches yet. <Link to="/matches/new" className="text-purple-400 hover:underline">Record the first one!</Link></p>
        ) : (
          <div className="space-y-2">
            {matches.map(m => (
              <div key={m.id} className="flex flex-col sm:flex-row sm:items-center gap-2 bg-gray-800/50 rounded-lg px-4 py-3">
                <div className="flex items-center gap-2">
                  <Badge color="purple">{MATCH_TYPE_LABEL[m.matchType] ?? m.matchType}</Badge>
                  <span className="text-gray-400 text-sm">{new Date(m.matchDate).toLocaleDateString()}</span>
                </div>
                <span className="text-gray-500 text-sm sm:flex-1 truncate">
                  {m.participants.map(p => p.username).join(', ')}
                </span>
                <div className="flex items-center gap-1 flex-wrap">
                  <span className="text-xs text-gray-500">Won:</span>
                  {m.winners.map(w => (
                    <Badge key={w.userId} color="green">{w.username}</Badge>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  )
}

