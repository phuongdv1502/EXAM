const palette = ['#10b981', '#34d399', '#059669', '#f59e0b', '#f97316', '#6366f1', '#94a3b8', '#ef4444'];

export const doughnutOptions = {
  plugins: { legend: { position: 'bottom', labels: { boxWidth: 12, usePointStyle: true } } },
  maintainAspectRatio: false,
  cutout: '62%',
};

export const barOptions = {
  plugins: { legend: { display: false } },
  maintainAspectRatio: false,
  scales: {
    x: { grid: { display: false }, ticks: { precision: 0, maxRotation: 0, autoSkip: false, font: { size: 11 } } },
    y: { beginAtZero: true, ticks: { precision: 0 } },
  },
};

export const horizontalBarOptions = {
  indexAxis: 'y' as const,
  plugins: { legend: { display: false } },
  maintainAspectRatio: false,
  scales: {
    x: { beginAtZero: true, ticks: { precision: 0 } },
    y: { grid: { display: false }, ticks: { font: { size: 12 } } },
  },
};

export function doughnutChart(labels: string[], values: number[]) {
  return {
    labels,
    datasets: [{ data: values, backgroundColor: palette.slice(0, values.length), borderWidth: 0 }],
  };
}

export function barChart(labels: string[], values: number[]) {
  return {
    labels,
    datasets: [{ data: values, backgroundColor: '#10b981', borderRadius: 8, maxBarThickness: 28 }],
  };
}

export function hasCount(items: { count: number }[]): boolean {
  return items.some((item) => item.count > 0);
}
