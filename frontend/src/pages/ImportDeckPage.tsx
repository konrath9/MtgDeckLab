import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { ALL_FORMATS, importDeck } from '../api/decks'
import type { DeckSection, Format, UnresolvedCardName } from '../api/types'
import { extractErrorMessage } from '../api/client'

const INPUT_CLASS =
  'w-full rounded-md border border-border bg-surface px-3 py-2 text-fg transition-colors focus:border-accent focus:outline-none focus-visible:ring-2 focus-visible:ring-accent/50'

const SECTION_LABELS: Record<DeckSection, string> = {
  Main: 'Main Deck',
  Commander: 'Commander',
  Sideboard: 'Sideboard',
  Maybeboard: 'Maybeboard',
}

export function ImportDeckPage() {
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [format, setFormat] = useState<Format>('Commander')
  const [description, setDescription] = useState('')
  const [mainDecklist, setMainDecklist] = useState('')
  const [commanderDecklist, setCommanderDecklist] = useState('')
  const [sideboardDecklist, setSideboardDecklist] = useState('')
  const [maybeboardDecklist, setMaybeboardDecklist] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [unresolved, setUnresolved] = useState<UnresolvedCardName[]>([])
  const [pendingDeckId, setPendingDeckId] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setUnresolved([])
    setPendingDeckId(null)
    setIsSubmitting(true)
    try {
      const result = await importDeck(name, format, mainDecklist, {
        commanderDecklist: format === 'Commander' ? commanderDecklist || undefined : undefined,
        sideboardDecklist: sideboardDecklist || undefined,
        maybeboardDecklist: maybeboardDecklist || undefined,
        description: description || undefined,
      })
      if (result.unresolvedCardNames.length > 0) {
        setUnresolved(result.unresolvedCardNames)
        setPendingDeckId(result.deckId)
      } else {
        navigate(`/decks/${result.deckId}`)
      }
    } catch (err) {
      setError(extractErrorMessage(err, 'Could not import deck.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  const unresolvedBySection = unresolved.reduce<Partial<Record<DeckSection, string[]>>>((acc, u) => {
    ;(acc[u.section] ??= []).push(u.cardName)
    return acc
  }, {})

  return (
    <div className="mx-auto max-w-2xl px-4 py-8">
      <h1 className="mb-6 text-2xl font-semibold tracking-tight text-fg">Import Deck</h1>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="mb-1 block text-sm text-muted" htmlFor="name">
            Deck name
          </label>
          <input id="name" required value={name} onChange={(e) => setName(e.target.value)} className={INPUT_CLASS} />
        </div>
        <div>
          <label className="mb-1 block text-sm text-muted" htmlFor="format">
            Format
          </label>
          <select
            id="format"
            value={format}
            onChange={(e) => setFormat(e.target.value as Format)}
            className={INPUT_CLASS}
          >
            {ALL_FORMATS.map((f) => (
              <option key={f} value={f} className="bg-surface text-fg">
                {f}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-sm text-muted" htmlFor="description">
            Description (optional)
          </label>
          <input
            id="description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className={INPUT_CLASS}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm text-muted" htmlFor="mainDecklist">
            Main Deck
          </label>
          <textarea
            id="mainDecklist"
            required
            rows={14}
            value={mainDecklist}
            onChange={(e) => setMainDecklist(e.target.value)}
            placeholder={'4 Lightning Bolt\n20 Mountain'}
            className={`${INPUT_CLASS} font-mono text-sm`}
          />
          <p className="mt-1 text-xs text-muted">
            One card per line — e.g. "4 Lightning Bolt". Paste a full multi-section list here and
            SB:/#Commander tags still work.
          </p>
        </div>

        {format === 'Commander' && (
          <div>
            <label className="mb-1 block text-sm text-muted" htmlFor="commanderDecklist">
              Commander
            </label>
            <textarea
              id="commanderDecklist"
              rows={2}
              value={commanderDecklist}
              onChange={(e) => setCommanderDecklist(e.target.value)}
              placeholder={"1 Atraxa, Praetors' Voice"}
              className={`${INPUT_CLASS} font-mono text-sm`}
            />
          </div>
        )}

        <div>
          <label className="mb-1 block text-sm text-muted" htmlFor="sideboardDecklist">
            Sideboard (optional)
          </label>
          <textarea
            id="sideboardDecklist"
            rows={4}
            value={sideboardDecklist}
            onChange={(e) => setSideboardDecklist(e.target.value)}
            placeholder={'2 Duress'}
            className={`${INPUT_CLASS} font-mono text-sm`}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm text-muted" htmlFor="maybeboardDecklist">
            Maybeboard (optional)
          </label>
          <textarea
            id="maybeboardDecklist"
            rows={4}
            value={maybeboardDecklist}
            onChange={(e) => setMaybeboardDecklist(e.target.value)}
            placeholder={'1 Rhystic Study'}
            className={`${INPUT_CLASS} font-mono text-sm`}
          />
          <p className="mt-1 text-xs text-muted">
            Cards you're considering but haven't committed to — excluded from score, curve, and
            validation.
          </p>
        </div>

        {error && <p className="text-sm text-danger">{error}</p>}
        {unresolved.length > 0 && (
          <div className="space-y-2 text-sm text-warning">
            {(Object.keys(unresolvedBySection) as DeckSection[]).map((section) => (
              <p key={section}>
                Not found in {SECTION_LABELS[section]}: {unresolvedBySection[section]!.join(', ')}
              </p>
            ))}
            {pendingDeckId && (
              <button
                type="button"
                onClick={() => navigate(`/decks/${pendingDeckId}`)}
                className="rounded-md border border-border px-3 py-1.5 text-sm text-fg transition-colors hover:bg-surface-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
              >
                Continue to deck
              </button>
            )}
          </div>
        )}
        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded-md bg-accent px-4 py-2 font-medium text-white transition-colors hover:bg-accent-strong disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
        >
          {isSubmitting ? 'Importing…' : 'Import Deck'}
        </button>
      </form>
    </div>
  )
}
