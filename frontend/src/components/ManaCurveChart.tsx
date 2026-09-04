import { Bar, BarChart, CartesianGrid, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { ManaCurve } from '../api/types'

const BAR_COLOR = '#3987e5'
const PEAK_COLOR = '#86b6ef'

export function ManaCurveChart({ manaCurve }: { manaCurve: ManaCurve }) {
  const data = Object.entries(manaCurve.distribution)
    .map(([cmc, count]) => ({ cmc: Number(cmc), count }))
    .sort((a, b) => a.cmc - b.cmc)

  if (data.length === 0) {
    return <p className="text-sm text-slate-400">No non-land cards to chart yet.</p>
  }

  return (
    <div>
      <div className="mb-2 flex gap-6 text-sm text-slate-300">
        <span>
          Average CMC: <strong className="text-slate-100">{manaCurve.averageCmc.toFixed(2)}</strong>
        </span>
        <span>
          Peak: <strong className="text-slate-100">{manaCurve.peakCmc}</strong>
        </span>
        <span>
          Non-land cards: <strong className="text-slate-100">{manaCurve.totalNonLandCards}</strong>
        </span>
      </div>
      <ResponsiveContainer width="100%" height={220}>
        <BarChart data={data} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#2c2c2a" vertical={false} />
          <XAxis
            dataKey="cmc"
            tickFormatter={(v) => (v === 7 ? '7+' : String(v))}
            stroke="#898781"
            tick={{ fill: '#898781', fontSize: 12 }}
          />
          <YAxis allowDecimals={false} stroke="#898781" tick={{ fill: '#898781', fontSize: 12 }} />
          <Tooltip
            contentStyle={{ background: '#1a1a19', border: '1px solid #383835', borderRadius: 8 }}
            labelFormatter={(v) => `CMC ${v === 7 ? '7+' : v}`}
            labelStyle={{ color: '#ffffff' }}
            itemStyle={{ color: '#c3c2b7' }}
          />
          <Bar dataKey="count" radius={[4, 4, 0, 0]} maxBarSize={40}>
            {data.map((entry) => (
              <Cell key={entry.cmc} fill={entry.cmc === manaCurve.peakCmc ? PEAK_COLOR : BAR_COLOR} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
