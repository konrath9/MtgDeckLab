import { useTranslation } from 'react-i18next'
import type { AnalysisValidationResult } from '../api/types'

export function ValidationList({ validation }: { validation: AnalysisValidationResult }) {
  const { t } = useTranslation()

  if (validation.errors.length === 0 && validation.warnings.length === 0) {
    return (
      <p className="flex items-center gap-2 text-sm text-success">
        <span aria-hidden>✓</span> {t('analysis.validation.noIssues')}
      </p>
    )
  }

  // As mensagens vêm da API já no idioma do usuário (o Engine só produz código + argumentos),
  // então aqui é só renderizar o texto.
  return (
    <ul className="space-y-1.5 text-sm">
      {validation.errors.map((error) => (
        <li key={`error-${error.code}-${error.text}`} className="flex gap-2 text-danger">
          <span aria-hidden>✕</span>
          <span>{error.text}</span>
        </li>
      ))}
      {validation.warnings.map((warning) => (
        <li key={`warning-${warning.code}-${warning.text}`} className="flex gap-2 text-warning">
          <span aria-hidden>⚠</span>
          <span>{warning.text}</span>
        </li>
      ))}
    </ul>
  )
}
