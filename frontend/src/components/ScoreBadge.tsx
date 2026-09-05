const GRADE_COLORS: Record<string, string> = {
  A: 'text-success',
  B: 'text-success',
  C: 'text-warning',
  D: 'text-danger',
  F: 'text-danger',
}

// Deterministic grade -> label/description mapping. Purely a restatement of the letter grade
// DeckScorer already computes — not a generated or per-deck judgment.
const GRADE_INTERPRETATION: Record<string, { label: string; description: string }> = {
  A: { label: 'Excellent', description: 'Strong fundamentals across the board.' },
  B: { label: 'Good', description: 'Solid deck with minor gaps.' },
  C: { label: 'Fair', description: 'Playable, but several areas need attention.' },
  D: { label: 'Weak', description: 'Significant issues affecting consistency.' },
  F: { label: 'Poor', description: 'Major structural problems to address.' },
}

export function ScoreBadge({ score, grade }: { score: number; grade: string }) {
  const gradeColor = GRADE_COLORS[grade] ?? GRADE_COLORS.C
  const interpretation = GRADE_INTERPRETATION[grade] ?? GRADE_INTERPRETATION.C

  return (
    <div className="flex flex-wrap items-baseline gap-x-5 gap-y-2">
      <span className={`text-8xl font-bold leading-none tracking-tight ${gradeColor}`}>{grade}</span>
      <div className="flex flex-col gap-0.5">
        <div className="text-2xl font-semibold text-fg">
          {score}
          <span className="text-lg font-normal text-muted"> / 100</span>
        </div>
        <div className={`text-base font-medium ${gradeColor}`}>{interpretation.label}</div>
        <div className="text-sm text-muted">{interpretation.description}</div>
      </div>
    </div>
  )
}
