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
    <div className="mx-auto max-w-5xl px-4 py-8">
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-2xl font-semibold text-slate-100">My Decks</h1>
        <Link
          to="/decks/import"
          className="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-500"
        >
          + Import Deck
        </Link>
      </div>

      {error && <p className="text-sm text-red-400">{error}</p>}

      {decks === null && !error && <p className="text-slate-400">Loading…</p>}

      {decks !== null && decks.length === 0 && (
        <p className="text-slate-400">
          No decks yet.{' '}
          <Link to="/decks/import" className="text-blue-400 hover:underline">
            Import your first one.
          </Link>
        </p>
      )}

      {decks !== null && decks.length > 0 && (
        <div className="grid gap-3 sm:grid-cols-2">
          {decks.map((deck) => (
            <Link
              key={deck.id}
              to={`/decks/${deck.id}`}
              className="rounded-lg border border-slate-800 bg-slate-900/60 p-4 hover:border-slate-600"
            >
              <div className="flex items-center justify-between">
                <h2 className="font-medium text-slate-100">{deck.name}</h2>
                <span className="rounded bg-slate-800 px-2 py-0.5 text-xs text-slate-300">
                  {deck.format}
                </span>
              </div>
              {deck.description && (
                <p className="mt-1 line-clamp-2 text-sm text-slate-400">{deck.description}</p>
              )}
              <p className="mt-2 text-xs text-slate-500">
                {deck.mainDeckCount} main · {deck.sideboardCount} sideboard
              </p>
            </Link>
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <div className="mt-6 flex items-center justify-center gap-3 text-sm text-slate-300">
          <button
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className="rounded-md bg-slate-800 px-3 py-1.5 disabled:opacity-40"
          >
            Previous
          </button>
          <span>
            Page {page} of {totalPages}
          </span>
          <button
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="rounded-md bg-slate-800 px-3 py-1.5 disabled:opacity-40"
          >
            Next
          </button>
        </div>
      )}
    </div>
  )
}
