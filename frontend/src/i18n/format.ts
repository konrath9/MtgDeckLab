import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'

/**
 * Formatadores de número presos ao idioma atual — "1,234.56" em en-US, "1.234,56" em pt-BR.
 *
 * Preço de carta tem seu próprio formatador — ver `useCurrency` em `currency/CurrencyContext` —
 * porque, além da notação, ele também decide a moeda (USD ou, convertido, BRL).
 */
export function useFormatters() {
  const { i18n } = useTranslation()

  return useMemo(
    () => ({
      twoDecimals: new Intl.NumberFormat(i18n.language, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }),
    }),
    [i18n.language],
  )
}
