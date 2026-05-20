export function Card({ children, className = '' }: { children: React.ReactNode; className?: string }) {
  return (
    <div className={`bg-gray-900 border border-gray-800 rounded-xl p-5 ${className}`}>
      {children}
    </div>
  )
}

export function Badge({ children, color = 'purple' }: { children: React.ReactNode; color?: string }) {
  const colors: Record<string, string> = {
    purple: 'bg-purple-900 text-purple-300',
    green: 'bg-green-900 text-green-300',
    red: 'bg-red-900 text-red-300',
    yellow: 'bg-yellow-900 text-yellow-300',
    blue: 'bg-blue-900 text-blue-300',
    gray: 'bg-gray-800 text-gray-400',
  }
  return (
    <span className={`text-xs font-semibold px-2 py-0.5 rounded-full ${colors[color] ?? colors.purple}`}>
      {children}
    </span>
  )
}

export function Spinner() {
  return (
    <div className="flex justify-center py-12">
      <div className="w-8 h-8 border-4 border-purple-500 border-t-transparent rounded-full animate-spin" />
    </div>
  )
}

export function ErrorMsg({ msg }: { msg: string }) {
  return (
    <div className="bg-red-900/40 border border-red-700 text-red-300 rounded-lg px-4 py-3 text-sm">
      ⚠️ {msg}
    </div>
  )
}

export function SectionHeader({ title, subtitle }: { title: string; subtitle?: string }) {
  return (
    <div className="mb-6">
      <h1 className="text-2xl font-bold text-white">{title}</h1>
      {subtitle && <p className="text-gray-400 mt-1 text-sm">{subtitle}</p>}
    </div>
  )
}

export function PrestigeBadge({ level }: { level: number }) {
  if (level === 0) return null
  const stars = '⭐'.repeat(Math.min(level, 5))
  return <Badge color="yellow">{stars} Prestige {level}</Badge>
}

export function ColorPips({ colors }: { colors?: string }) {
  if (!colors) return null
  const map: Record<string, string> = { W: 'bg-yellow-100', U: 'bg-blue-500', B: 'bg-gray-700', R: 'bg-red-500', G: 'bg-green-600', C: 'bg-gray-400' }
  return (
    <span className="flex gap-0.5 items-center">
      {colors.split('').map((c, i) => (
        <span key={i} className={`w-3 h-3 rounded-full inline-block border border-gray-600 ${map[c] ?? 'bg-gray-500'}`} title={c} />
      ))}
    </span>
  )
}

export function WinRateBar({ winRate }: { winRate: number }) {
  return (
    <div className="w-full bg-gray-800 rounded-full h-1.5 mt-1">
      <div
        className="bg-purple-500 h-1.5 rounded-full transition-all"
        style={{ width: `${Math.min(winRate, 100)}%` }}
      />
    </div>
  )
}

export function GuestBadge() {
  return <Badge color="gray">👤 Guest</Badge>
}

