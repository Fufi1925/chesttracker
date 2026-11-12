"use client";
export function Chart({
  points,
  cumulative = false,
}: {
  points: { label: string; value: number }[];
  cumulative?: boolean;
}) {
  const max = Math.max(5, ...points.map((p) => p.value)),
    top = Math.ceil(max / 5) * 5;
  const x = (i: number) => 48 + i * (650 / Math.max(1, points.length - 1)),
    y = (v: number) => 190 - (v / top) * 155;
  const path = points
    .map((p, i) => `${i ? "L" : "M"}${x(i)},${y(p.value)}`)
    .join(" ");
  if (!points.length)
    return (
      <div className="chart-empty">
        <span>
          Noch keine{" "}
          {cumulative ? "Truhenereignisse" : "abgeschlossenen Matches"}
        </span>
        <small>
          {cumulative
            ? "Bestätigte Öffnungen erscheinen hier als zeitlicher Verlauf."
            : "Dein erster erfasster Match erscheint hier."}
        </small>
      </div>
    );
  return (
    <svg
      viewBox="0 0 730 230"
      className="chart"
      role="img"
      aria-label={
        cumulative ? "Kumulierte Truhen im Match" : "Truhen pro Match"
      }
    >
      <defs>
        <linearGradient id="area" x1="0" y1="0" x2="0" y2="1">
          <stop stopColor="#b4bccb" stopOpacity=".18" />
          <stop offset="1" stopColor="#b4bccb" stopOpacity="0" />
        </linearGradient>
      </defs>
      {[0, 1, 2, 3, 4].map((i) => (
        <g key={i}>
          <line
            x1="48"
            x2="704"
            y1={y((top * i) / 4)}
            y2={y((top * i) / 4)}
            stroke="#282b31"
          />
          <text
            x="32"
            y={y((top * i) / 4) + 4}
            textAnchor="end"
            fill="#7e8490"
            fontSize="11"
          >
            {Math.round((top * i) / 4)}
          </text>
        </g>
      ))}
      <path
        d={`${path} L${x(points.length - 1)},190 L48,190Z`}
        fill="url(#area)"
      />
      <path
        d={path}
        fill="none"
        stroke="#d6dbe5"
        strokeWidth="2"
        strokeLinejoin="round"
      />
      {points.map((p, i) => (
        <g key={i}>
          <circle
            cx={x(i)}
            cy={y(p.value)}
            r="4"
            fill="#e5e9f0"
            stroke="#14161a"
            strokeWidth="2"
          >
            <title>
              {p.label}: {p.value} Truhen
            </title>
          </circle>
          {(points.length <= 12 || i % Math.ceil(points.length / 10) === 0) && (
            <text
              x={x(i)}
              y="217"
              textAnchor="middle"
              fill="#7e8490"
              fontSize="10"
            >
              {p.label}
            </text>
          )}
        </g>
      ))}
    </svg>
  );
}
