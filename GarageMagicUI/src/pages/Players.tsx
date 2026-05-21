import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getLeaderboard, getCurrentSeason } from '../api'
import type { UserStandingDto, SeasonDto } from '../types'
import { Card, Spinner, ErrorMsg, SectionHeader, PrestigeBadge, WinRateBar } from '../components/Ui'

export default function Players() {
  const [players, setPlayers] = useState<UserStandingDto[]>([])
  const [season, setSeason] = useState<SeasonDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const loadPlayers = async () => {
    const s = await getCurrentSeason()
    const leaderboard = await getLeaderboard(s.id)
    return { s, leaderboard }
  }

  const applyPlayers = (s: SeasonDto, leaderboard: UserStandingDto[]) => {
    setSeason(s)
    setPlayers(leaderboard)
  }

  useEffect(() => {
    let active = true

    void (async () => {
      try {
        const { s, leaderboard } = await loadPlayers()
        if (!active) return
        applyPlayers(s, leaderboard)
      } catch {
        if (active) setError('Could not load players. Is the API running?')
      } finally {
        if (active) setLoading(false)
      }
    })()

    return () => { active = false }
  }, [])

  if (loading) return <Spinner />
  if (error) return <ErrorMsg msg={error} />

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <SectionHeader title="👤 Players" subtitle={season ? `Rankings for ${season.name}` : ''} />
      </div>

      {players.length === 0 ? (
        <Card><p className="text-gray-500 text-sm">No players yet. Create an account to appear here.</p></Card>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {players.map((p, i) => (
            <Link key={p.userId} to={`/players/${p.userId}`}>
              <Card className="hover:border-purple-700 transition-colors cursor-pointer h-full">
                <div className="flex items-start justify-between mb-3">
                  <div>
                    <span className="text-2xl font-bold text-gray-600 mr-2">#{i + 1}</span>
                    <span className="text-white font-semibold text-lg">{p.username}</span>
                  </div>
                  {p.prestigeLevel > 0 && <PrestigeBadge level={p.prestigeLevel} />}
                </div>
                <div className="grid grid-cols-3 gap-2 text-center text-sm mb-3">
                  <div className="bg-gray-800 rounded-lg py-2">
                    <p className="text-green-400 font-bold text-lg">{p.totalWins}</p>
                    <p className="text-gray-500 text-xs">Wins</p>
                  </div>
                  <div className="bg-gray-800 rounded-lg py-2">
                    <p className="text-red-400 font-bold text-lg">{p.totalLosses}</p>
                    <p className="text-gray-500 text-xs">Losses</p>
                  </div>
                  <div className="bg-gray-800 rounded-lg py-2">
                    <p className="text-purple-400 font-bold text-lg">{p.winRate.toFixed(0)}%</p>
                    <p className="text-gray-500 text-xs">Win Rate</p>
                  </div>
                </div>
                <WinRateBar winRate={p.winRate} />
                <p className="text-gray-600 text-xs mt-2">{p.totalMatches} matches played</p>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}

