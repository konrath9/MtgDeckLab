import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../auth/AuthContext'
import { LanguageSwitcher } from './LanguageSwitcher'

export function NavBar() {
  const { isAuthenticated, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const { t } = useTranslation()

  return (
    <header className="border-b border-border bg-surface">
      <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-x-4 gap-y-2 px-4 py-3">
        <Link to="/" className="text-lg font-semibold tracking-tight text-fg">
          {t('nav.brand')}
        </Link>
        <nav className="flex items-center gap-3 text-sm sm:gap-4">
          {isAuthenticated && (
            <>
              <NavLink to="/" active={location.pathname === '/'}>
                {t('nav.myDecks')}
              </NavLink>
              <NavLink to="/decks/import" active={location.pathname === '/decks/import'}>
                {t('nav.importDeck')}
              </NavLink>
            </>
          )}
          {/* Disponível também antes do login: quem cai na tela de entrar precisa poder trocar
              o idioma sem ter conta. */}
          <LanguageSwitcher />
          {isAuthenticated && (
            <button
              onClick={() => {
                logout()
                navigate('/login')
              }}
              className="rounded-md border border-border px-3 py-1.5 text-fg transition-colors hover:bg-surface-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
            >
              {t('nav.logOut')}
            </button>
          )}
        </nav>
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
