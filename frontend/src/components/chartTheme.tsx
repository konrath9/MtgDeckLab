// Shared chart styling so every chart in the app reads as one system: same axis/grid treatment,
// same tooltip, same domain colors. Values mirror the design tokens in index.css (recharts needs
// concrete colors, it can't consume CSS custom properties for SVG fills).
export const CHART = {
  grid: 'rgba(255,255,255,0.06)',
  axis: 'rgba(255,255,255,0.12)',
  tick: '#92929a',
  label: '#f5f5f5',
  surface: '#161618',
  border: 'rgba(255,255,255,0.08)',
  accent: '#5e6ad2',
  neutralBar: '#3f3f46',
} as const

// Magic's colors are domain data, not decoration — same hexes used wherever color identity
// shows up, tuned for legibility on the dark surface (true black/white wouldn't read).
export const MANA_COLORS = {
  White: '#e8e4c9',
  Blue: '#3b82d6',
  Black: '#9a8fd0',
  Red: '#e05a56',
  Green: '#3aa578',
  Colorless: '#8b8b93',
} as const

export const axisTick = { fill: CHART.tick, fontSize: 11 }

/** Ordered WUBRG stops, for anything that has to represent "many colors at once". */
export const RAINBOW_STOPS = [
  MANA_COLORS.White,
  MANA_COLORS.Blue,
  MANA_COLORS.Black,
  MANA_COLORS.Red,
  MANA_COLORS.Green,
] as const

type TooltipEntry = { name?: string | number; value?: number | string; color?: string }

/** Recharts tooltip content styled with the app's tokens instead of the library default. */
export function ChartTooltip({
  active,
  payload,
  label,
  labelFormatter,
}: {
  active?: boolean
  payload?: TooltipEntry[]
  label?: string | number
  labelFormatter?: (label: string | number) => string
}) {
  if (!active || !payload?.length) return null

  const rows = payload.filter((p) => p.value !== undefined && p.value !== null && p.value !== 0)
  if (rows.length === 0) return null

  return (
    <div className="rounded-md border border-border bg-surface px-2.5 py-1.5 text-xs shadow-sm">
      {label !== undefined && (
        <div className="mb-1 font-medium text-fg">
          {labelFormatter ? labelFormatter(label) : label}
        </div>
      )}
      {rows.map((row, i) => (
        <div key={i} className="flex items-center gap-2 text-muted">
          {row.color && (
            <span className="h-2 w-2 shrink-0 rounded-full" style={{ background: row.color }} />
          )}
          <span className="text-fg">{row.name}</span>
          <span className="ml-auto tabular-nums">{row.value}</span>
        </div>
      ))}
    </div>
  )
}
