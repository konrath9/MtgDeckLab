import { useTranslation } from 'react-i18next'

const GRADE_COLORS: Record<string, string> = {
  A: 'text-success',
  B: 'text-success',
  C: 'text-warning',
  D: 'text-danger',
  F: 'text-danger',
}

const FALLBACK_GRADE = 'C'

export function ScoreBadge({ score, grade }: { score: number; grade: string }) {
  const { t } = useTranslation()

  const gradeColor = GRADE_COLORS[grade] ?? GRADE_COLORS[FALLBACK_GRADE]

  // Rótulo e descrição são apenas a leitura da letra que o DeckScorer já calculou — não são um
  // julgamento por deck. A letra em si é universal; só o texto ao lado dela é traduzido.
  const key = grade in GRADE_COLORS ? grade : FALLBACK_GRADE

  return (
    <div className="flex flex-wrap items-baseline gap-x-5 gap-y-2">
      <span className={`text-8xl font-bold leading-none tracking-tight ${gradeColor}`}>{grade}</span>
      <div className="flex flex-col gap-0.5">
        <div className="text-2xl font-semibold text-fg">
          {score}
          <span className="text-lg font-normal text-muted"> {t('analysis.score.outOf')}</span>
        </div>
        <div className={`text-base font-medium ${gradeColor}`}>{t(`analysis.score.${key}.label`)}</div>
        <div className="text-sm text-muted">{t(`analysis.score.${key}.description`)}</div>
      </div>
    </div>
  )
}
