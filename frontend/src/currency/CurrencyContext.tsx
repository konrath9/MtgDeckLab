import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { getExchangeRate } from '../api/exchangeRate'

interface CurrencyContextValue {
  /** Formata um preço em dólar — converte e mostra em R$ quando o idioma é pt-BR e a cotação está disponível. */
  formatUsd: (amountUsd: number) => string
}

const CurrencyContext = createContext<CurrencyContextValue | undefined>(undefined)

/**
 * Busca a cotação USD→BRL cacheada no servidor uma vez, na montagem, e a mantém em memória pelo
 * resto da sessão — ela só muda uma vez por dia no servidor, não vale a pena buscar de novo a
 * cada render. Se a busca falhar (ou ainda não tiver rodado o primeiro sync), os preços continuam
 * em dólar: conversão é um extra de exibição, nunca um bloqueio pro resto da página.
 */
export function CurrencyProvider({ children }: { children: ReactNode }) {
  const { i18n } = useTranslation()
  const [usdToBrl, setUsdToBrl] = useState<number | null>(null)

  useEffect(() => {
    let cancelled = false
    getExchangeRate()
      .then((rate) => {
        if (!cancelled) setUsdToBrl(rate.usdToBrl)
      })
      .catch(() => {
        // Sem cotação, os preços caem para dólar — ver formatUsd abaixo. Não há erro pra mostrar
        // ao usuário por causa disso.
      })
    return () => {
      cancelled = true
    }
  }, [])

  const value = useMemo<CurrencyContextValue>(() => {
    const showBrl = i18n.language === 'pt-BR' && usdToBrl !== null

    const formatter = showBrl
      ? new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
      : new Intl.NumberFormat(i18n.language, { style: 'currency', currency: 'USD' })

    return {
      formatUsd: (amountUsd) => formatter.format(showBrl ? amountUsd * usdToBrl! : amountUsd),
    }
  }, [i18n.language, usdToBrl])

  return <CurrencyContext.Provider value={value}>{children}</CurrencyContext.Provider>
}

export function useCurrency(): CurrencyContextValue {
  const context = useContext(CurrencyContext)
  if (!context) throw new Error('useCurrency must be used within a CurrencyProvider')
  return context
}
