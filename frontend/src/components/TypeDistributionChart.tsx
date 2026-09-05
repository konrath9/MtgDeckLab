import { Bar, BarChart, CartesianGrid, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { useTranslation } from 'react-i18next'
import type { TypeDistribution } from '../api/types'
import { CHART, ChartTooltip, MANA_COLORS, axisTick } from './chartTheme'

const TYPE_COLOR = '#3f6fd1'

// The Lands row is split by basic-land color (plus nonbasic), so a mana base reads at a glance
// instead of collapsing into one number. Order matters: it's the stacking order, left to right.
// Estas chaves são as da API (landBreakdown) — o rótulo traduzido entra só na exibição.
const LAND_BUCKETS = ['Plains', 'Island', 'Swamp', 'Mountain', 'Forest', 'Colorless', 'Nonbasic'] as const
const LAND_BUCKET_COLORS: Record<string, string> = {
  Plains: MANA_COLORS.White,
  Island: MANA_COLORS.Blue,
  Swamp: MANA_COLORS.Black,
  Mountain: MANA_COLORS.Red,
  Forest: MANA_COLORS.Green,
  Colorless: MANA_COLORS.Colorless,
  // Light neutral: nonbasics have no color of their own, and it must not read as "black"
  // (which would collide with Swamp) — lightness is what separates it from Colorless.
  Nonbasic: '#b4b4bd',
}

type Row = {
  type: string
  count?: number
  total: number
  // Land buckets are added dynamically as extra numeric keys for the stacked segments.
  [bucket: string]: string | number | undefined
}

export function TypeDistributionChart({ typeDistribution }: { typeDistribution: TypeDistribution }) {
  const { t } = useTranslation()

  const landBreakdown = typeDistribution.landBreakdown ?? {}
  const activeLandBuckets = LAND_BUCKETS.filter((b) => (landBreakdown[b] ?? 0) > 0)

  const rows: Row[] = [
    { type: t('cardTypes.Creature'), count: typeDistribution.creatures, total: typeDistribution.creatures },
    {
      type: t('cardTypes.Planeswalker'),
      count: typeDistribution.planeswalkers,
      total: typeDistribution.planeswalkers,
    },
    { type: t('cardTypes.Instant'), count: typeDistribution.instants, total: typeDistribution.instants },
    { type: t('cardTypes.Sorcery'), count: typeDistribution.sorceries, total: typeDistribution.sorceries },
    { type: t('cardTypes.Artifact'), count: typeDistribution.artifacts, total: typeDistribution.artifacts },
    {
      type: t('cardTypes.Enchantment'),
      count: typeDistribution.enchantments,
      total: typeDistribution.enchantments,
    },
    { type: t('cardTypes.Other'), count: typeDistribution.other, total: typeDistribution.other },
    {
      type: t('cardTypes.Land'),
      total: typeDistribution.lands,
      // Only non-zero buckets are set, so empty ones don't show up in the tooltip.
      ...Object.fromEntries(activeLandBuckets.map((b) => [b, landBreakdown[b]])),
    },
  ].filter((r) => r.total > 0)

  if (rows.length === 0) {
    return <p className="text-sm text-muted">{t('analysis.charts.emptyTypes')}</p>
  }

  const lastBucket = activeLandBuckets[activeLandBuckets.length - 1]

  return (
    <div>
      <ResponsiveContainer width="100%" height={Math.max(rows.length * 32 + 28, 160)}>
        <BarChart data={rows} layout="vertical" margin={{ top: 4, right: 28, left: 4, bottom: 0 }}>
          <defs>
            <linearGradient id="typeDistBar" x1="0" y1="0" x2="1" y2="0">
              <stop offset="0%" stopColor={TYPE_COLOR} stopOpacity={0.55} />
              <stop offset="100%" stopColor={TYPE_COLOR} stopOpacity={1} />
            </linearGradient>
            {activeLandBuckets.map((bucket) => (
              <linearGradient key={bucket} id={`typeDist-${bucket}`} x1="0" y1="0" x2="1" y2="0">
                <stop offset="0%" stopColor={LAND_BUCKET_COLORS[bucket]} stopOpacity={0.55} />
                <stop offset="100%" stopColor={LAND_BUCKET_COLORS[bucket]} stopOpacity={1} />
              </linearGradient>
            ))}
          </defs>
          <CartesianGrid strokeDasharray="2 4" stroke={CHART.grid} horizontal={false} />
          <XAxis type="number" allowDecimals={false} stroke={CHART.axis} tickLine={false} tick={axisTick} />
          <YAxis
            type="category"
            dataKey="type"
            width={92}
            stroke={CHART.axis}
            tickLine={false}
            axisLine={false}
            tick={axisTick}
          />
          <Tooltip cursor={{ fill: 'rgba(255,255,255,0.04)' }} content={<ChartTooltip />} />

          <Bar
            dataKey="count"
            name={t('analysis.charts.cards')}
            stackId="a"
            fill="url(#typeDistBar)"
            radius={[0, 5, 5, 0]}
            maxBarSize={20}
          >
            <LabelList dataKey="count" position="right" fill={CHART.tick} fontSize={10} />
          </Bar>

          {activeLandBuckets.map((bucket) => (
            <Bar
              key={bucket}
              dataKey={bucket}
              name={t(`lands.${bucket}`)}
              stackId="a"
              fill={`url(#typeDist-${bucket})`}
              maxBarSize={20}
              radius={bucket === lastBucket ? [0, 5, 5, 0] : undefined}
            >
              {bucket === lastBucket && (
                <LabelList dataKey="total" position="right" fill={CHART.tick} fontSize={10} />
              )}
            </Bar>
          ))}
        </BarChart>
      </ResponsiveContainer>
      {activeLandBuckets.length > 0 && (
        <p className="mt-2 text-xs text-muted">{t('analysis.charts.landsFootnote')}</p>
      )}
    </div>
  )
}
