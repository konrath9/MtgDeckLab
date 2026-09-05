import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function NavBar() {
  const { isAuthenticated, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  return (
    <header className="border-b border-border bg-surface">
      <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-x-4 gap-y-2 px-4 py-3">
        <Link to="/" className="text-lg font-semibold tracking-tight text-fg">
          MTG Deck Lab
        </Link>
        {isAuthenticated && (
          <nav className="flex items-center gap-3 text-sm sm:gap-4">
            <NavLink to="/" active={location.pathname === '/'}>
              My Decks
            </NavLink>
            <NavLink to="/decks/import" active={location.pathname === '/decks/import'}>
              Import Deck
            </NavLink>
            <button
              onClick={() => {
                logout()
                navigate('/login')
              }}
              className="rounded-md border border-border px-3 py-1.5 text-fg transition-colors hover:bg-surface-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
            >
              Log out
            </button>
          </nav>
        )}
      </div>
    </header>
  )
}

function NavLink({ to, active, children }: { to: string; active: boolean; children: string }) {
  return (
    <Link
      to={to}
      className={`rounded-md px-1 py-1.5 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg ${
        active ? 'text-accent-strong' : 'text-muted hover:text-fg'
      }`}
    >
      {children}
    </Link>
  )
}
