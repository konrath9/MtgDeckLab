import type { AnalysisValidationResult } from '../api/types'

export function ValidationList({ validation }: { validation: AnalysisValidationResult }) {
  if (validation.errors.length === 0 && validation.warnings.length === 0) {
    return (
      <p className="flex items-center gap-2 text-sm text-emerald-300">
        <span aria-hidden>✓</span> No issues detected.
      </p>
    )
  }

  return (
    <ul className="space-y-1.5 text-sm">
      {validation.errors.map((error, i) => (
        <li key={`error-${i}`} className="flex gap-2 text-red-300">
          <span aria-hidden>✕</span>
          <span>{error}</span>
        </li>
      ))}
      {validation.warnings.map((warning, i) => (
        <li key={`warning-${i}`} className="flex gap-2 text-amber-300">
          <span aria-hidden>⚠</span>
          <span>{warning}</span>
        </li>
      ))}
    </ul>
  )
}
