import { Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { RequireAuth } from './auth/RequireAuth'
import { CurrencyProvider } from './currency/CurrencyContext'
import { NavBar } from './components/NavBar'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { DeckListPage } from './pages/DeckListPage'
import { ImportDeckPage } from './pages/ImportDeckPage'
import { DeckDetailPage } from './pages/DeckDetailPage'

export default function App() {
  return (
    <AuthProvider>
      <CurrencyProvider>
        <NavBar />
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route
            path="/"
            element={
              <RequireAuth>
                <DeckListPage />
              </RequireAuth>
            }
          />
          <Route
            path="/decks/import"
            element={
              <RequireAuth>
                <ImportDeckPage />
              </RequireAuth>
            }
          />
          <Route
            path="/decks/:id"
            element={
              <RequireAuth>
                <DeckDetailPage />
              </RequireAuth>
            }
          />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </CurrencyProvider>
    </AuthProvider>
  )
}
