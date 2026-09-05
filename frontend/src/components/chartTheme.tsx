import { useMemo } from 'react'
import { useTheme, type ThemeMode } from '../theme/ThemeContext'

// Shared chart styling so every chart in the app reads as one system: same axis/grid treatment,
// same tooltip, same domain colors. Values mirror the design tokens in index.css per theme —
// recharts needs concrete colors, it can't consume CSS custom properties for SVG fills, então os
// charts resolvem a paleta do tema ativo via useChartTheme() e re-renderizam ao trocar de tema.

const CHART_DARK = {
  grid: 'rgba(150, 168, 228, 0.12)',
  axis: 'rgba(160, 178, 236, 0.22)',
  cursor: 'rgba(150, 168, 228, 0.06)',
  tick: '#9aa5bd',
  label: '#eef1f8',
  surface: '#141a29',
  border: 'rgba(150, 168, 228, 0.16)',
  accent: '#7180f2',
  neutralBar: '#2b3247',
} as const

// Tema claro — paleta congelada (não deve mudar quando o escuro evoluir).
const CHART_LIGHT = {
  grid: 'rgba(17,20,30,0.06)',
  axis: 'rgba(17,20,30,0.14)',
  cursor: 'rgba(17,20,30,0.04)',
  tick: '#5d626d',
  label: '#17191f',
  surface: '#ffffff',
  border: 'rgba(17,20,30,0.1)',
  accent: '#4b56c8',
  neutralBar: '#c7cad1',
} as const

// Magic's colors are domain data, not decoration — same hexes used wherever color identity
// shows up. No tema escuro os tons sobem um pouco de luminosidade/saturação (neon suave) para
// não ficarem "sujos" sobre o fundo azul-marinho; o claro usa valores próprios e congelados.
export const MANA_COLORS = {
  White: '#f4efd2',
  Blue: '#5aa6ff',
  Black: '#ad98ee',
  Red: '#ff7370',
  Green: '#46d196',
  Colorless: '#a7afc6',
} as const

// Tema claro — mesmos valores de antes (não acompanham as mudanças do escuro).
const MANA_COLORS_LIGHT = {
  White: '#c7c2a0',
  Blue: '#3b82d6',
  Black: '#6a6190',
  Red: '#e05a56',
  Green: '#3aa578',
  Colorless: '#6c6c75',
} as const

// Chroma das barras neutras/principais (curva de mana e distribuição por tipo): azul neon mais
// vivo no escuro; o claro mantém o azul-índigo de sempre.
const BAR_DARK = { bar: '#4f8fff', peak: '#7db4ff', nonbasic: '#c2c9dc' } as const
const BAR_LIGHT = { bar: '#3a63c8', peak: '#5582dd', nonbasic: '#8a8a94' } as const

export type ResolvedChartTheme = {
  grid: string
  axis: string
  cursor: string
  tick: string
  label: string
  surface: string
  border: string
  accent: string
  neutralBar: string
  /** Preenchimento das barras de carta (curva de mana, tipos). */
  bar: string
  /** Barra destacada (pico da curva). */
  peak: string
  /** Terrenos não básicos (sem cor própria, não pode colidir com Swamp). */
  nonbasic: string
  mana: { White: string; Blue: string; Black: string; Red: string; Green: string; Colorless: string }
  axisTick: { fill: string; fontSize: number }
  /** Stops WUBRG ordenados, para o gradiente de Multicolor. */
  rainbowStops: readonly string[]
}

export function resolveChartTheme(theme: ThemeMode): ResolvedChartTheme {
  const chart = theme === 'light' ? CHART_LIGHT : CHART_DARK
  const barColors = theme === 'light' ? BAR_LIGHT : BAR_DARK
  const mana = theme === 'light' ? MANA_COLORS_LIGHT : MANA_COLORS
  const rainbowStops = [mana.White, mana.Blue, mana.Black, mana.Red, mana.Green] as const

  return {
    grid: chart.grid,
    axis: chart.axis,
    cursor: chart.cursor,
    tick: chart.tick,
    label: chart.label,
    surface: chart.surface,
    border: chart.border,
    accent: chart.accent,
    neutralBar: chart.neutralBar,
    bar: barColors.bar,
    peak: barColors.peak,
    nonbasic: barColors.nonbasic,
    mana: { ...mana },
    axisTick: { fill: chart.tick, fontSize: 11 },
    rainbowStops,
  }
}

export function useChartTheme(): ResolvedChartTheme {
  const { theme } = useTheme()
  return useMemo(() => resolveChartTheme(theme), [theme])
}

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

