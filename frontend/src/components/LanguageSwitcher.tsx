import { useTranslation } from 'react-i18next'
import { LANGUAGE_LABELS, SUPPORTED_LANGUAGES, isSupportedLanguage } from '../i18n'

/**
 * Troca o idioma da interface. A escolha é persistida pelo detector do i18next (localStorage) e
 * passa a valer também para as respostas da API, via header Accept-Language.
 *
 * Deliberadamente discreto: é configuração, não navegação — não deve competir com "Meus decks"
 * nem com a ação primária da página.
 */
export function LanguageSwitcher() {
  const { i18n, t } = useTranslation()

  const current = isSupportedLanguage(i18n.language) ? i18n.language : SUPPORTED_LANGUAGES[0]

  return (
    <span className="group inline-flex items-center gap-1.5">
      <i className="fa-solid fa-globe text-xs text-muted transition-colors group-hover:text-accent-strong dark:text-accent-soft" aria-hidden="true" />
      <select
        aria-label={t('nav.language')}
        value={current}
        onChange={(e) => void i18n.changeLanguage(e.target.value)}
        className="rounded-md border border-border bg-surface px-2 py-1.5 text-xs text-muted transition-colors hover:border-accent hover:bg-accent/10 hover:text-accent-strong focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
      >
        {SUPPORTED_LANGUAGES.map((language) => (
          <option key={language} value={language} className="bg-surface text-fg">
            {LANGUAGE_LABELS[language]}
          </option>
        ))}
      </select>
    </span>
  )
}
