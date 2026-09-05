import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { getExchangeRate } from '../api/exchangeRate'

// A cotação comercial do dólar sozinha deixa o preço convertido irreal pro mercado brasileiro de
// Magic (LigaMagic e cia. costumam ser mais baratos, sobretudo em cartas de menor valor, onde
// frete/custo fixo de importação pesa proporcionalmente mais). Estas faixas são uma aproximação
// heurística desse efeito — não uma fonte de preço real — e claramente vão precisar de reajuste
// conforme o mercado for observado; por isso ficam como constantes nomeadas aqui, não espalhadas
// dentro da função de conversão.
//
// A faixa mais barata nem usa a cotação do dia: LigaMagic tende a ter um "piso" de preço quase
// fixo pra cartas de bulk, então aplicamos um fator direto sobre o valor em dólar em vez de
// cotação × desconto.
const CHEAP_CARD_THRESHOLD_USD = 1.0
const CHEAP_CARD_FLAT_FACTOR = 1.8

const MID_LOW_CARD_THRESHOLD_USD = 5.0
const MID_LOW_CARD_DISCOUNT = 0.6 // cotação com 40% de desconto

const MID_HIGH_CARD_THRESHOLD_USD = 20.0
const MID_HIGH_CARD_DISCOUNT = 0.75 // cotação com 25% de desconto

const EXPENSIVE_CARD_DISCOUNT = 0.88 // cotação com 12% de desconto

/**
 * Converte um preço em dólar da Scryfall pra uma estimativa em reais ajustada ao mercado
 * brasileiro, aplicando um desconto regressivo sobre a cotação do dia: quanto mais barata a
 * carta, maior o desconto (e a carta mais barata ignora a cotação por completo).
 */
function applyRegressiveMarketAdjustment(amountUsd: number, usdToBrl: number): number {
  if (amountUsd <= CHEAP_CARD_THRESHOLD_USD) return amountUsd * CHEAP_CARD_FLAT_FACTOR
  if (amountUsd <= MID_LOW_CARD_THRESHOLD_USD) return amountUsd * usdToBrl * MID_LOW_CARD_DISCOUNT
  if (amountUsd <= MID_HIGH_CARD_THRESHOLD_USD) return amountUsd * usdToBrl * MID_HIGH_CARD_DISCOUNT
  return amountUsd * usdToBrl * EXPENSIVE_CARD_DISCOUNT
}

interface CurrencyContextValue {
  /**
   * Converte um preço em dólar pro valor a exibir (BRL ajustado se pt-BR e a cotação já carregou;
   * o próprio dólar, sem alteração, caso contrário) — sem formatar. Some vários preços já
   * convertidos (ex.: o total de um deck) antes de formatar o resultado com formatAmount; somar
   * dólares brutos e só então converter aplicaria a faixa regressiva ao total agregado em vez de a
   * cada carta, o que é outra conta.
   */
  convertUsd: (amountUsd: number) => number
  /** Formata um valor já convertido (de convertUsd) na moeda e notação corretas. */
  formatAmount: (amount: number) => string
  /** Atalho pro caso comum de converter e formatar um único preço em dólar de uma vez. */
  formatUsd: (amountUsd: number) => string
  /** true quando os valores exibidos são a estimativa ajustada em BRL, não o dólar original. */
  isEstimated: boolean
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

    const convertUsd = (amountUsd: number) =>
      showBrl ? applyRegressiveMarketAdjustment(amountUsd, usdToBrl!) : amountUsd

    return {
      isEstimated: showBrl,
      convertUsd,
      formatAmount: (amount) => formatter.format(amount),
      formatUsd: (amountUsd) => formatter.format(convertUsd(amountUsd)),
    }
  }, [i18n.language, usdToBrl])

  return <CurrencyContext.Provider value={value}>{children}</CurrencyContext.Provider>
}

export function useCurrency(): CurrencyContextValue {
  const context = useContext(CurrencyContext)
  if (!context) throw new Error('useCurrency must be used within a CurrencyProvider')
  return context
}
