import { useEffect, useRef, useState, type FormEvent, type PointerEvent as ReactPointerEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { deleteDeck, getDeck, getDeckAnalysis, updateDeck, upsertDeckEntry } from '../api/decks'
import type { CardType, DeckAnalysisResult, DeckDetail, DeckSection } from '../api/types'
import { extractErrorMessage } from '../api/client'
import { ScoreBadge } from '../components/ScoreBadge'
import { ManaCost } from '../components/ManaCost'
import { ManaCurveChart } from '../components/ManaCurveChart'
import { ColorDistributionChart } from '../components/ColorDistributionChart'
import { TypeDistributionChart } from '../components/TypeDistributionChart'
import { ValidationList } from '../components/ValidationList'

const INPUT_CLASS =
  'w-full rounded-md border border-border bg-surface px-3 py-1.5 text-fg transition-colors focus:border-accent focus:outline-none focus-visible:ring-2 focus-visible:ring-accent/50'
const GHOST_BUTTON_CLASS =
  'rounded-md border border-border px-3 py-1.5 text-sm text-fg transition-colors hover:bg-surface-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg'
const PRIMARY_BUTTON_CLASS =
  'rounded-md bg-accent px-3 py-1.5 text-sm font-medium text-white transition-colors hover:bg-accent-strong disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg'

// Consistent vertical rhythm: SECTION_GAP separates major narrative sections (identity, score,
// strengths/weaknesses, the entries+analysis area); everything inside a section uses smaller,
// tighter steps so the page reads as a few deliberate groups rather than a stack of equal parts.
const SECTION_GAP = 'mb-8 border-b border-border pb-8'

const SECTIONS: DeckSection[] = ['Main', 'Commander', 'Sideboard', 'Maybeboard']

// Friendly labels for DeckScore.componentScores keys (MtgDeckLab.Engine.Analysis.DeckScorer).
const COMPONENT_LABELS: Record<string, string> = {
  ManaCurve: 'Mana Curve',
  LandRatio: 'Land Ratio',
  ColorConsistency: 'Color Consistency',
  RuleCompliance: 'Format Legality',
}
const STRENGTH_THRESHOLD = 80

// Priority order for picking one display category out of a (possibly multi-type) card's
// types — e.g. an Artifact Creature groups under Creatures. Lands are checked last, so a
// permanent that's also a land still groups by its more distinctive type.
const TYPE_CATEGORY_ORDER: CardType[] = [
  'Creature',
  'Planeswalker',
  'Battle',
  'Instant',
  'Sorcery',
  'Artifact',
  'Enchantment',
  'Tribal',
  'Land',
]
const CATEGORY_LABELS: Record<string, string> = {
  Creature: 'Creatures',
  Planeswalker: 'Planeswalkers',
  Battle: 'Battles',
  Instant: 'Instants',
  Sorcery: 'Sorceries',
  Artifact: 'Artifacts',
  Enchantment: 'Enchantments',
  Tribal: 'Tribals',
  Land: 'Lands',
  Other: 'Other',
}

function categorize(types: CardType[]): string {
  for (const t of TYPE_CATEGORY_ORDER) {
    if (types.includes(t)) return t
  }
  return 'Other'
}

function groupByCategory(
  entries: DeckDetail['entries'],
): { category: string; entries: DeckDetail['entries'] }[] {
  const buckets = new Map<string, DeckDetail['entries']>()
  for (const entry of entries) {
    const category = categorize(entry.types)
    if (!buckets.has(category)) buckets.set(category, [])
    buckets.get(category)!.push(entry)
  }
  return [...TYPE_CATEGORY_ORDER, 'Other']
    .filter((category) => buckets.has(category))
    .map((category) => ({ category, entries: buckets.get(category)! }))
}

function buildVerdict(analysis: DeckAnalysisResult, format: string): string {
  const { score, validation } = analysis
  if (!validation.isValid) {
    const count = validation.errors.length
    return `This deck currently breaks ${format} rules — ${count} issue${count === 1 ? '' : 's'} to fix before it's legal.`
  }
  if (score.warnings.length > 0) {
    const count = score.warnings.length
    return `Legal for ${format}, with ${count} area${count === 1 ? '' : 's'} worth reviewing below.`
  }
  return `Legal for ${format} with no issues detected.`
}

// Short factual caption per score component, built only from fields already on
// DeckAnalysisResult — never a judgment, just the real number(s) behind that component's score.
function componentCaption(key: string, analysis: DeckAnalysisResult): string | null {
  switch (key) {
    case 'ManaCurve':
      return `Average CMC ${analysis.manaCurve.averageCmc.toFixed(2)}`
    case 'LandRatio': {
      const { lands, total } = analysis.typeDistribution
      return `${lands} land${lands === 1 ? '' : 's'} of ${total}`
    }
    case 'ColorConsistency': {
      const colorCount = Object.keys(analysis.colorDistribution.cardCount).length
      return colorCount === 0 ? 'Colorless' : `${colorCount} color${colorCount === 1 ? '' : 's'}`
    }
    case 'RuleCompliance': {
      const count = analysis.validation.errors.length
      return count === 0 ? 'No rule violations' : `${count} rule violation${count === 1 ? '' : 's'}`
    }
    default:
      return null
  }
}

export function DeckDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const [deck, setDeck] = useState<DeckDetail | null>(null)
  const [analysis, setAnalysis] = useState<DeckAnalysisResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [analysisError, setAnalysisError] = useState<string | null>(null)

  const [isEditing, setIsEditing] = useState(false)
  const [editName, setEditName] = useState('')
  const [editDescription, setEditDescription] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  const [entryError, setEntryError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState<'entries' | 'analysis'>('entries')

  async function refetchDeck() {
    if (!id) return
    const data = await getDeck(id)
    setDeck(data)
    getDeckAnalysis(id)
      .then((a) => setAnalysis(a))
      .catch((err) => setAnalysisError(extractErrorMessage(err, 'Could not load analysis.')))
  }

  useEffect(() => {
    if (!id) return
    let cancelled = false

    setDeck(null)
    setAnalysis(null)
    setError(null)
    setAnalysisError(null)

    getDeck(id)
      .then((data) => {
        if (cancelled) return
        setDeck(data)
        setEditName(data.name)
        setEditDescription(data.description ?? '')
      })
      .catch((err) => !cancelled && setError(extractErrorMessage(err, 'Deck not found.')))

    getDeckAnalysis(id)
      .then((data) => !cancelled && setAnalysis(data))
      .catch((err) => !cancelled && setAnalysisError(extractErrorMessage(err, 'Could not load analysis.')))

    return () => {
      cancelled = true
    }
  }, [id])

  async function handleSave() {
    if (!id) return
    setIsSaving(true)
    try {
      const updated = await updateDeck(id, editName, editDescription || undefined)
      setDeck(updated)
      setIsEditing(false)
    } catch (err) {
      setError(extractErrorMessage(err, 'Could not update deck.'))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDelete() {
    if (!id) return
    if (!confirm('Delete this deck? This cannot be undone.')) return
    try {
      await deleteDeck(id)
      navigate('/')
    } catch (err) {
      setError(extractErrorMessage(err, 'Could not delete deck.'))
    }
  }

  async function handleAddCard(cardName: string, quantity: number, section: DeckSection) {
    if (!id) return
    setEntryError(null)
    try {
      await upsertDeckEntry(id, cardName, quantity, section)
      await refetchDeck()
    } catch (err) {
      setEntryError(extractErrorMessage(err, `Could not find card "${cardName}".`))
    }
  }

  async function handleRemoveEntry(cardName: string, section: DeckSection) {
    if (!id) return
    setEntryError(null)
    try {
      await upsertDeckEntry(id, cardName, 0, section)
      await refetchDeck()
    } catch (err) {
      setEntryError(extractErrorMessage(err, 'Could not remove card.'))
    }
  }

  async function handleMoveEntry(
    cardName: string,
    moveQuantity: number,
    fromSection: DeckSection,
    toSection: DeckSection,
  ) {
    if (!id || !deck || fromSection === toSection) return
    setEntryError(null)
    try {
      // moveQuantity may be less than the full stack (partial move), and the destination
      // section may already have some copies of this card — merge rather than overwrite.
      const fromEntry = deck.entries.find((e) => e.cardName === cardName && e.section === fromSection)
      const toEntry = deck.entries.find((e) => e.cardName === cardName && e.section === toSection)
      const remaining = Math.max((fromEntry?.quantity ?? 0) - moveQuantity, 0)
      const combined = (toEntry?.quantity ?? 0) + moveQuantity

      await upsertDeckEntry(id, cardName, remaining, fromSection)
      await upsertDeckEntry(id, cardName, combined, toSection)
      await refetchDeck()
    } catch (err) {
      setEntryError(extractErrorMessage(err, 'Could not move card.'))
    }
  }

  if (error) return <p className="mx-auto max-w-5xl px-4 py-10 text-danger">{error}</p>
  if (!deck) return <p className="mx-auto max-w-5xl px-4 py-10 text-muted">Loading…</p>

  const mainEntries = deck.entries.filter((e) => e.section === 'Main')
  const commanderEntries = deck.entries.filter((e) => e.section === 'Commander')
  const sideboardEntries = deck.entries.filter((e) => e.section === 'Sideboard')
  const maybeboardEntries = deck.entries.filter((e) => e.section === 'Maybeboard')

  // Estimated value covers Main + Commander only — what you'd actually need to own; Sideboard/
  // Maybeboard aren't committed to the deck.
  const totalValueUsd = mainEntries
    .concat(commanderEntries)
    .reduce((sum, e) => sum + (e.priceUsd ?? 0) * e.quantity, 0)

  const componentEntries = analysis ? Object.entries(analysis.score.componentScores) : []
  const strengths = componentEntries.filter(([, v]) => v >= STRENGTH_THRESHOLD).sort((a, b) => b[1] - a[1])
  const weakComponents = componentEntries.filter(([, v]) => v < STRENGTH_THRESHOLD).sort((a, b) => a[1] - b[1])
  const hasWeaknesses = analysis
    ? analysis.validation.errors.length > 0 || analysis.score.warnings.length > 0 || weakComponents.length > 0
    : false

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      {/* Deck identity */}
      <div className={`flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between ${SECTION_GAP}`}>
        <div className="min-w-0 flex-1">
          {isEditing ? (
            <div className="space-y-2">
              <input
                aria-label="Deck name"
                value={editName}
                onChange={(e) => setEditName(e.target.value)}
                className={`${INPUT_CLASS} text-xl font-semibold`}
              />
              <input
                aria-label="Description"
                value={editDescription}
                onChange={(e) => setEditDescription(e.target.value)}
                placeholder="Description"
                className={`${INPUT_CLASS} text-sm`}
              />
              <div className="flex gap-2">
                <button onClick={handleSave} disabled={isSaving} className={PRIMARY_BUTTON_CLASS}>
                  {isSaving ? 'Saving…' : 'Save'}
                </button>
                <button onClick={() => setIsEditing(false)} className={GHOST_BUTTON_CLASS}>
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <>
              <div className="flex min-w-0 items-center gap-3">
                <h1 className="min-w-0 truncate text-2xl font-semibold tracking-tight text-fg" title={deck.name}>
                  {deck.name}
                </h1>
                <span className="shrink-0 rounded bg-surface-hover px-2 py-0.5 text-xs text-muted">
                  {deck.format}
                </span>
              </div>
              {deck.description && <p className="mt-1 text-sm text-muted">{deck.description}</p>}
            </>
          )}
        </div>
        {!isEditing && (
          <div className="flex gap-2">
            <button onClick={() => setIsEditing(true)} className={GHOST_BUTTON_CLASS}>
              Edit
            </button>
            <button
              onClick={handleDelete}
              className="rounded-md border border-border px-3 py-1.5 text-sm text-danger transition-colors hover:bg-danger/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
            >
              Delete
            </button>
          </div>
        )}
      </div>

      {/* Score + verdict — stays visible above the tabs: the one quick-scan summary the user
          should always have in view, kept deliberately minimal so it doesn't crowd out Entries. */}
      {analysisError && <p className="text-sm text-danger">{analysisError}</p>}
      {!analysis && !analysisError && <p className="text-muted">Loading analysis…</p>}
      {analysis && (
        <div className={SECTION_GAP}>
          <ScoreBadge score={analysis.score.score} grade={analysis.score.grade} />
          <p className="mt-5 max-w-2xl text-base text-fg">{buildVerdict(analysis, deck.format)}</p>
        </div>
      )}

      {/* In-page navigation — Entries first: it's the content people come here to act on. Analysis
          and, later, Versions/Recommendations/Simulation/Synergy sit alongside it as detail modes. */}
      <TabBar
        active={activeTab}
        onChange={setActiveTab}
        tabs={[
          { key: 'entries', label: 'Entries' },
          { key: 'analysis', label: 'Analysis' },
        ]}
      />

      {/* Analysis — strengths/weaknesses first (the "why"), then the charts that back them up. */}
      {activeTab === 'analysis' && analysis && (
        <section>
          <div className={`grid gap-8 sm:grid-cols-2 ${SECTION_GAP}`}>
            <div>
              <h2 className="text-sm font-semibold text-fg">Strengths</h2>
              {strengths.length === 0 ? (
                <p className="mt-3 text-sm text-muted">No standout strengths yet.</p>
              ) : (
                <ul className="mt-3 space-y-3">
                  {strengths.map(([key, value]) => {
                    const caption = componentCaption(key, analysis)
                    return (
                      <li key={key}>
                        <div className="flex items-baseline justify-between">
                          <span className="text-sm text-fg">{COMPONENT_LABELS[key] ?? key}</span>
                          <span className="text-xs text-success">{value}</span>
                        </div>
                        {caption && <p className="text-xs text-muted">{caption}</p>}
                      </li>
                    )
                  })}
                </ul>
              )}
            </div>
            <div>
              <h2 className="text-sm font-semibold text-fg">Needs Attention</h2>
              {!hasWeaknesses ? (
                <p className="mt-3 text-sm text-muted">No significant weaknesses detected.</p>
              ) : (
                <ul className="mt-3 space-y-3">
                  {analysis.validation.errors.map((e, i) => (
                    <li key={`err-${i}`} className="text-sm text-danger">
                      {e}
                    </li>
                  ))}
                  {analysis.score.warnings.map((w, i) => (
                    <li key={`warn-${i}`} className="text-sm text-warning">
                      {w}
                    </li>
                  ))}
                  {weakComponents.map(([key, value]) => {
                    const caption = componentCaption(key, analysis)
                    return (
                      <li key={key}>
                        <div className="flex items-baseline justify-between">
                          <span className="text-sm text-fg">{COMPONENT_LABELS[key] ?? key}</span>
                          <span className="text-xs text-warning">{value}</span>
                        </div>
                        {caption && <p className="text-xs text-muted">{caption}</p>}
                      </li>
                    )
                  })}
                </ul>
              )}
            </div>
          </div>

          <div className="grid gap-x-10 gap-y-6 md:grid-cols-2">
            <div>
              <h3 className="mb-3 text-sm font-medium text-muted">Mana Curve</h3>
              <ManaCurveChart manaCurve={analysis.manaCurve} />
            </div>

            <div>
              <h3 className="mb-3 text-sm font-medium text-muted">Color Distribution</h3>
              <ColorDistributionChart colorDistribution={analysis.colorDistribution} />
            </div>

            <div>
              <h3 className="mb-3 text-sm font-medium text-muted">Type Distribution</h3>
              <TypeDistributionChart typeDistribution={analysis.typeDistribution} />
            </div>

            <div>
              <h3 className="mb-3 text-sm font-medium text-muted">Format Validation</h3>
              <ValidationList validation={analysis.validation} />
            </div>
          </div>
        </section>
      )}

      {/* Entries — the editing area, now a multi-column card grid so a full decklist doesn't
          force endless single-column scrolling. */}
      {activeTab === 'entries' && (
        <section>
          <h2 className="text-sm font-semibold text-fg">
            Entries ({deck.mainDeckCount} main
            {deck.sideboardCount > 0 ? `, ${deck.sideboardCount} sideboard` : ''}
            {deck.maybeboardCount > 0 ? `, ${deck.maybeboardCount} maybeboard` : ''})
          </h2>
          <p className="mb-4 text-xs text-muted">
            Add, move, or remove cards. Estimated value: ${totalValueUsd.toFixed(2)}
          </p>

          <AddCardToDeckForm onAdd={handleAddCard} />
          {entryError && <p className="mb-3 text-sm text-danger">{entryError}</p>}

          {commanderEntries.length > 0 && (
            <EntryGroup
              title="Commander"
              section="Commander"
              entries={commanderEntries}
              onRemove={handleRemoveEntry}
              onMove={handleMoveEntry}
            />
          )}
          <EntryGroup
            title="Main Deck"
            section="Main"
            entries={mainEntries}
            groupByType
            onRemove={handleRemoveEntry}
            onMove={handleMoveEntry}
          />
          {sideboardEntries.length > 0 && (
            <EntryGroup
              title="Sideboard"
              section="Sideboard"
              entries={sideboardEntries}
              onRemove={handleRemoveEntry}
              onMove={handleMoveEntry}
            />
          )}
          {maybeboardEntries.length > 0 && (
            <EntryGroup
              title="Maybeboard"
              section="Maybeboard"
              caption="not counted in analysis"
              entries={maybeboardEntries}
              onRemove={handleRemoveEntry}
              onMove={handleMoveEntry}
            />
          )}
        </section>
      )}
    </div>
  )
}

function TabBar<T extends string>({
  active,
  onChange,
  tabs,
}: {
  active: T
  onChange: (tab: T) => void
  tabs: { key: T; label: string }[]
}) {
  return (
    <div className="mb-8 flex gap-6 border-b border-border">
      {tabs.map((tab) => (
        <button
          key={tab.key}
          onClick={() => onChange(tab.key)}
          className={`-mb-px border-b-2 px-1 pb-3 text-sm font-medium transition-colors focus-visible:outline-none ${
            active === tab.key ? 'border-accent text-fg' : 'border-transparent text-muted hover:text-fg'
          }`}
        >
          {tab.label}
        </button>
      ))}
    </div>
  )
}

function AddCardToDeckForm({
  onAdd,
}: {
  onAdd: (cardName: string, quantity: number, section: DeckSection) => Promise<void>
}) {
  const [cardName, setCardName] = useState('')
  const [quantity, setQuantity] = useState(1)
  const [section, setSection] = useState<DeckSection>('Main')
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!cardName.trim()) return
    setIsSubmitting(true)
    try {
      await onAdd(cardName.trim(), quantity, section)
      setCardName('')
      setQuantity(1)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="mb-4 flex flex-wrap items-end gap-2">
      <div className="flex-1 basis-40">
        <label className="mb-1 block text-xs text-muted" htmlFor="add-card-name">
          Add a card
        </label>
        <input
          id="add-card-name"
          value={cardName}
          onChange={(e) => setCardName(e.target.value)}
          placeholder="Card name"
          className={`${INPUT_CLASS} text-sm`}
        />
      </div>
      <div className="w-16">
        <label className="mb-1 block text-xs text-muted" htmlFor="add-card-qty">
          Qty
        </label>
        <input
          id="add-card-qty"
          type="number"
          min={1}
          value={quantity}
          onChange={(e) => setQuantity(Math.max(1, Number(e.target.value)))}
          className={`${INPUT_CLASS} text-sm`}
        />
      </div>
      <div className="w-32">
        <label className="mb-1 block text-xs text-muted" htmlFor="add-card-section">
          Section
        </label>
        <select
          id="add-card-section"
          value={section}
          onChange={(e) => setSection(e.target.value as DeckSection)}
          className={`${INPUT_CLASS} text-sm`}
        >
          {SECTIONS.map((s) => (
            <option key={s} value={s} className="bg-surface text-fg">
              {s}
            </option>
          ))}
        </select>
      </div>
      <button type="submit" disabled={isSubmitting} className={PRIMARY_BUTTON_CLASS}>
        {isSubmitting ? 'Adding…' : 'Add'}
      </button>
    </form>
  )
}

function EntryGroup({
  title,
  section,
  caption,
  entries,
  groupByType = false,
  onRemove,
  onMove,
}: {
  title: string
  section: DeckSection
  caption?: string
  entries: DeckDetail['entries']
  groupByType?: boolean
  onRemove: (cardName: string, section: DeckSection) => Promise<void>
  onMove: (cardName: string, quantity: number, fromSection: DeckSection, toSection: DeckSection) => Promise<void>
}) {
  if (entries.length === 0) return null

  const otherSections = SECTIONS.filter((s) => s !== section)

  return (
    <div className="mb-4">
      <h3 className="mb-1 text-sm font-medium text-muted">
        {title}
        {caption && <span className="ml-2 text-xs italic text-muted/70">({caption})</span>}
      </h3>
      {groupByType ? (
        <div className="columns-1 gap-x-8 sm:columns-2">
          {groupByCategory(entries).map(({ category, entries: categoryEntries }) => (
            <div key={category} className="mb-3 break-inside-avoid-column">
              <h4 className="mb-0.5 text-xs text-muted">
                {CATEGORY_LABELS[category] ?? category} (
                {categoryEntries.reduce((sum, e) => sum + e.quantity, 0)})
              </h4>
              <EntryList
                entries={categoryEntries}
                section={section}
                otherSections={otherSections}
                onRemove={onRemove}
                onMove={onMove}
              />
            </div>
          ))}
        </div>
      ) : (
        <EntryList
          entries={entries}
          section={section}
          otherSections={otherSections}
          onRemove={onRemove}
          onMove={onMove}
        />
      )}
    </div>
  )
}

// A plain, dense list — one row per card, no boxes or per-cell borders. Deliberately close to
// a text decklist (name, quantity, type-grouped sections) rather than a card/grid UI.
function EntryList({
  entries,
  section,
  otherSections,
  onRemove,
  onMove,
}: {
  entries: DeckDetail['entries']
  section: DeckSection
  otherSections: DeckSection[]
  onRemove: (cardName: string, section: DeckSection) => Promise<void>
  onMove: (cardName: string, quantity: number, fromSection: DeckSection, toSection: DeckSection) => Promise<void>
}) {
  return (
    <ul>
      {entries.map((entry) => (
        <EntryRow
          key={`${entry.cardId}-${entry.section}`}
          entry={entry}
          section={section}
          otherSections={otherSections}
          onRemove={onRemove}
          onMove={onMove}
        />
      ))}
    </ul>
  )
}

function EntryRow({
  entry,
  section,
  otherSections,
  onRemove,
  onMove,
}: {
  entry: DeckDetail['entries'][number]
  section: DeckSection
  otherSections: DeckSection[]
  onRemove: (cardName: string, section: DeckSection) => Promise<void>
  onMove: (cardName: string, moveQuantity: number, fromSection: DeckSection, toSection: DeckSection) => Promise<void>
}) {
  // A single copy can just move — nothing to choose. With 2+ copies, ask how many, defaulting
  // to "all" so the common case is still one click, but a partial move is just as easy.
  const [pendingTarget, setPendingTarget] = useState<DeckSection | null>(null)
  const [moveQty, setMoveQty] = useState(entry.quantity)

  // Actions take zero layout width until revealed, so the card name gets the space instead of
  // sharing it with a permanently-reserved (if invisible) action slot. Desktop reveals on hover
  // via the [@media(hover:hover)]:group-hover: rule below (no re-render needed); touch devices
  // don't have hover, so a tap toggles this state instead, and a tap outside the row closes it.
  const [revealed, setRevealed] = useState(false)
  const rowRef = useRef<HTMLLIElement>(null)

  useEffect(() => {
    if (!revealed) return
    function handlePointerDownOutside(e: PointerEvent) {
      if (rowRef.current && !rowRef.current.contains(e.target as Node)) {
        setRevealed(false)
      }
    }
    document.addEventListener('pointerdown', handlePointerDownOutside)
    return () => document.removeEventListener('pointerdown', handlePointerDownOutside)
  }, [revealed])

  function handleRowPointerUp(e: ReactPointerEvent<HTMLLIElement>) {
    if (e.pointerType === 'touch') setRevealed((r) => !r)
  }

  function handleSelectTarget(target: DeckSection) {
    if (entry.quantity <= 1) {
      void onMove(entry.cardName, entry.quantity, section, target)
      return
    }
    setMoveQty(entry.quantity)
    setPendingTarget(target)
  }

  function handleConfirmMove() {
    if (!pendingTarget) return
    void onMove(entry.cardName, moveQty, section, pendingTarget)
    setPendingTarget(null)
  }

  return (
    <li
      ref={rowRef}
      onPointerUp={handleRowPointerUp}
      className="group flex items-center justify-between gap-2 rounded px-1 py-0.5 text-sm hover:bg-surface-hover"
    >
      <span className="flex min-w-0 flex-1 items-baseline gap-1.5">
        <span className="truncate text-fg" title={entry.cardName}>
          {entry.cardName}
        </span>
        <span className="shrink-0 text-xs text-muted">×{entry.quantity}</span>
      </span>

      <span className="flex shrink-0 items-center gap-2 text-xs text-muted tabular-nums">
        <ManaCost manaCost={entry.manaCost} />
        <span className="w-14 text-right" title="Price (USD)">
          {entry.priceUsd != null ? `$${entry.priceUsd.toFixed(2)}` : '—'}
        </span>
      </span>

      {pendingTarget ? (
        <div className="flex flex-shrink-0 flex-wrap items-center justify-end gap-1.5">
          <span className="text-xs text-muted">Move</span>
          <input
            type="number"
            aria-label={`Quantity of ${entry.cardName} to move`}
            min={1}
            max={entry.quantity}
            value={moveQty}
            onChange={(e) => setMoveQty(Math.min(entry.quantity, Math.max(1, Number(e.target.value))))}
            className="w-12 rounded border border-border bg-surface px-1 py-0.5 text-xs text-fg"
          />
          <span className="text-xs text-muted">to {pendingTarget}</span>
          <button
            onClick={handleConfirmMove}
            className="rounded px-1.5 py-0.5 text-xs font-medium text-accent-strong hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            Move
          </button>
          <button
            onClick={() => setPendingTarget(null)}
            className="rounded px-1.5 py-0.5 text-xs text-muted hover:text-fg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            Cancel
          </button>
        </div>
      ) : (
        <div
          onPointerUp={(e) => e.stopPropagation()}
          className={`grid shrink-0 transition-[grid-template-columns] duration-300 ease-out focus-within:grid-cols-[1fr] [@media(hover:hover)]:group-hover:grid-cols-[1fr] ${
            revealed ? 'grid-cols-[1fr]' : 'grid-cols-[0fr]'
          }`}
        >
          <div className="flex min-w-0 items-center gap-1 overflow-hidden">
            <select
              aria-label={`Move ${entry.cardName}`}
              defaultValue=""
              onChange={(e) => {
                const target = e.target.value as DeckSection
                if (target) handleSelectTarget(target)
                e.target.value = ''
              }}
              className="rounded bg-transparent px-1 py-0.5 text-xs text-muted hover:text-fg"
            >
              <option value="" disabled className="bg-surface text-fg">
                Move to…
              </option>
              {otherSections.map((s) => (
                <option key={s} value={s} className="bg-surface text-fg">
                  {s}
                </option>
              ))}
            </select>
            <button
              onClick={() => void onRemove(entry.cardName, section)}
              aria-label={`Remove ${entry.cardName}`}
              className="rounded p-1 text-muted transition-colors hover:bg-danger/10 hover:text-danger focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              <TrashIcon />
            </button>
          </div>
        </div>
      )}
    </li>
  )
}

function TrashIcon() {
  return (
    <svg
      width="14"
      height="14"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M3 6h18" />
      <path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
      <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
      <line x1="10" y1="11" x2="10" y2="17" />
      <line x1="14" y1="11" x2="14" y2="17" />
    </svg>
  )
}
