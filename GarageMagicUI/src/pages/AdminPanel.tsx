import { useEffect, useState } from 'react'
import { getPendingUsers, approveUser, rejectUser } from '../api'
import type { PendingUserDto } from '../types'
import { Card, Spinner, ErrorMsg, SectionHeader } from '../components/Ui'

export default function AdminPanel() {
  const [pending, setPending] = useState<PendingUserDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [actioning, setActioning] = useState<number | null>(null)

  const load = async () => {
    try {
      setPending(await getPendingUsers())
    } catch {
      setError('Could not load pending users.')
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

  if (loading) return <Spinner />
  if (error) return <ErrorMsg msg={error} />

  return (
    <div>
      <SectionHeader title="⚙️ Admin Panel" subtitle="Approve or reject pending registrations" />

      {pending.length === 0 ? (
        <Card>
          <p className="text-gray-500 text-sm">✅ No pending registrations.</p>
        </Card>
      ) : (
        <div className="space-y-3">
          {pending.map(u => (
            <Card key={u.id} className="flex items-center justify-between">
              <div>
                <p className="text-white font-semibold">{u.username}</p>
                <p className="text-gray-500 text-xs">{u.email} · registered {new Date(u.createdAt).toLocaleDateString()}</p>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => approve(u.id)}
                  disabled={actioning === u.id}
                  className="bg-green-700 hover:bg-green-600 disabled:opacity-50 text-white text-sm font-medium px-4 py-1.5 rounded-lg transition-colors"
                >
                  ✓ Approve
                </button>
                <button
                  onClick={() => reject(u.id)}
                  disabled={actioning === u.id}
                  className="bg-red-800 hover:bg-red-700 disabled:opacity-50 text-white text-sm font-medium px-4 py-1.5 rounded-lg transition-colors"
                >
                  ✕ Reject
                </button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}

