export type Format =
  | 'Commander'
  | 'Standard'
  | 'Modern'
  | 'Pioneer'
  | 'Legacy'
  | 'Vintage'
  | 'Pauper'
  | 'Historic'

export type Color = 'White' | 'Blue' | 'Black' | 'Red' | 'Green' | 'Colorless'

export interface AuthResponse {
  userId: string
  token: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface DeckSummary {
  id: string
  name: string
  format: Format
  description: string | null
  mainDeckCount: number
  sideboardCount: number
  createdAt: string
  updatedAt: string
}

export interface DeckEntryDetail {
  cardId: string
  cardName: string
  quantity: number
  isCommander: boolean
  isSideboard: boolean
}

export interface DeckDetail {
  id: string
  name: string
  format: Format
  description: string | null
  mainDeckCount: number
  sideboardCount: number
  createdAt: string
  updatedAt: string
  entries: DeckEntryDetail[]
}

export interface ImportDeckResponse {
  deckId: string
  resolvedCards: number
  unresolvedCardNames: string[]
}

export interface ManaCurve {
  distribution: Record<string, number>
  averageCmc: number
  peakCmc: number
  totalNonLandCards: number
}

export interface ColorDistribution {
  cardCount: Partial<Record<Color, number>>
  percentage: Partial<Record<Color, number>>
  isColorless: boolean
}

export interface TypeDistribution {
  creatures: number
  instants: number
  sorceries: number
  artifacts: number
  enchantments: number
  lands: number
  planeswalkers: number
  other: number
  total: number
}

export interface DeckScore {
  score: number
  grade: string
  warnings: string[]
  componentScores: Record<string, number>
}

export interface AnalysisValidationResult {
  isValid: boolean
  errors: string[]
  warnings: string[]
}

// Campos além do escopo "núcleo" (roleDistribution, roleCoverage, manaBase, synergy) existem na
// resposta real da API mas não são consumidos ainda nesta primeira fase do frontend.
export interface DeckAnalysisResult {
  manaCurve: ManaCurve
  colorDistribution: ColorDistribution
  typeDistribution: TypeDistribution
  validation: AnalysisValidationResult
  score: DeckScore
}

export interface ApiError {
  error?: string
}
