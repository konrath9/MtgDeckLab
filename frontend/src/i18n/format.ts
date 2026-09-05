import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'

/**
 * Formatadores de número presos ao idioma atual — "1,234.56" em en-US, "1.234,56" em pt-BR.
 *
 * Preços da Scryfall são sempre em dólar; o que muda com o idioma é a notação, não a moeda.
 */
export function useFormatters() {
  const { i18n } = useTranslation()

  return useMemo(
    () => ({
      usd: new Intl.NumberFormat(i18n.language, { style: 'currency', currency: 'USD' }),
      twoDecimals: new Intl.NumberFormat(i18n.language, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }),
    }),
    [i18n.language],
  )
}
