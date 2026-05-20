import { BrowserRouter, Routes, Route } from 'react-router-dom'
import Layout from './components/Layout'
import Dashboard from './pages/Dashboard'
import Players from './pages/Players'
import PlayerDetail from './pages/PlayerDetail'
import Matches from './pages/Matches'
import RecordMatch from './pages/RecordMatch'
import Betrayals from './pages/Betrayals'
import Seasons from './pages/Seasons'

export default function App() {
  return (
    <BrowserRouter>
      <Layout>
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/players" element={<Players />} />
          <Route path="/players/:id" element={<PlayerDetail />} />
          <Route path="/matches" element={<Matches />} />
          <Route path="/matches/new" element={<RecordMatch />} />
          <Route path="/betrayals" element={<Betrayals />} />
          <Route path="/seasons" element={<Seasons />} />
        </Routes>
      </Layout>
    </BrowserRouter>
  )
}
