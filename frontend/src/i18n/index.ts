import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import enUS from './locales/en-US.json'
import ptBR from './locales/pt-BR.json'

/**
 * Idiomas da interface. A ordem é a que aparece no seletor.
 *
 * Um idioma novo entra aqui + um JSON ao lado, e é preciso que a API também o atenda
 * (`Localization:SupportedCultures`) — senão as mensagens vindas do servidor voltam no idioma
 * padrão dele enquanto a UI já está traduzida.
 */
export const SUPPORTED_LANGUAGES = ['en-US', 'pt-BR'] as const
export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number]

export const DEFAULT_LANGUAGE: SupportedLanguage = 'en-US'

export const LANGUAGE_STORAGE_KEY = 'mtgdecklab.language'

// Rótulos escritos no próprio idioma: quem abriu a aplicação no idioma errado precisa reconhecer
// o seu na lista sem depender de entender o idioma atual.
export const LANGUAGE_LABELS: Record<SupportedLanguage, string> = {
  'en-US': 'English',
  'pt-BR': 'Português',
}

export const resources = {
  'en-US': { translation: enUS },
  'pt-BR': { translation: ptBR },
} as const

// Navegadores costumam mandar só o idioma curto ("pt", "en") em vez da cultura completa que
// usamos em supportedLngs ("pt-BR", "en-US"). Resolvemos isso aqui, no detector — NÃO com
// `nonExplicitSupportedLngs`, que faz o oposto do que parece: ele trunca *toda* checagem de
// idioma (mesmo "pt-BR") para a forma curta antes de comparar com supportedLngs, e como
// supportedLngs só tem os códigos completos, a checagem nunca bate — toResolveHierarchy() volta
// vazio e t() passa a devolver a própria chave para tudo. (Já caímos nisso uma vez.)
function convertDetectedLanguage(detected: string): SupportedLanguage {
  const normalized = detected.replace('_', '-')
  const exact = SUPPORTED_LANGUAGES.find((lang) => lang.toLowerCase() === normalized.toLowerCase())
  if (exact) return exact

  const primary = normalized.split('-')[0].toLowerCase()
  const byPrimary = SUPPORTED_LANGUAGES.find((lang) => lang.split('-')[0].toLowerCase() === primary)
  return byPrimary ?? DEFAULT_LANGUAGE
}

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    supportedLngs: [...SUPPORTED_LANGUAGES],
    fallbackLng: DEFAULT_LANGUAGE,
    detection: {
      // A escolha explícita do usuário (localStorage) ganha da detecção; sem escolha, vale o
      // idioma do navegador — é o que faz a aplicação abrir no idioma da pessoa sem configurar nada.
      order: ['localStorage', 'navigator'],
      lookupLocalStorage: LANGUAGE_STORAGE_KEY,
      caches: ['localStorage'],
      convertDetectedLanguage,
    },
    interpolation: {
      // React já escapa tudo que renderiza.
      escapeValue: false,
    },
  })

// Mantém <html lang> em dia — leitores de tela e a hifenização do navegador dependem disso.
function syncDocumentLanguage(language: string) {
  document.documentElement.lang = language
}

syncDocumentLanguage(i18n.language)
i18n.on('languageChanged', syncDocumentLanguage)

export function isSupportedLanguage(value: string): value is SupportedLanguage {
  return (SUPPORTED_LANGUAGES as readonly string[]).includes(value)
}

export default i18n
