import { apiClient } from './client'

export interface ExchangeRateResponse {
  usdToBrl: number | null
  asOf: string | null
}

export async function getExchangeRate(): Promise<ExchangeRateResponse> {
  const { data } = await apiClient.get<ExchangeRateResponse>('/exchange-rate')
  return data
}
