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

export type DeckSection = 'Main' | 'Sideboard' | 'Commander' | 'Maybeboard'

export type CardType =
  | 'Creature'
  | 'Instant'
  | 'Sorcery'
  | 'Artifact'
  | 'Enchantment'
  | 'Land'
  | 'Planeswalker'
  | 'Tribal'
  | 'Battle'

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
  maybeboardCount: number
  createdAt: string
  updatedAt: string
}

export interface DeckEntryDetail {
  cardId: string
  // Nome canônico em inglês (chave de negócio). Para exibir: localizedName ?? cardName.
  cardName: string
  localizedName: string | null
  quantity: number
  section: DeckSection
  types: CardType[]
  cmc: number
  priceUsd: number | null
  manaCost: string | null
}

export interface DeckDetail {
  id: string
  name: string
  format: Format
  description: string | null
  mainDeckCount: number
  sideboardCount: number
  maybeboardCount: number
  createdAt: string
  updatedAt: string
  entries: DeckEntryDetail[]
}

export interface UnresolvedCardName {
  cardName: string
  section: DeckSection
}

export interface ImportDeckResponse {
  deckId: string
  resolvedCards: number
  unresolvedCardNames: UnresolvedCardName[]
}

export interface UpsertDeckEntryResult {
  mainDeckCount: number
  sideboardCount: number
  maybeboardCount: number
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
  multicolorCount: number
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
  // "Plains" | "Island" | "Swamp" | "Mountain" | "Forest" | "Colorless" | "Nonbasic" → copies
  landBreakdown: Record<string, number>
}

// O Engine emite código + argumentos; a API devolve o texto já traduzido no idioma do request
// (header Accept-Language) e mantém código e argumentos para quem quiser traduzir por conta própria.
export interface LocalizedMessage {
  code: string
  text: string
  args: Record<string, unknown>
}

export interface DeckScore {
  score: number
  grade: string
  warnings: LocalizedMessage[]
  componentScores: Record<string, number>
}

export interface AnalysisValidationResult {
  isValid: boolean
  errors: LocalizedMessage[]
  warnings: LocalizedMessage[]
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
