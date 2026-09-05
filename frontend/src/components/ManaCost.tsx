// Renders a Magic mana cost (e.g. "{2}{U}{U}") using Scryfall's own card-symbol SVGs
// (https://svgs.scryfall.io/card-symbols/<SYMBOL>.svg) — Scryfall is this project's single
// source of truth for MTG data and assets, icons included; no third-party icon font or custom
// CSS shape stands in for a symbol Scryfall already serves.
//
// Scryfall's own /symbology endpoint confirms the filename convention this builds: uppercase the
// symbol and drop slashes — "W" -> "W.svg", "W/U" -> "WU.svg", "2/W" -> "2W.svg",
// "W/P" -> "WP.svg". The SVGs already render as the familiar colored circular pips, so no extra
// styling is needed beyond sizing them.
const SCRYFALL_SYMBOL_BASE_URL = 'https://svgs.scryfall.io/card-symbols/'

export function manaSymbolCode(symbol: string): string {
  return symbol.toUpperCase().replace(/\//g, '')
}

export function parseManaSymbols(manaCost: string): string[] {
  return [...manaCost.matchAll(/\{([^}]+)\}/g)].map((m) => m[1])
}

export function ManaCost({ manaCost, className = '' }: { manaCost: string | null; className?: string }) {
  if (!manaCost) return null

  // Multi-faced cards (Adventure, split, some MDFCs) get one combined mana cost string like
  // "{4}{U} // {X}{U}{U}" — the same " // " convention used for the card's combined name.
  // Keep each face's pips grouped and separated, instead of flattening every symbol together.
  const faces = manaCost
    .split(' // ')
    .map((face) => parseManaSymbols(face))
    .filter((symbols) => symbols.length > 0)

  if (faces.length === 0) return null

  return (
    <span className={`inline-flex shrink-0 items-center gap-1 ${className}`} aria-label={manaCost}>
      {faces.map((symbols, faceIndex) => (
        <span key={faceIndex} className="inline-flex items-center gap-0.5">
          {faceIndex > 0 && (
            <span className="mx-0.5 text-muted" aria-hidden="true">
              //
            </span>
          )}
          {symbols.map((s, i) => (
            <img
              key={i}
              src={`${SCRYFALL_SYMBOL_BASE_URL}${manaSymbolCode(s)}.svg`}
              alt=""
              aria-hidden="true"
              className="h-3.5 w-3.5"
            />
          ))}
        </span>
      ))}
    </span>
  )
}
