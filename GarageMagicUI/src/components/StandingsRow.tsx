import { Link } from 'react-router-dom'
import type { UserStandingDto } from '../types'
import { PrestigeBadge, WinsBar } from './Ui'

interface StandingsRowProps {
  player: UserStandingDto
  rank: number
  maxWins: number
  /** Show "(Guest)" label — default false */
  showGuest?: boolean
  /** Wrap the row in a Link to the player detail page — default true */
  linkable?: boolean
}

export default function StandingsRow({ player, rank, maxWins, showGuest = false, linkable = true }: StandingsRowProps) {
  const medal = rank === 0 ? '🥇' : rank === 1 ? '🥈' : rank === 2 ? '🥉' : null
  const rankLabel = medal ?? (
    <span className="text-gray-600 font-bold text-sm">{rank + 1}</span>
  )

  const inner = (
    <div className="flex items-center gap-3 py-2 border-b border-gray-800/50 last:border-0">
      <span className="w-7 text-center text-lg shrink-0">{rankLabel}</span>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-1.5 flex-wrap">
          <span className="font-medium text-white truncate">{player.username}</span>
          {showGuest && player.isGuest && <span className="text-gray-600 text-xs">(Guest)</span>}
          {player.prestigeLevel > 0 && <PrestigeBadge level={player.prestigeLevel} />}
        </div>
        <WinsBar wins={player.totalWins} maxWins={maxWins} />
      </div>
      <div className="text-right shrink-0">
        <p className="text-white text-sm font-semibold">
          <span className="text-green-400">{player.totalWins}W</span>
          <span className="text-gray-500 mx-1">/</span>
          <span className="text-red-400">{player.totalLosses}L</span>
        </p>
        <p className="text-gray-500 text-xs">{player.totalMatches} games</p>
      </div>
    </div>
  )

  return linkable
    ? <Link to={`/players/${player.userId}`} className="block group hover:bg-gray-800/30 rounded transition-colors">{inner}</Link>
    : inner
}

