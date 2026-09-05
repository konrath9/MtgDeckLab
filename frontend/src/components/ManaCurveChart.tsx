import { Bar, BarChart, CartesianGrid, Cell, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { useTranslation } from 'react-i18next'
import type { ManaCurve } from '../api/types'
import { ChartTooltip, useChartTheme } from './chartTheme'
import { useFormatters } from '../i18n/format'

export function ManaCurveChart({ manaCurve }: { manaCurve: ManaCurve }) {
  const { t } = useTranslation()
  const { twoDecimals } = useFormatters()
  const { bar, peak, grid, axis, cursor, axisTick, tick } = useChartTheme()

  const data = Object.entries(manaCurve.distribution)
    .map(([cmc, count]) => ({ cmc: Number(cmc), count }))
    .sort((a, b) => a.cmc - b.cmc)

  if (data.length === 0) {
    return <p className="text-sm text-muted">{t('analysis.charts.emptyManaCurve')}</p>
  }

  return (
    <div>
      <div className="mb-3 flex flex-wrap gap-x-6 gap-y-1 text-xs text-muted">
        <span>
          {t('analysis.charts.averageCmc')}{' '}
          <strong className="ml-1 text-sm text-fg tabular-nums">{twoDecimals.format(manaCurve.averageCmc)}</strong>
        </span>
        <span>
          {t('analysis.charts.peak')}{' '}
          <strong className="ml-1 text-sm text-fg tabular-nums">{manaCurve.peakCmc}</strong>
        </span>
        <span>
          {t('analysis.charts.nonLandCards')}{' '}
          <strong className="ml-1 text-sm text-fg tabular-nums">{manaCurve.totalNonLandCards}</strong>
        </span>
      </div>
      <ResponsiveContainer width="100%" height={200}>
        <BarChart data={data} margin={{ top: 16, right: 4, left: -20, bottom: 0 }}>
          <defs>
            <linearGradient id="manaCurveBar" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={bar} stopOpacity={1} />
              <stop offset="100%" stopColor={bar} stopOpacity={0.55} />
            </linearGradient>
            <linearGradient id="manaCurvePeak" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={peak} stopOpacity={1} />
              <stop offset="100%" stopColor={peak} stopOpacity={0.55} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="2 4" stroke={grid} vertical={false} />
          <XAxis
            dataKey="cmc"
            tickFormatter={(v) => (v === 7 ? '7+' : String(v))}
            stroke={axis}
            tickLine={false}
            tick={axisTick}
          />
          <YAxis allowDecimals={false} stroke={axis} tickLine={false} axisLine={false} tick={axisTick} />
          <Tooltip
            cursor={{ fill: cursor }}
            content={
              <ChartTooltip
                labelFormatter={(v) => t('analysis.charts.manaValue', { value: v === 7 ? '7+' : v })}
              />
            }
          />
          <Bar dataKey="count" name={t('analysis.charts.cards')} radius={[5, 5, 0, 0]} maxBarSize={38}>
            <LabelList dataKey="count" position="top" fill={tick} fontSize={10} />
            {data.map((entry) => (
              <Cell
                key={entry.cmc}
                fill={entry.cmc === manaCurve.peakCmc ? 'url(#manaCurvePeak)' : 'url(#manaCurveBar)'}
              />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}

