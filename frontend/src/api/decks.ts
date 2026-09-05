import { apiClient } from './client'
import type {
  DeckAnalysisResult,
  DeckDetail,
  DeckSection,
  DeckSummary,
  Format,
  ImportDeckResponse,
  PagedResult,
  UpsertDeckEntryResult,
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
  mainDecklist: string,
  options?: {
    commanderDecklist?: string
    sideboardDecklist?: string
    maybeboardDecklist?: string
    description?: string
  },
): Promise<ImportDeckResponse> {
  const { data } = await apiClient.post<ImportDeckResponse>('/decks/import', {
    name,
    format,
    mainDecklist,
    commanderDecklist: options?.commanderDecklist,
    sideboardDecklist: options?.sideboardDecklist,
    maybeboardDecklist: options?.maybeboardDecklist,
    description: options?.description,
  })
  return data
}

export async function upsertDeckEntry(
  deckId: string,
  cardName: string,
  quantity: number,
  section: DeckSection = 'Main',
): Promise<UpsertDeckEntryResult> {
  const { data } = await apiClient.put<UpsertDeckEntryResult>(`/decks/${deckId}/entries`, {
    cardName,
    quantity,
    section,
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
