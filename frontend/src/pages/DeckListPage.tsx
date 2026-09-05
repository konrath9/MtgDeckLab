import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { listDecks } from '../api/decks'
import type { DeckSummary } from '../api/types'
import { extractErrorMessage } from '../api/client'

export function DeckListPage() {
  const { t } = useTranslation()
  const [decks, setDecks] = useState<DeckSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)

  useEffect(() => {
    let cancelled = false
    setDecks(null)
    listDecks(page)
      .then((result) => {
        if (cancelled) return
        setDecks(result.items)
        setTotalPages(Math.max(result.totalPages, 1))
      })
      .catch((err) => {
        if (!cancelled) setError(extractErrorMessage(err, t('deckList.error')))
      })
    return () => {
      cancelled = true
    }
  }, [page, t])

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <div className="mb-8 flex items-center justify-between">
        <h1 className="text-2xl font-semibold tracking-tight text-fg">{t('deckList.title')}</h1>
        {decks !== null && decks.length > 0 && (
          <Link
            to="/decks/import"
            className="inline-flex items-center gap-2 rounded-md bg-accent px-4 py-2 text-sm font-medium text-white shadow-sm transition duration-150 hover:-translate-y-px hover:bg-accent-strong hover:shadow-md active:translate-y-0 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
          >
            <i className="fa-solid fa-file-import text-xs" aria-hidden="true" />
            {t('deckList.importCta')}
          </Link>
        )}
      </div>

      {error && <p className="text-sm text-danger">{error}</p>}

      {decks === null && !error && <p className="text-muted">{t('common.loading')}</p>}

      {/* Empty state doubles as this app's landing moment — the primary action ("analyze your
          deck") should be obvious and undistracted, not a quiet inline link. */}
      {decks !== null && decks.length === 0 && (
        <div className="flex flex-col items-center gap-3 rounded-md border border-border py-20 text-center">
          <h2 className="text-xl font-semibold tracking-tight text-fg">{t('deckList.empty.title')}</h2>
          <p className="max-w-sm text-sm text-muted">{t('deckList.empty.description')}</p>
          <Link
            to="/decks/import"
            className="mt-2 inline-flex items-center gap-2 rounded-md bg-accent px-5 py-2.5 text-sm font-medium text-white shadow-sm transition duration-150 hover:-translate-y-px hover:bg-accent-strong hover:shadow-md active:translate-y-0 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
          >
            <i className="fa-solid fa-file-import text-sm" aria-hidden="true" />
            {t('deckList.empty.cta')}
          </Link>
        </div>
      )}

      {decks !== null && decks.length > 0 && (
        <div className="grid gap-3 sm:grid-cols-2">
          {decks.map((deck) => (
            <Link
              key={deck.id}
              to={`/decks/${deck.id}`}
              className="group rounded-md border border-border bg-surface p-4 transition duration-200 hover:-translate-y-0.5 hover:border-border-strong hover:bg-surface-hover hover:shadow-md active:translate-y-0 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
            >
              <div className="flex items-center justify-between">
                <h2 className="font-medium text-fg">{deck.name}</h2>
                {/* Nome de formato é o mesmo em qualquer idioma (Commander, Modern, ...). */}
                <span className="rounded bg-surface-hover px-2 py-0.5 text-xs text-muted">{deck.format}</span>
              </div>
              {deck.description && <p className="mt-1 line-clamp-2 text-sm text-muted">{deck.description}</p>}
              <p className="mt-2 text-xs text-muted">
                {t('deckList.counts', { main: deck.mainDeckCount, sideboard: deck.sideboardCount })}
              </p>
            </Link>
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <div className="mt-6 flex items-center justify-center gap-3 text-sm text-fg">
          <button
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className="rounded-md border border-border px-3 py-1.5 transition-colors hover:bg-surface-hover disabled:opacity-40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
          >
            {t('common.previous')}
          </button>
          <span className="text-muted">{t('common.pageOf', { page, total: totalPages })}</span>
          <button
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="rounded-md border border-border px-3 py-1.5 transition-colors hover:bg-surface-hover disabled:opacity-40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
          >
            {t('common.next')}
          </button>
        </div>
      )}
    </div>
  )
}
