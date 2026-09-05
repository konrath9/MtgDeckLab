import { Bar, BarChart, CartesianGrid, Cell, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { Trans, useTranslation } from 'react-i18next'
import type { Color, ColorDistribution } from '../api/types'
import { CHART, ChartTooltip, MANA_COLORS, RAINBOW_STOPS, axisTick } from './chartTheme'

const COLOR_ORDER: Color[] = ['White', 'Blue', 'Black', 'Red', 'Green', 'Colorless']
const MULTICOLOR_GRADIENT_ID = 'colorDist-multicolor'

// `key` é o valor da API (estável, usado para cor/gradiente); `label` é o texto traduzido que
// aparece no eixo.
type Row = { key: string; label: string; count: number; fill: string; gradientId: string; isMulticolor?: boolean }

export function ColorDistributionChart({ colorDistribution }: { colorDistribution: ColorDistribution }) {
  const { t } = useTranslation()

  const colorRows: Row[] = COLOR_ORDER.map((color) => ({
    key: color,
    label: t(`colors.${color}`),
    count: colorDistribution.cardCount[color] ?? 0,
    fill: MANA_COLORS[color],
    gradientId: `colorDist-${color}`,
  })).filter((r) => r.count > 0)

  const rows: Row[] =
    colorDistribution.multicolorCount > 0
      ? [
          ...colorRows,
          {
            key: 'Multicolor',
            label: t('colors.Multicolor'),
            count: colorDistribution.multicolorCount,
            // Not one of the five colors — the bar itself spans all of them instead.
            fill: MANA_COLORS.Colorless,
            gradientId: MULTICOLOR_GRADIENT_ID,
            isMulticolor: true,
          },
        ]
      : colorRows

  if (colorDistribution.isColorless || rows.length === 0) {
    return <p className="text-sm text-muted">{t('analysis.charts.emptyColors')}</p>
  }

  return (
    <div>
      <ResponsiveContainer width="100%" height={Math.max(rows.length * 32 + 28, 140)}>
        <BarChart data={rows} layout="vertical" margin={{ top: 4, right: 28, left: 4, bottom: 0 }}>
          <defs>
            {rows
              .filter((row) => !row.isMulticolor)
              .map((row) => (
                <linearGradient key={row.gradientId} id={row.gradientId} x1="0" y1="0" x2="1" y2="0">
                  <stop offset="0%" stopColor={row.fill} stopOpacity={0.55} />
                  <stop offset="100%" stopColor={row.fill} stopOpacity={1} />
                </linearGradient>
              ))}
            {/* Multicolor spans WUBRG, keeping the same base-to-tip fade as every other bar. */}
            <linearGradient id={MULTICOLOR_GRADIENT_ID} x1="0" y1="0" x2="1" y2="0">
              {RAINBOW_STOPS.map((color, i) => (
                <stop
                  key={color}
                  offset={`${(i / (RAINBOW_STOPS.length - 1)) * 100}%`}
                  stopColor={color}
                  stopOpacity={0.7 + (i / (RAINBOW_STOPS.length - 1)) * 0.3}
                />
              ))}
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="2 4" stroke={CHART.grid} horizontal={false} />
          <XAxis type="number" allowDecimals={false} stroke={CHART.axis} tickLine={false} tick={axisTick} />
          <YAxis
            type="category"
            dataKey="label"
            width={78}
            stroke={CHART.axis}
            tickLine={false}
            axisLine={false}
            tick={axisTick}
          />
          <Tooltip cursor={{ fill: 'rgba(255,255,255,0.04)' }} content={<ChartTooltip />} />
          <Bar dataKey="count" name={t('analysis.charts.cards')} radius={[0, 5, 5, 0]} maxBarSize={20}>
            <LabelList dataKey="count" position="right" fill={CHART.tick} fontSize={10} />
            {rows.map((row) => (
              <Cell key={row.key} fill={`url(#${row.gradientId})`} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
      <p className="mt-2 text-xs text-muted">
        <Trans i18nKey="analysis.charts.colorFootnote" components={[<span key="multicolor" className="text-fg" />]} />
      </p>
    </div>
  )
}
