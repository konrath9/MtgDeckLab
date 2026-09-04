import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { deleteDeck, getDeck, getDeckAnalysis, updateDeck } from '../api/decks'
import type { DeckAnalysisResult, DeckDetail } from '../api/types'
import { extractErrorMessage } from '../api/client'
import { ScoreBadge } from '../components/ScoreBadge'
import { ManaCurveChart } from '../components/ManaCurveChart'
import { ColorDistributionChart } from '../components/ColorDistributionChart'
import { TypeDistributionChart } from '../components/TypeDistributionChart'
import { ValidationList } from '../components/ValidationList'

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

  useEffect(() => {
    if (!id) return
    let cancelled = false

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
    await deleteDeck(id)
    navigate('/')
  }

  if (error) return <p className="mx-auto max-w-5xl px-4 py-8 text-red-400">{error}</p>
  if (!deck) return <p className="mx-auto max-w-5xl px-4 py-8 text-slate-400">Loading…</p>

  const mainEntries = deck.entries.filter((e) => !e.isSideboard && !e.isCommander)
  const commanderEntries = deck.entries.filter((e) => e.isCommander)
  const sideboardEntries = deck.entries.filter((e) => e.isSideboard)

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <div className="mb-6 flex items-start justify-between">
        <div className="flex-1">
          {isEditing ? (
            <div className="space-y-2">
              <input
                value={editName}
                onChange={(e) => setEditName(e.target.value)}
                className="w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-1.5 text-xl font-semibold text-slate-100 focus:border-blue-500 focus:outline-none"
              />
              <input
                value={editDescription}
                onChange={(e) => setEditDescription(e.target.value)}
                placeholder="Description"
                className="w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-1.5 text-sm text-slate-300 focus:border-blue-500 focus:outline-none"
              />
              <div className="flex gap-2">
                <button
                  onClick={handleSave}
                  disabled={isSaving}
                  className="rounded-md bg-blue-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-blue-500 disabled:opacity-50"
                >
                  {isSaving ? 'Saving…' : 'Save'}
                </button>
                <button
                  onClick={() => setIsEditing(false)}
                  className="rounded-md bg-slate-800 px-3 py-1.5 text-sm text-slate-200 hover:bg-slate-700"
                >
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <>
              <div className="flex items-center gap-3">
                <h1 className="text-2xl font-semibold text-slate-100">{deck.name}</h1>
                <span className="rounded bg-slate-800 px-2 py-0.5 text-xs text-slate-300">{deck.format}</span>
              </div>
              {deck.description && <p className="mt-1 text-sm text-slate-400">{deck.description}</p>}
            </>
          )}
        </div>
        {!isEditing && (
          <div className="flex gap-2">
            <button
              onClick={() => setIsEditing(true)}
              className="rounded-md bg-slate-800 px-3 py-1.5 text-sm text-slate-200 hover:bg-slate-700"
            >
              Edit
            </button>
            <button
              onClick={handleDelete}
              className="rounded-md bg-red-900/50 px-3 py-1.5 text-sm text-red-300 hover:bg-red-900"
            >
              Delete
            </button>
          </div>
        )}
      </div>

      <div className="grid gap-6 lg:grid-cols-[1fr_1.4fr]">
        <section>
          <h2 className="mb-2 text-lg font-medium text-slate-200">
            Entries ({deck.mainDeckCount} main{deck.sideboardCount > 0 ? `, ${deck.sideboardCount} sideboard` : ''})
          </h2>
          {commanderEntries.length > 0 && (
            <EntryGroup title="Commander" entries={commanderEntries} />
          )}
          <EntryGroup title="Main Deck" entries={mainEntries} />
          {sideboardEntries.length > 0 && <EntryGroup title="Sideboard" entries={sideboardEntries} />}
        </section>

        <section>
          <h2 className="mb-2 text-lg font-medium text-slate-200">Analysis</h2>
          {analysisError && <p className="text-sm text-red-400">{analysisError}</p>}
          {!analysis && !analysisError && <p className="text-slate-400">Loading analysis…</p>}
          {analysis && (
            <div className="space-y-6">
              <ScoreBadge score={analysis.score.score} grade={analysis.score.grade} />

              {analysis.score.warnings.length > 0 && (
                <ul className="space-y-1 text-sm text-amber-300">
                  {analysis.score.warnings.map((w, i) => (
                    <li key={i}>⚠ {w}</li>
                  ))}
                </ul>
              )}

              <div className="rounded-lg border border-slate-800 bg-slate-900/60 p-4">
                <h3 className="mb-3 text-sm font-medium text-slate-300">Mana Curve</h3>
                <ManaCurveChart manaCurve={analysis.manaCurve} />
              </div>

              <div className="rounded-lg border border-slate-800 bg-slate-900/60 p-4">
                <h3 className="mb-3 text-sm font-medium text-slate-300">Color Distribution</h3>
                <ColorDistributionChart colorDistribution={analysis.colorDistribution} />
              </div>

              <div className="rounded-lg border border-slate-800 bg-slate-900/60 p-4">
                <h3 className="mb-3 text-sm font-medium text-slate-300">Type Distribution</h3>
                <TypeDistributionChart typeDistribution={analysis.typeDistribution} />
              </div>

              <div className="rounded-lg border border-slate-800 bg-slate-900/60 p-4">
                <h3 className="mb-3 text-sm font-medium text-slate-300">Format Validation</h3>
                <ValidationList validation={analysis.validation} />
              </div>
            </div>
          )}
        </section>
      </div>
    </div>
  )
}

function EntryGroup({
  title,
  entries,
}: {
  title: string
  entries: DeckDetail['entries']
}) {
  if (entries.length === 0) return null

  return (
    <div className="mb-4">
      <h3 className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-500">{title}</h3>
      <ul className="divide-y divide-slate-800 rounded-lg border border-slate-800 bg-slate-900/60">
        {entries.map((entry) => (
          <li
            key={`${entry.cardId}-${entry.isSideboard}-${entry.isCommander}`}
            className="flex items-center justify-between px-3 py-1.5 text-sm"
          >
            <span className="text-slate-200">{entry.cardName}</span>
            <span className="text-slate-400">×{entry.quantity}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}
