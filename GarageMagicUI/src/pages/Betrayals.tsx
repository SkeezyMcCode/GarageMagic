import { useEffect, useState } from 'react'
import { getRecentBetrayals, getLeaderboard, createBetrayal } from '../api'
import type { BetrayalDto, UserStandingDto } from '../types'
import { Card, Spinner, ErrorMsg, SectionHeader } from '../components/Ui'

export default function Betrayals() {
  const [betrayals, setBetrayals] = useState<BetrayalDto[]>([])
  const [players, setPlayers] = useState<UserStandingDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({ betrayerUserId: 0, victimUserId: 0, description: '' })
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState('')

  const loadBetrayals = async () => Promise.all([getRecentBetrayals(50), getLeaderboard()])

  const applyBetrayals = (b: BetrayalDto[], p: UserStandingDto[]) => {
    setBetrayals(b)
    setPlayers(p)
  }

  useEffect(() => {
    let active = true

    void (async () => {
      try {
        const [b, p] = await loadBetrayals()
        if (!active) return
        applyBetrayals(b, p)
      } catch {
        if (active) setError('Could not load betrayals.')
      } finally {
        if (active) setLoading(false)
      }
    })()

    return () => { active = false }
  }, [])

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitError('')
    if (form.betrayerUserId === form.victimUserId) { setSubmitError('Betrayer and victim must be different players'); return }
    setSubmitting(true)
    try {
      await createBetrayal({ ...form, betrayalDate: new Date().toISOString() })
      setShowForm(false)
      setForm({ betrayerUserId: 0, victimUserId: 0, description: '' })
      const [b, p] = await loadBetrayals()
      applyBetrayals(b, p)
    } catch (err: unknown) {
      setSubmitError((err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Failed to record betrayal')
    } finally { setSubmitting(false) }
  }

  if (loading) return <Spinner />
  if (error) return <ErrorMsg msg={error} />

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-6">
        <SectionHeader title="🗡️ Hall of Betrayal" subtitle="Where trust goes to die" />
        <button onClick={() => setShowForm(v => !v)} className="bg-red-700 hover:bg-red-600 text-white font-semibold px-4 py-2.5 rounded-lg text-sm transition-colors">
          {showForm ? 'Cancel' : '+ Record Betrayal'}
        </button>
      </div>

      {showForm && (
        <Card className="mb-6 border-red-900">
          <h3 className="font-semibold text-white mb-4">Record a Betrayal</h3>
          <form onSubmit={submit} className="space-y-3">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label className="text-gray-500 text-xs block mb-1">The Backstabber</label>
                <select className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-red-500" value={form.betrayerUserId || ''} onChange={e => setForm(f => ({ ...f, betrayerUserId: Number(e.target.value) }))} required>
                  <option value="">Select player…</option>
                  {players.map(p => <option key={p.userId} value={p.userId}>{p.username}</option>)}
                </select>
              </div>
              <div>
                <label className="text-gray-500 text-xs block mb-1">The Victim</label>
                <select className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-red-500" value={form.victimUserId || ''} onChange={e => setForm(f => ({ ...f, victimUserId: Number(e.target.value) }))} required>
                  <option value="">Select player…</option>
                  {players.map(p => <option key={p.userId} value={p.userId}>{p.username}</option>)}
                </select>
              </div>
            </div>
            <div>
              <label className="text-gray-500 text-xs block mb-1">What happened?</label>
              <textarea className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-red-500 h-20 resize-none" placeholder="Describe the dastardly deed…" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} required />
            </div>
            {submitError && <ErrorMsg msg={submitError} />}
            <button type="submit" disabled={submitting} className="bg-red-700 hover:bg-red-600 disabled:opacity-50 text-white font-semibold px-6 py-2 rounded-lg text-sm transition-colors">
              {submitting ? 'Recording…' : 'Record Betrayal'}
            </button>
          </form>
        </Card>
      )}

      {betrayals.length === 0 ? (
        <Card><p className="text-gray-500 text-sm">No betrayals recorded. How civil.</p></Card>
      ) : (
        <div className="space-y-3">
          {betrayals.map(b => (
            <Card key={b.id} className="border-l-4 border-l-red-800">
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-white">
                    <span className="text-red-400 font-semibold">{b.betrayerUsername}</span>
                    <span className="text-gray-500 mx-2">stabbed</span>
                    <span className="text-purple-400 font-semibold">{b.victimUsername}</span>
                    <span className="text-gray-500 mx-2">in the back</span>
                  </p>
                  <p className="text-gray-400 mt-1 text-sm">{b.description}</p>
                </div>
                <span className="text-gray-600 text-xs shrink-0 ml-4">{new Date(b.betrayalDate).toLocaleDateString()}</span>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}

