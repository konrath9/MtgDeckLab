import { Bar, BarChart, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { TypeDistribution } from '../api/types'

// Paleta categórica validada (ver skill dataviz/references/palette.md) — ordem fixa, nunca ciclada.
const SERIES_COLORS = ['#3987e5', '#d95926', '#199e70', '#c98500', '#d55181', '#9085e9', '#e66767']

export function TypeDistributionChart({ typeDistribution }: { typeDistribution: TypeDistribution }) {
  const data = [
    { type: 'Creatures', count: typeDistribution.creatures },
    { type: 'Instants', count: typeDistribution.instants },
    { type: 'Sorceries', count: typeDistribution.sorceries },
    { type: 'Artifacts', count: typeDistribution.artifacts },
    { type: 'Enchantments', count: typeDistribution.enchantments },
    { type: 'Lands', count: typeDistribution.lands },
    { type: 'Planeswalkers', count: typeDistribution.planeswalkers },
    { type: 'Other', count: typeDistribution.other },
  ].filter((d) => d.count > 0)

  if (data.length === 0) {
    return <p className="text-sm text-slate-400">No cards to chart yet.</p>
  }

  return (
    <ResponsiveContainer width="100%" height={240}>
      <BarChart data={data} layout="vertical" margin={{ top: 8, right: 24, left: 8, bottom: 0 }}>
        <XAxis type="number" allowDecimals={false} stroke="#898781" tick={{ fill: '#898781', fontSize: 12 }} />
        <YAxis
          type="category"
          dataKey="type"
          width={90}
          stroke="#898781"
          tick={{ fill: '#c3c2b7', fontSize: 12 }}
        />
        <Tooltip
          contentStyle={{ background: '#1a1a19', border: '1px solid #383835', borderRadius: 8 }}
          labelStyle={{ color: '#ffffff' }}
          itemStyle={{ color: '#c3c2b7' }}
        />
        <Bar dataKey="count" radius={[0, 4, 4, 0]} maxBarSize={20} label={{ position: 'right', fill: '#c3c2b7', fontSize: 12 }}>
          {data.map((entry, i) => (
            <Cell key={entry.type} fill={SERIES_COLORS[i % SERIES_COLORS.length]} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  )
}
