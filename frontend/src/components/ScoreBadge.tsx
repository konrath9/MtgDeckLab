const GRADE_COLORS: Record<string, string> = {
  A: 'bg-emerald-500/20 text-emerald-300 ring-emerald-500/40',
  B: 'bg-lime-500/20 text-lime-300 ring-lime-500/40',
  C: 'bg-amber-500/20 text-amber-300 ring-amber-500/40',
  D: 'bg-orange-500/20 text-orange-300 ring-orange-500/40',
  F: 'bg-red-500/20 text-red-300 ring-red-500/40',
}

export function ScoreBadge({ score, grade }: { score: number; grade: string }) {
  const colors = GRADE_COLORS[grade] ?? GRADE_COLORS.C

  return (
    <div className={`flex items-center gap-3 rounded-lg px-4 py-3 ring-1 ${colors}`}>
      <span className="text-3xl font-bold">{grade}</span>
      <div className="text-sm">
        <div className="font-medium">Deck Score</div>
        <div className="opacity-80">{score} / 100</div>
      </div>
    </div>
  )
}
