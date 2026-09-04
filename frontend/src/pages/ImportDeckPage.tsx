import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { ALL_FORMATS, importDeck } from '../api/decks'
import type { Format } from '../api/types'
import { extractErrorMessage } from '../api/client'

export function ImportDeckPage() {
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [format, setFormat] = useState<Format>('Commander')
  const [description, setDescription] = useState('')
  const [decklist, setDecklist] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [unresolved, setUnresolved] = useState<string[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setUnresolved([])
    setIsSubmitting(true)
    try {
      const result = await importDeck(name, format, decklist, description || undefined)
      if (result.unresolvedCardNames.length > 0) setUnresolved(result.unresolvedCardNames)
      navigate(`/decks/${result.deckId}`)
    } catch (err) {
      setError(extractErrorMessage(err, 'Could not import deck.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="mx-auto max-w-2xl px-4 py-8">
      <h1 className="mb-6 text-2xl font-semibold text-slate-100">Import Deck</h1>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="mb-1 block text-sm text-slate-300" htmlFor="name">
            Deck name
          </label>
          <input
            id="name"
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:border-blue-500 focus:outline-none"
          />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="mb-1 block text-sm text-slate-300" htmlFor="format">
              Format
            </label>
            <select
              id="format"
              value={format}
              onChange={(e) => setFormat(e.target.value as Format)}
              className="w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:border-blue-500 focus:outline-none"
            >
              {ALL_FORMATS.map((f) => (
                <option key={f} value={f}>
                  {f}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm text-slate-300" htmlFor="description">
              Description (optional)
            </label>
            <input
              id="description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:border-blue-500 focus:outline-none"
            />
          </div>
        </div>
        <div>
          <label className="mb-1 block text-sm text-slate-300" htmlFor="decklist">
            Decklist
          </label>
          <textarea
            id="decklist"
            required
            rows={14}
            value={decklist}
            onChange={(e) => setDecklist(e.target.value)}
            placeholder={'4 Lightning Bolt\n20 Mountain\n1 Sol Ring #Commander'}
            className="w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 font-mono text-sm text-slate-100 focus:border-blue-500 focus:outline-none"
          />
          <p className="mt-1 text-xs text-slate-500">
            One card per line — "4 Lightning Bolt", "#Commander" and "SB:" prefixes are supported.
          </p>
        </div>
        {error && <p className="text-sm text-red-400">{error}</p>}
        {unresolved.length > 0 && (
          <p className="text-sm text-amber-300">
            {unresolved.length} card(s) not found in the card database: {unresolved.join(', ')}
          </p>
        )}
        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded-md bg-blue-600 px-4 py-2 font-medium text-white hover:bg-blue-500 disabled:opacity-50"
        >
          {isSubmitting ? 'Importing…' : 'Import Deck'}
        </button>
      </form>
    </div>
  )
}
