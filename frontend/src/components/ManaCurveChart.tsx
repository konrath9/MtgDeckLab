import { Bar, BarChart, CartesianGrid, Cell, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { ManaCurve } from '../api/types'
import { CHART, ChartTooltip, axisTick } from './chartTheme'

const BAR_COLOR = '#3f6fd1'
const PEAK_COLOR = '#5e8ee8'

export function ManaCurveChart({ manaCurve }: { manaCurve: ManaCurve }) {
  const data = Object.entries(manaCurve.distribution)
    .map(([cmc, count]) => ({ cmc: Number(cmc), count }))
    .sort((a, b) => a.cmc - b.cmc)

  if (data.length === 0) {
    return <p className="text-sm text-muted">No non-land cards to chart yet.</p>
  }

  return (
    <div>
      <div className="mb-3 flex flex-wrap gap-x-6 gap-y-1 text-xs text-muted">
        <span>
          Average CMC <strong className="ml-1 text-sm text-fg tabular-nums">{manaCurve.averageCmc.toFixed(2)}</strong>
        </span>
        <span>
          Peak <strong className="ml-1 text-sm text-fg tabular-nums">{manaCurve.peakCmc}</strong>
        </span>
        <span>
          Non-land cards <strong className="ml-1 text-sm text-fg tabular-nums">{manaCurve.totalNonLandCards}</strong>
        </span>
      </div>
      <ResponsiveContainer width="100%" height={200}>
        <BarChart data={data} margin={{ top: 16, right: 4, left: -20, bottom: 0 }}>
          <defs>
            <linearGradient id="manaCurveBar" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={BAR_COLOR} stopOpacity={1} />
              <stop offset="100%" stopColor={BAR_COLOR} stopOpacity={0.55} />
            </linearGradient>
            <linearGradient id="manaCurvePeak" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={PEAK_COLOR} stopOpacity={1} />
              <stop offset="100%" stopColor={PEAK_COLOR} stopOpacity={0.55} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="2 4" stroke={CHART.grid} vertical={false} />
          <XAxis
            dataKey="cmc"
            tickFormatter={(v) => (v === 7 ? '7+' : String(v))}
            stroke={CHART.axis}
            tickLine={false}
            tick={axisTick}
          />
          <YAxis allowDecimals={false} stroke={CHART.axis} tickLine={false} axisLine={false} tick={axisTick} />
          <Tooltip
            cursor={{ fill: 'rgba(255,255,255,0.04)' }}
            content={<ChartTooltip labelFormatter={(v) => `Mana value ${v === 7 ? '7+' : v}`} />}
          />
          <Bar dataKey="count" name="Cards" radius={[5, 5, 0, 0]} maxBarSize={38}>
            <LabelList dataKey="count" position="top" fill={CHART.tick} fontSize={10} />
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
