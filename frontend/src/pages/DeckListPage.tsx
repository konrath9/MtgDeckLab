import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listDecks } from '../api/decks'
import type { DeckSummary } from '../api/types'
import { extractErrorMessage } from '../api/client'

export function DeckListPage() {
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
        if (!cancelled) setError(extractErrorMessage(err, 'Could not load your decks.'))
      })
    return () => {
      cancelled = true
    }
  }, [page])

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <div className="mb-8 flex items-center justify-between">
        <h1 className="text-2xl font-semibold tracking-tight text-fg">My Decks</h1>
        {decks !== null && decks.length > 0 && (
          <Link
            to="/decks/import"
            className="rounded-md bg-accent px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-accent-strong focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
          >
            Import Deck
          </Link>
        )}
      </div>

      {error && <p className="text-sm text-danger">{error}</p>}

      {decks === null && !error && <p className="text-muted">Loading…</p>}

      {/* Empty state doubles as this app's landing moment — the primary action ("analyze your
          deck") should be obvious and undistracted, not a quiet inline link. */}
      {decks !== null && decks.length === 0 && (
        <div className="flex flex-col items-center gap-3 rounded-md border border-border py-20 text-center">
          <h2 className="text-xl font-semibold tracking-tight text-fg">Analyze your deck</h2>
          <p className="max-w-sm text-sm text-muted">
            Paste a decklist to see its mana curve, color balance, and format legality in seconds.
          </p>
          <Link
            to="/decks/import"
            className="mt-2 rounded-md bg-accent px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-accent-strong focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
          >
            Import a deck
          </Link>
        </div>
      )}

      {decks !== null && decks.length > 0 && (
        <div className="grid gap-3 sm:grid-cols-2">
          {decks.map((deck) => (
            <Link
              key={deck.id}
              to={`/decks/${deck.id}`}
              className="rounded-md border border-border bg-surface p-4 transition-colors hover:border-border-strong focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
            >
              <div className="flex items-center justify-between">
                <h2 className="font-medium text-fg">{deck.name}</h2>
                <span className="rounded bg-surface-hover px-2 py-0.5 text-xs text-muted">{deck.format}</span>
              </div>
              {deck.description && <p className="mt-1 line-clamp-2 text-sm text-muted">{deck.description}</p>}
              <p className="mt-2 text-xs text-muted">
                {deck.mainDeckCount} main · {deck.sideboardCount} sideboard
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
            Previous
          </button>
          <span className="text-muted">
            Page {page} of {totalPages}
          </span>
          <button
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="rounded-md border border-border px-3 py-1.5 transition-colors hover:bg-surface-hover disabled:opacity-40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
          >
            Next
          </button>
        </div>
      )}
    </div>
  )
}
