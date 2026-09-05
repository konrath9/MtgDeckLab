// Renders a Magic mana cost (e.g. "{2}{U}{U}") as colored pips using the mana-font icon font
// (https://mana.andrewgioia.com, npm package "mana-font", CSS imported once in main.tsx).
//
// Class naming: lowercase the symbol, drop slashes — "W" -> "ms-w", "W/U" -> "ms-wu",
// "2/W" -> "ms-2w", "W/P" -> "ms-wp", "W/U/P" -> "ms-wup". `ms-cost` gives the circular
// colored background matching how costs appear on a real card.
export function manaSymbolClass(symbol: string): string {
  return symbol.toLowerCase().replace(/\//g, '')
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
    <span className={`inline-flex shrink-0 items-center gap-1 text-xs ${className}`} aria-label={manaCost}>
      {faces.map((symbols, faceIndex) => (
        <span key={faceIndex} className="inline-flex items-center gap-0.5">
          {faceIndex > 0 && (
            <span className="mx-0.5 text-muted" aria-hidden="true">
              //
            </span>
          )}
          {symbols.map((s, i) => (
            <i key={i} className={`ms ms-cost ms-${manaSymbolClass(s)}`} aria-hidden="true" />
          ))}
        </span>
      ))}
    </span>
  )
}
