import { apiClient } from './client'
import type {
  DeckAnalysisResult,
  DeckDetail,
  DeckSummary,
  Format,
  ImportDeckResponse,
  PagedResult,
} from './types'

export async function listDecks(page = 1, pageSize = 20): Promise<PagedResult<DeckSummary>> {
  const { data } = await apiClient.get<PagedResult<DeckSummary>>('/decks', {
    params: { page, pageSize },
  })
  return data
}

export async function getDeck(id: string): Promise<DeckDetail> {
  const { data } = await apiClient.get<DeckDetail>(`/decks/${id}`)
  return data
}

export async function importDeck(
  name: string,
  format: Format,
  decklist: string,
  description?: string,
): Promise<ImportDeckResponse> {
  const { data } = await apiClient.post<ImportDeckResponse>('/decks/import', {
    name,
    format,
    decklist,
    description,
  })
  return data
}

export async function updateDeck(
  id: string,
  name: string,
  description?: string,
): Promise<DeckDetail> {
  const { data } = await apiClient.put<DeckDetail>(`/decks/${id}`, { name, description })
  return data
}

export async function deleteDeck(id: string): Promise<void> {
  await apiClient.delete(`/decks/${id}`)
}

export async function getDeckAnalysis(id: string): Promise<DeckAnalysisResult> {
  const { data } = await apiClient.get<DeckAnalysisResult>(`/decks/${id}/analysis`)
  return data
}

export const ALL_FORMATS: Format[] = [
  'Commander',
  'Standard',
  'Modern',
  'Pioneer',
  'Legacy',
  'Vintage',
  'Pauper',
  'Historic',
]
