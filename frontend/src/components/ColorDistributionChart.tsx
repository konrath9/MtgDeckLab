import { Bar, BarChart, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { Color, ColorDistribution } from '../api/types'

// Cores de identidade de Magic são convenção de domínio bem estabelecida (W/U/B/R/G) — usadas
// aqui em vez da paleta categórica abstrata, mas sempre com contagem/rótulo direto no texto (não
// dependemos só da cor pra passar a informação, já que White/Colorless têm contraste baixo no dark).
const COLOR_HEX: Record<Color, string> = {
  White: '#f8f6d8',
  Blue: '#2a78d6',
  Black: '#8b7fc7',
  Red: '#e34948',
  Green: '#1baf7a',
  Colorless: '#9a9a94',
}

const COLOR_ORDER: Color[] = ['White', 'Blue', 'Black', 'Red', 'Green', 'Colorless']

export function ColorDistributionChart({ colorDistribution }: { colorDistribution: ColorDistribution }) {
  const data = COLOR_ORDER
    .map((color) => ({ color, count: colorDistribution.cardCount[color] ?? 0 }))
    .filter((d) => d.count > 0)

  if (colorDistribution.isColorless || data.length === 0) {
    return <p className="text-sm text-slate-400">This deck has no colored cards.</p>
  }

  return (
    <ResponsiveContainer width="100%" height={200}>
      <BarChart data={data} layout="vertical" margin={{ top: 8, right: 24, left: 8, bottom: 0 }}>
        <XAxis type="number" allowDecimals={false} stroke="#898781" tick={{ fill: '#898781', fontSize: 12 }} />
        <YAxis
          type="category"
          dataKey="color"
          width={70}
          stroke="#898781"
          tick={{ fill: '#c3c2b7', fontSize: 12 }}
        />
        <Tooltip
          contentStyle={{ background: '#1a1a19', border: '1px solid #383835', borderRadius: 8 }}
          labelStyle={{ color: '#ffffff' }}
          itemStyle={{ color: '#c3c2b7' }}
        />
        <Bar dataKey="count" radius={[0, 4, 4, 0]} maxBarSize={24} label={{ position: 'right', fill: '#c3c2b7', fontSize: 12 }}>
          {data.map((entry) => (
            <Cell key={entry.color} fill={COLOR_HEX[entry.color]} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  )
}
