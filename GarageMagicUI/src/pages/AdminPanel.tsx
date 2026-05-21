import { useEffect, useState } from 'react'
import { getPendingUsers, approveUser, approveAndLinkUser, rejectUser, createGuest, getGuests, deleteUser, getAllUsers, setUserAdminStatus } from '../api'
import { useAuth } from '../context/useAuth'
import type { PendingUserDto, UserDto } from '../types'
import { Card, Spinner, ErrorMsg, SectionHeader, GuestBadge, Badge } from '../components/Ui'

export default function AdminPanel() {
  const { user } = useAuth()
  const [pending, setPending] = useState<PendingUserDto[]>([])
  const [guests, setGuests] = useState<UserDto[]>([])
  const [approvedUsers, setApprovedUsers] = useState<UserDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [actioning, setActioning] = useState<number | null>(null)
  const [adminTogglingId, setAdminTogglingId] = useState<number | null>(null)
  const [pendingGuestLinks, setPendingGuestLinks] = useState<Record<number, string>>({})
  const [actionError, setActionError] = useState('')
  const [guestName, setGuestName] = useState('')
  const [guestAdding, setGuestAdding] = useState(false)
  const [guestError, setGuestError] = useState('')

  const loadAdminData = async () => Promise.all([getPendingUsers(), getGuests(), getAllUsers()])

  const applyAdminData = (p: PendingUserDto[], g: UserDto[], users: UserDto[]) => {
    setPending(p)
    setGuests(g)
    setApprovedUsers(users.filter(u => u.isApproved && !u.isGuest))
  }

  useEffect(() => {
    let active = true

    void (async () => {
      try {
        const [p, g, users] = await loadAdminData()
        if (!active) return
        applyAdminData(p, g, users)
      } catch {
        if (active) setError('Could not load data.')
      } finally {
        if (active) setLoading(false)
      }
    })()

    return () => { active = false }
  }, [])

  const approve = async (id: number) => {
    setActionError('')
    const guestUserId = Number(pendingGuestLinks[id])
    if (guestUserId > 0) {
      const pendingUser = pending.find(u => u.id === id)
      const guest = guests.find(g => g.id === guestUserId)
      if (!pendingUser || !guest) {
        setActionError('Selected guest could not be found. Refresh and try again.')
        return
      }

      const confirmed = confirm(`Approve "${pendingUser.username}" and link guest history from "${guest.username}"?`)
      if (!confirmed) return
    }

    setActioning(id)
    try {
      if (guestUserId > 0) {
        await approveAndLinkUser(id, guestUserId)
      } else {
        await approveUser(id)
      }
      const [p, g, users] = await loadAdminData()
      applyAdminData(p, g, users)
      setPendingGuestLinks(current => {
        const next = { ...current }
        delete next[id]
        return next
      })
    }
    catch {
      setActionError('Could not approve user. If linking was selected, verify the backend supports approve-and-link.')
    }
    finally { setActioning(null) }
  }

  const reject = async (id: number) => {
    if (!confirm('Reject and delete this registration?')) return
    setActionError('')
    setActioning(id)
    try {
      await rejectUser(id)
      const [p, g, users] = await loadAdminData()
      applyAdminData(p, g, users)
    }
    finally { setActioning(null) }
  }

  const addGuest = async (e: React.FormEvent) => {
    e.preventDefault()
    setGuestError('')
    setGuestAdding(true)
    try {
      await createGuest({ displayName: guestName.trim() })
      setGuestName('')
      const [p, g, users] = await loadAdminData()
      applyAdminData(p, g, users)
    } catch {
      setGuestError('Could not create guest.')
    } finally {
      setGuestAdding(false)
    }
  }

  const removeGuest = async (id: number, name: string) => {
    if (!confirm(`Remove guest "${name}"? This will delete their match history.`)) return
    setActionError('')
    setActioning(id)
    try {
      await deleteUser(id)
      const [p, g, users] = await loadAdminData()
      applyAdminData(p, g, users)
    }
    finally { setActioning(null) }
  }

  const toggleAdmin = async (target: UserDto) => {
    if (user?.id === target.id) return

    setActionError('')
    const nextIsAdmin = !target.isAdmin

    // Optimistic update to keep UI responsive; rollback in catch if request fails.
    setApprovedUsers(current => current.map(u => u.id === target.id ? { ...u, isAdmin: nextIsAdmin } : u))
    setAdminTogglingId(target.id)

    try {
      const updated = await setUserAdminStatus(target.id, nextIsAdmin)
      setApprovedUsers(current => current.map(u => u.id === target.id ? updated : u))
    } catch {
      setApprovedUsers(current => current.map(u => u.id === target.id ? { ...u, isAdmin: target.isAdmin } : u))
      setActionError('Could not update admin role. Please try again.')
    } finally {
      setAdminTogglingId(null)
    }
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
        {pending.length > 0 && (
          <p className="text-gray-400 text-sm mb-3">
            Optionally select a guest before approving to carry over existing guest stats and match history.
          </p>
        )}
        {pending.length === 0 ? (
          <Card><p className="text-gray-500 text-sm">✅ No pending registrations.</p></Card>
        ) : (
          <div className="space-y-3">
            {actionError && <ErrorMsg msg={actionError} />}
            {pending.map(u => (
              <Card key={u.id} className="space-y-3">
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="text-white font-semibold truncate">{u.username}</p>
                    <p className="text-gray-500 text-xs mt-0.5 truncate">{u.email} · {new Date(u.createdAt).toLocaleDateString()}</p>
                  </div>
                </div>
                {guests.length > 0 && (
                  <select
                    value={pendingGuestLinks[u.id] ?? ''}
                    onChange={e => setPendingGuestLinks(current => ({ ...current, [u.id]: e.target.value }))}
                    disabled={actioning === u.id}
                    className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                  >
                    <option value="">No guest link</option>
                    {guests.map(g => (
                      <option key={g.id} value={String(g.id)}>{g.username}</option>
                    ))}
                  </select>
                )}
                <div className="flex gap-2">
                  <button onClick={() => approve(u.id)} disabled={actioning === u.id}
                    className="flex-1 bg-green-700 hover:bg-green-600 disabled:opacity-50 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors">
                    {pendingGuestLinks[u.id] ? '✓ Approve & Link' : '✓ Approve'}
                  </button>
                  <button onClick={() => reject(u.id)} disabled={actioning === u.id}
                    className="flex-1 bg-red-800 hover:bg-red-700 disabled:opacity-50 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors">
                    ✕ Reject
                  </button>
                </div>
              </Card>
            ))}
          </div>
        )}
      </div>

      {/* Users */}
      <div>
        <h2 className="text-lg font-semibold text-white mb-3">Users</h2>
        {approvedUsers.length === 0 ? (
          <Card><p className="text-gray-500 text-sm">No approved users yet.</p></Card>
        ) : (
          <div className="space-y-2">
            {approvedUsers.map(u => {
              const isSelf = user?.id === u.id
              const isToggling = adminTogglingId === u.id
              return (
                <Card key={u.id} className="flex items-center justify-between py-3 gap-3">
                  <div className="min-w-0">
                    <p className="text-white font-medium truncate">{u.username}</p>
                    <p className="text-gray-500 text-xs truncate">{u.email}</p>
                  </div>
                  <div className="flex items-center gap-2 shrink-0">
                    {u.isAdmin ? <Badge color="yellow">Admin</Badge> : <Badge color="gray">User</Badge>}
                    <button
                      type="button"
                      title={isSelf ? "You can't remove your own admin role" : (u.isAdmin ? 'Remove admin role' : 'Make admin')}
                      onClick={() => void toggleAdmin(u)}
                      disabled={isSelf || isToggling}
                      className={`text-xs px-3 py-1.5 rounded-lg font-medium transition-colors ${
                        isSelf
                          ? 'bg-gray-800 text-gray-500 cursor-not-allowed'
                          : u.isAdmin
                            ? 'bg-red-900/40 text-red-300 hover:bg-red-900/60'
                            : 'bg-green-900/40 text-green-300 hover:bg-green-900/60'
                      } ${isToggling ? 'opacity-60' : ''}`}
                    >
                      {isToggling ? 'Saving…' : (u.isAdmin ? 'Remove Admin' : 'Make Admin')}
                    </button>
                  </div>
                </Card>
              )
            })}
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
