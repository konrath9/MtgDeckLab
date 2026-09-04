import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function NavBar() {
  const { isAuthenticated, logout } = useAuth()
  const navigate = useNavigate()

  return (
    <header className="border-b border-slate-800 bg-slate-900/60">
      <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
        <Link to="/" className="text-lg font-semibold tracking-tight text-slate-100">
          MTG Deck Lab
        </Link>
        {isAuthenticated && (
          <nav className="flex items-center gap-4 text-sm">
            <Link to="/" className="text-slate-300 hover:text-white">
              My Decks
            </Link>
            <Link to="/decks/import" className="text-slate-300 hover:text-white">
              Import Deck
            </Link>
            <button
              onClick={() => {
                logout()
                navigate('/login')
              }}
              className="rounded-md bg-slate-800 px-3 py-1.5 text-slate-200 hover:bg-slate-700"
            >
              Log out
            </button>
          </nav>
        )}
      </div>
    </header>
  )
}
