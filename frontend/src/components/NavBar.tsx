import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../auth/AuthContext'
import { useTheme } from '../theme/ThemeContext'
import { LanguageSwitcher } from './LanguageSwitcher'

export function NavBar() {
  const { isAuthenticated, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const { t } = useTranslation()

  return (
    <header className="border-b border-header-line bg-surface transition-colors">
      <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-x-4 gap-y-2 px-4 py-3">
        <Link
          to="/"
          className="flex items-center gap-2.5 text-lg font-semibold tracking-tight text-fg transition-opacity hover:opacity-90"
        >
          <BrandMark className="brand-mark h-7 w-7 shrink-0" />
          <span>{t('nav.brand')}</span>
        </Link>
        <nav className="flex items-center gap-2 text-sm sm:gap-3">
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
          <ThemeToggle />
          {/* Disponível também antes do login: quem cai na tela de entrar precisa poder trocar
              o idioma sem ter conta. */}
          <LanguageSwitcher />
          {isAuthenticated && (
            <button
              onClick={() => {
                logout()
                navigate('/login')
              }}
              className="inline-flex items-center gap-2 rounded-md border border-border px-3 py-1.5 text-fg transition duration-150 hover:-translate-y-px hover:border-accent hover:bg-accent/10 hover:text-accent-strong hover:shadow-sm active:translate-y-0 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
            >
              <i className="fa-solid fa-right-from-bracket text-xs" aria-hidden="true" />
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

/** Alterna entre tema escuro e claro. O ícone mostra o tema que a ação vai ATIVAR. */
function ThemeToggle() {
  const { theme, toggleTheme } = useTheme()
  const { t } = useTranslation()
  const isDark = theme === 'dark'
  const label = isDark ? t('nav.themeLight') : t('nav.themeDark')

  return (
    <button
      type="button"
      onClick={toggleTheme}
      aria-label={label}
      title={label}
      className="inline-flex items-center justify-center rounded-md border border-border p-1.5 text-muted transition-colors hover:border-accent hover:bg-accent/10 hover:text-accent-strong hover:shadow-sm dark:text-accent-soft focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
    >
      <i className={`fa-solid text-sm ${isDark ? 'fa-sun' : 'fa-moon'}`} aria-hidden="true" />
    </button>
  )
}

/**
 * Logo minimalista: um cristal facetado em gradiente azul→roxo neon com brilho sutil. O glow
 * (classe .brand-mark) fica contido na própria marca — o resto da interface segue sóbria.
 */
function BrandMark({ className = '' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" aria-hidden="true" className={className}>
      <defs>
        <linearGradient id="brand-gradient" x1="2.8" y1="1.6" x2="21.2" y2="21" gradientUnits="userSpaceOnUse">
          <stop stopColor="#4f9dff" />
          <stop offset="0.55" stopColor="#8f5bff" />
          <stop offset="1" stopColor="#d247ff" />
        </linearGradient>
      </defs>
      <path d="M12 1.6 21.2 7.6 18.6 21 5.4 21 2.8 7.6 Z" fill="url(#brand-gradient)" />
      {/* Facetas internas do cristal, apenas para dar leveza ao corte geométrico. */}
      <path d="M12 1.6 V21 M12 1.6 18.6 21 M12 1.6 5.4 21 M2.8 7.6 H21.2" stroke="rgba(255,255,255,0.45)" strokeWidth="0.8" />
    </svg>
  )
}

