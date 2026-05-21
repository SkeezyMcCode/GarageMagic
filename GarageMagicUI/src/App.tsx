import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import { useAuth } from './context/useAuth'
import Layout from './components/Layout'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'
import Players from './pages/Players'
import PlayerDetail from './pages/PlayerDetail'
import Matches from './pages/Matches'
import RecordMatch from './pages/RecordMatch'
import Betrayals from './pages/Betrayals'
import Seasons from './pages/Seasons'
import AdminPanel from './pages/AdminPanel'
import { Spinner } from './components/Ui'

function ProtectedRoutes() {
  const { user, isLoading } = useAuth()
  if (isLoading) return <div className="min-h-screen bg-gray-950 flex items-center justify-center"><Spinner /></div>
  if (!user) return <Navigate to="/login" replace />
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/players" element={<Players />} />
        <Route path="/players/:id" element={<PlayerDetail />} />
        <Route path="/matches" element={<Matches />} />
        <Route path="/matches/new" element={<RecordMatch />} />
        <Route path="/betrayals" element={<Betrayals />} />
        <Route path="/seasons" element={<Seasons />} />
        {user.isAdmin && <Route path="/admin" element={<AdminPanel />} />}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </Layout>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginGuard />} />
          <Route path="/*" element={<ProtectedRoutes />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}

function LoginGuard() {
  const { user, isLoading } = useAuth()
  if (isLoading) return null
  if (user) return <Navigate to="/" replace />
  return <Login />
}
