import { useEffect, useState } from 'react'
import { getPendingUsers, approveUser, rejectUser, createGuest, getGuests, deleteUser } from '../api'
import type { PendingUserDto, UserDto } from '../types'
import { Card, Spinner, ErrorMsg, SectionHeader, GuestBadge } from '../components/Ui'

export default function AdminPanel() {
  const [pending, setPending] = useState<PendingUserDto[]>([])
  const [guests, setGuests] = useState<UserDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [actioning, setActioning] = useState<number | null>(null)
  const [guestName, setGuestName] = useState('')
  const [guestAdding, setGuestAdding] = useState(false)
  const [guestError, setGuestError] = useState('')

  const load = async () => {
    try {
      const [p, g] = await Promise.all([getPendingUsers(), getGuests()])
      setPending(p)
      setGuests(g)
    } catch {
      setError('Could not load data.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const approve = async (id: number) => {
    setActioning(id)
    try { await approveUser(id); await load() }
    finally { setActioning(null) }
  }

  const reject = async (id: number) => {
    if (!confirm('Reject and delete this registration?')) return
    setActioning(id)
    try { await rejectUser(id); await load() }
    finally { setActioning(null) }
  }

  const addGuest = async (e: React.FormEvent) => {
    e.preventDefault()
    setGuestError('')
    setGuestAdding(true)
    try {
      await createGuest({ displayName: guestName.trim() })
      setGuestName('')
      await load()
    } catch {
      setGuestError('Could not create guest.')
    } finally {
      setGuestAdding(false)
    }
  }

  const removeGuest = async (id: number, name: string) => {
    if (!confirm(`Remove guest "${name}"? This will delete their match history.`)) return
    setActioning(id)
    try { await deleteUser(id); await load() }
    finally { setActioning(null) }
  }

  if (loading) return <Spinner />
  if (error) return <ErrorMsg msg={error} />

  return (
    <div className="space-y-8">
      <SectionHeader title="⚙️ Admin Panel" />

      {/* Pending Approvals */}
      <div>
        <h2 className="text-lg font-semibold text-white mb-3">
          Pending Approvals
          {pending.length > 0 && <span className="ml-2 bg-red-700 text-white text-xs font-bold px-2 py-0.5 rounded-full">{pending.length}</span>}
        </h2>
        {pending.length === 0 ? (
          <Card><p className="text-gray-500 text-sm">✅ No pending registrations.</p></Card>
        ) : (
          <div className="space-y-3">
            {pending.map(u => (
              <Card key={u.id} className="flex items-center justify-between">
                <div>
                  <p className="text-white font-semibold">{u.username}</p>
                  <p className="text-gray-500 text-xs">{u.email} · {new Date(u.createdAt).toLocaleDateString()}</p>
                </div>
                <div className="flex gap-2">
                  <button onClick={() => approve(u.id)} disabled={actioning === u.id}
                    className="bg-green-700 hover:bg-green-600 disabled:opacity-50 text-white text-sm font-medium px-4 py-1.5 rounded-lg transition-colors">
                    ✓ Approve
                  </button>
                  <button onClick={() => reject(u.id)} disabled={actioning === u.id}
                    className="bg-red-800 hover:bg-red-700 disabled:opacity-50 text-white text-sm font-medium px-4 py-1.5 rounded-lg transition-colors">
                    ✕ Reject
                  </button>
                </div>
              </Card>
            ))}
          </div>
        )}
      </div>

      {/* Guest Players */}
      <div>
        <h2 className="text-lg font-semibold text-white mb-3">Guest Players</h2>
        <Card className="mb-3">
          <p className="text-gray-400 text-sm mb-3">Add a guest for a one-off player who doesn't have an account.</p>
          <form onSubmit={addGuest} className="flex gap-2">
            <input
              className="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
              placeholder="Guest display name…"
              value={guestName}
              onChange={e => setGuestName(e.target.value)}
              required
            />
            <button type="submit" disabled={guestAdding}
              className="bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white font-semibold px-4 py-2 rounded-lg text-sm transition-colors whitespace-nowrap">
              {guestAdding ? 'Adding…' : '+ Add Guest'}
            </button>
          </form>
          {guestError && <div className="mt-2"><ErrorMsg msg={guestError} /></div>}
        </Card>
        {guests.length > 0 && (
          <div className="space-y-2">
            {guests.map(g => (
              <Card key={g.id} className="flex items-center justify-between py-3">
                <div className="flex items-center gap-2">
                  <span className="text-white font-medium">{g.username}</span>
                  <GuestBadge />
                </div>
                <button onClick={() => removeGuest(g.id, g.username)} disabled={actioning === g.id}
                  className="text-red-500 hover:text-red-400 disabled:opacity-50 text-sm transition-colors">
                  Remove
                </button>
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
