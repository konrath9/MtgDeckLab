import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'

export type ThemeMode = 'dark' | 'light'

export const THEME_STORAGE_KEY = 'mtgdecklab.theme'

// O produto nasceu escuro e o tema claro é o "modo alternativo": sem preferência salva,
// seguimos com o escuro (mesma essência de antes da reformulação).
export const DEFAULT_THEME: ThemeMode = 'dark'

interface ThemeContextValue {
  theme: ThemeMode
  toggleTheme: () => void
}

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined)

function getStoredTheme(): ThemeMode {
  try {
    const stored = localStorage.getItem(THEME_STORAGE_KEY)
    if (stored === 'light' || stored === 'dark') return stored
  } catch {
    // localStorage indisponível (modo privado) — cai no padrão.
  }
  return DEFAULT_THEME
}

/**
 * Tema claro/escuro por variáveis CSS: o ThemeProvider liga `data-theme` no <html>, que é o
 * gatilho do override de tokens em index.css, e persiste a escolha no localStorage. O script
 * inline no index.html já aplica o tema salvo antes do primeiro paint para não piscar o errado.
 */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<ThemeMode>(getStoredTheme)

  useEffect(() => {
    document.documentElement.dataset.theme = theme
    try {
      localStorage.setItem(THEME_STORAGE_KEY, theme)
    } catch {
      // Sem localStorage a preferência simplesmente não persiste entre sessões.
    }
  }, [theme])

  const toggleTheme = useCallback(() => {
    setTheme((current) => (current === 'dark' ? 'light' : 'dark'))
  }, [])

  const value = useMemo<ThemeContextValue>(() => ({ theme, toggleTheme }), [theme, toggleTheme])

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}

export function useTheme(): ThemeContextValue {
  const context = useContext(ThemeContext)
  if (!context) throw new Error('useTheme must be used within a ThemeProvider')
  return context
}
