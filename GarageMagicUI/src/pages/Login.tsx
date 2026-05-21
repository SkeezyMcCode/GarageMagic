import { useState } from 'react'
import { loginUser, registerUser } from '../api'
import { useAuth } from '../context/AuthContext'
import { ErrorMsg } from '../components/Ui'

export default function Login() {
  const { login } = useAuth()
  const [tab, setTab] = useState<'login' | 'register'>('login')

  const [loginForm, setLoginForm] = useState({ username: '', password: '' })
  const [loginError, setLoginError] = useState('')
  const [loginLoading, setLoginLoading] = useState(false)

  const [regForm, setRegForm] = useState({ username: '', email: '', password: '' })
  const [regError, setRegError] = useState('')
  const [regSuccess, setRegSuccess] = useState(false)
  const [regLoading, setRegLoading] = useState(false)

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoginError('')
    setLoginLoading(true)
    try {
      const res = await loginUser(loginForm)
      login(res.token, res.user)
      // LoginGuard will redirect to / once user state updates
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number; data?: { error?: string } } })?.response?.status
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      if (status === 403) setLoginError('Your account is pending admin approval.')
      else setLoginError(msg ?? 'Invalid username or password.')
    } finally {
      setLoginLoading(false)
    }
  }

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault()
    setRegError('')
    setRegLoading(true)
    try {
      await registerUser(regForm)
      setRegSuccess(true)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } | string[] } })?.response?.data
      if (Array.isArray(msg)) setRegError(msg.join(', '))
      else setRegError((msg as { error?: string })?.error ?? 'Registration failed.')
    } finally {
      setRegLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center px-4">
      <div className="w-full max-w-sm">
        <div className="text-center mb-8">
          <h1 className="text-4xl font-bold text-purple-400">🃏 GarageMagic</h1>
          <p className="text-gray-500 mt-2 text-sm">Commander league tracker</p>
        </div>

        {/* Tabs */}
        <div className="flex bg-gray-900 border border-gray-800 rounded-xl mb-1 p-1">
          {(['login', 'register'] as const).map(t => (
            <button key={t} onClick={() => setTab(t)}
              className={`flex-1 py-2 rounded-lg text-sm font-medium transition-colors capitalize ${tab === t ? 'bg-purple-600 text-white' : 'text-gray-400 hover:text-white'}`}>
              {t === 'login' ? 'Sign In' : 'Register'}
            </button>
          ))}
        </div>

        <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
          {tab === 'login' ? (
            <form onSubmit={handleLogin} className="space-y-4">
              <div>
                <label className="text-gray-400 text-xs block mb-1">Username</label>
                <input
                  className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                  value={loginForm.username}
                  onChange={e => setLoginForm(f => ({ ...f, username: e.target.value }))}
                  autoComplete="username"
                  required
                />
              </div>
              <div>
                <label className="text-gray-400 text-xs block mb-1">Password</label>
                <input
                  type="password"
                  className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                  value={loginForm.password}
                  onChange={e => setLoginForm(f => ({ ...f, password: e.target.value }))}
                  autoComplete="current-password"
                  required
                />
              </div>
              {loginError && <ErrorMsg msg={loginError} />}
              <button type="submit" disabled={loginLoading}
                className="w-full bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white font-semibold py-2.5 rounded-lg transition-colors">
                {loginLoading ? 'Signing in…' : 'Sign In'}
              </button>
            </form>
          ) : regSuccess ? (
            <div className="text-center space-y-3">
              <p className="text-4xl">⏳</p>
              <p className="text-white font-semibold">Registration submitted!</p>
              <p className="text-gray-400 text-sm">An admin needs to approve your account before you can sign in.</p>
              <button onClick={() => { setTab('login'); setRegSuccess(false) }}
                className="text-purple-400 hover:text-purple-300 text-sm">
                Back to sign in →
              </button>
            </div>
          ) : (
            <form onSubmit={handleRegister} className="space-y-4">
              <div>
                <label className="text-gray-400 text-xs block mb-1">Username</label>
                <input
                  className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                  value={regForm.username}
                  onChange={e => setRegForm(f => ({ ...f, username: e.target.value }))}
                  required
                />
              </div>
              <div>
                <label className="text-gray-400 text-xs block mb-1">Email</label>
                <input
                  type="email"
                  className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                  value={regForm.email}
                  onChange={e => setRegForm(f => ({ ...f, email: e.target.value }))}
                  required
                />
              </div>
              <div>
                <label className="text-gray-400 text-xs block mb-1">Password</label>
                <input
                  type="password"
                  className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
                  value={regForm.password}
                  onChange={e => setRegForm(f => ({ ...f, password: e.target.value }))}
                  required
                />
              </div>
              {regError && <ErrorMsg msg={regError} />}
              <button type="submit" disabled={regLoading}
                className="w-full bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white font-semibold py-2.5 rounded-lg transition-colors">
                {regLoading ? 'Registering…' : 'Register'}
              </button>
              <p className="text-gray-600 text-xs text-center">Your account will need admin approval before you can sign in.</p>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}

