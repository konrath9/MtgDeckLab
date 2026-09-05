import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ALL_FORMATS, importDeck } from '../api/decks'
import type { DeckSection, Format, UnresolvedCardName } from '../api/types'
import { extractErrorMessage } from '../api/client'

const INPUT_CLASS =
  'w-full rounded-md border border-border bg-surface px-3 py-2 text-fg transition-colors focus:border-accent focus:outline-none focus-visible:ring-2 focus-visible:ring-accent/50'

export function ImportDeckPage() {
  const navigate = useNavigate()
  const { t } = useTranslation()
  const [name, setName] = useState('')
  const [format, setFormat] = useState<Format>('Commander')
  const [description, setDescription] = useState('')
  const [mainDecklist, setMainDecklist] = useState('')
  const [commanderDecklist, setCommanderDecklist] = useState('')
  const [sideboardDecklist, setSideboardDecklist] = useState('')
  const [maybeboardDecklist, setMaybeboardDecklist] = useState('')
  const [showOptional, setShowOptional] = useState(false)
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
      setError(extractErrorMessage(err, t('import.error')))
    } finally {
      setIsSubmitting(false)
    }
  }

  const unresolvedBySection = unresolved.reduce<Partial<Record<DeckSection, string[]>>>((acc, u) => {
    ;(acc[u.section] ??= []).push(u.cardName)
    return acc
  }, {})

  const hasOptionalContent = Boolean(sideboardDecklist || maybeboardDecklist)

  return (
    <div className="mx-auto max-w-2xl px-4 py-8">
      <h1 className="mb-1 text-2xl font-semibold tracking-tight text-fg">{t('import.title')}</h1>
      <p className="mb-6 text-sm text-muted">{t('import.subtitle')}</p>

      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Identity — small, secondary fields, deliberately quiet */}
        <div className="flex gap-3">
          <div className="flex-1">
            <label className="mb-1 block text-xs text-muted" htmlFor="name">
              {t('import.deckName')}
            </label>
            <input
              id="name"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              className={`${INPUT_CLASS} text-sm`}
            />
          </div>
          <div className="w-40">
            <label className="mb-1 block text-xs text-muted" htmlFor="format">
              {t('import.format')}
            </label>
            <select
              id="format"
              value={format}
              onChange={(e) => setFormat(e.target.value as Format)}
              className={`${INPUT_CLASS} text-sm`}
            >
              {ALL_FORMATS.map((f) => (
                <option key={f} value={f} className="bg-surface text-fg">
                  {f}
                </option>
              ))}
            </select>
          </div>
        </div>

        {format === 'Commander' && (
          <div>
            <label className="mb-1 block text-xs text-muted" htmlFor="commanderDecklist">
              {t('import.commander')}
            </label>
            <input
              id="commanderDecklist"
              value={commanderDecklist}
              onChange={(e) => setCommanderDecklist(e.target.value)}
              placeholder={t('import.commanderPlaceholder')}
              className={`${INPUT_CLASS} font-mono text-sm`}
            />
          </div>
        )}

        <div>
          <label className="mb-1 block text-xs text-muted" htmlFor="description">
            {t('import.description')}
          </label>
          <input
            id="description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className={`${INPUT_CLASS} text-sm`}
          />
        </div>

        {/* Main Deck — the one required action, visually the largest and strongest thing here */}
        <div>
          <label className="mb-1 block text-sm font-medium text-fg" htmlFor="mainDecklist">
            {t('import.mainDeck')}
          </label>
          <textarea
            id="mainDecklist"
            required
            rows={16}
            value={mainDecklist}
            onChange={(e) => setMainDecklist(e.target.value)}
            placeholder={t('import.mainDeckPlaceholder')}
            className={`${INPUT_CLASS} font-mono text-sm`}
          />
          <p className="mt-1 text-xs text-muted">
            {t('import.mainDeckHint')} {t('import.languageHint')}
          </p>
        </div>

        {/* Sideboard/Maybeboard — collapsed by default so the page doesn't compete with Main Deck */}
        {!showOptional && !hasOptionalContent ? (
          <button
            type="button"
            onClick={() => setShowOptional(true)}
            className="text-sm text-accent-strong hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            {t('import.addOptional')}
          </button>
        ) : (
          <div className="space-y-4 border-l-2 border-border pl-4">
            <div>
              <label className="mb-1 block text-xs text-muted" htmlFor="sideboardDecklist">
                {t('import.sideboard')}
              </label>
              <textarea
                id="sideboardDecklist"
                rows={4}
                value={sideboardDecklist}
                onChange={(e) => setSideboardDecklist(e.target.value)}
                placeholder={t('import.sideboardPlaceholder')}
                className={`${INPUT_CLASS} font-mono text-sm`}
              />
            </div>
            <div>
              <label className="mb-1 block text-xs text-muted" htmlFor="maybeboardDecklist">
                {t('import.maybeboard')}
              </label>
              <textarea
                id="maybeboardDecklist"
                rows={4}
                value={maybeboardDecklist}
                onChange={(e) => setMaybeboardDecklist(e.target.value)}
                placeholder={t('import.maybeboardPlaceholder')}
                className={`${INPUT_CLASS} font-mono text-sm`}
              />
              <p className="mt-1 text-xs text-muted">{t('import.maybeboardHint')}</p>
            </div>
          </div>
        )}

        {error && <p className="text-sm text-danger">{error}</p>}
        {unresolved.length > 0 && (
          <div className="space-y-2 text-sm text-warning">
            {(Object.keys(unresolvedBySection) as DeckSection[]).map((section) => (
              <p key={section}>
                {t('import.unresolved', {
                  section: t(`sections.${section}`),
                  cards: unresolvedBySection[section]!.join(', '),
                })}
              </p>
            ))}
            {pendingDeckId && (
              <button
                type="button"
                onClick={() => navigate(`/decks/${pendingDeckId}`)}
                className="rounded-md border border-border px-3 py-1.5 text-sm text-fg transition-colors hover:bg-surface-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
              >
                {t('import.continueToDeck')}
              </button>
            )}
          </div>
        )}
        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded-md bg-accent px-4 py-2 font-medium text-white transition-colors hover:bg-accent-strong disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
        >
          {isSubmitting ? t('import.submitting') : t('import.submit')}
        </button>
      </form>
    </div>
  )
}
