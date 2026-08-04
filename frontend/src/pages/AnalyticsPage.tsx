import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, BarChart, Bar, AreaChart, Area,
  PieChart, Pie, Cell,
} from 'recharts';
import { getAnalyticsSummary, getAiStats } from '../api/endpoints';
import Sidebar from '../components/Sidebar/Sidebar';
import './AnalyticsPage.css';

const PERIOD_OPTIONS = [
  { label: '7 ngày', days: 7 },
  { label: '30 ngày', days: 30 },
  { label: '90 ngày', days: 90 },
];

const STATUS_COLORS: Record<string, string> = {
  Draft: '#f59e0b',
  Confirmed: '#6366f1',
  Preparing: '#3b82f6',
  Completed: '#10b981',
  Cancelled: '#ef4444',
};

const INTENT_COLORS = [
  '#6366f1', '#8b5cf6', '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#ec4899',
];

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type CustomTooltipProps = Record<string, any>;

function RevenueTooltip({ active, payload, label }: CustomTooltipProps) {
  if (!active || !payload?.length) return null;
  const value: number = payload[0]?.value ?? 0;
  return (
    <div style={{ background: '#16161f', border: '1px solid #1e1e2e', borderRadius: 12, padding: '10px 14px', color: '#f0f0f8', fontSize: 13 }}>
      <div style={{ color: '#9898b8', marginBottom: 4 }}>Ngày: {label}</div>
      <div>{value.toLocaleString('vi-VN')}₫</div>
    </div>
  );
}

function BarTooltip({ active, payload }: CustomTooltipProps) {
  if (!active || !payload?.length) return null;
  const qty: number = payload[0]?.value ?? 0;
  return (
    <div style={{ background: '#16161f', border: '1px solid #1e1e2e', borderRadius: 12, padding: '10px 14px', color: '#f0f0f8', fontSize: 13 }}>
      <div>{qty} phần</div>
    </div>
  );
}

function HourlyTooltip({ active, payload, label }: CustomTooltipProps) {
  if (!active || !payload?.length) return null;
  const count: number = payload[0]?.value ?? 0;
  return (
    <div style={{ background: '#16161f', border: '1px solid #1e1e2e', borderRadius: 12, padding: '10px 14px', color: '#f0f0f8', fontSize: 13 }}>
      <div style={{ color: '#9898b8', marginBottom: 4 }}>{label}:00</div>
      <div>{count} đơn</div>
    </div>
  );
}

const TOOLTIP_STYLE = {
  background: '#16161f',
  border: '1px solid #1e1e2e',
  borderRadius: 12,
  color: '#f0f0f8',
  fontSize: 13,
};

export default function AnalyticsPage() {
  const [days, setDays] = useState(30);

  const { data: summary, isLoading: loadingSummary } = useQuery({
    queryKey: ['analytics-summary', days],
    queryFn: () => getAnalyticsSummary(days),
  });

  const { data: aiStats, isLoading: loadingAi } = useQuery({
    queryKey: ['ai-stats', days],
    queryFn: () => getAiStats(days),
  });

  const isLoading = loadingSummary || loadingAi;

  const parseRate = aiStats && aiStats.totalMessages > 0
    ? Math.round((aiStats.parsedSuccessfully / aiStats.totalMessages) * 100)
    : 0;
  const maxIntentCount = aiStats?.intentDistribution.length
    ? Math.max(...aiStats.intentDistribution.map((i) => i.count))
    : 1;

  return (
    <div className="app-layout">
      <Sidebar />

      <main className="analytics">
        {/* Header */}
        <header className="analytics__header">
          <div>
            <h1 className="analytics__title">📊 Thống Kê</h1>
            <p className="analytics__subtitle">
              Doanh thu, đơn hàng &amp; AI performance
            </p>
          </div>
          <div className="analytics__period-selector">
            {PERIOD_OPTIONS.map((p) => (
              <button
                key={p.days}
                id={`period-${p.days}`}
                className={`period-btn ${days === p.days ? 'period-btn--active' : ''}`}
                onClick={() => setDays(p.days)}
              >
                {p.label}
              </button>
            ))}
          </div>
        </header>

        {isLoading ? (
          <div className="analytics__loading">
            <div className="analytics__loading-spinner" />
            Đang tải dữ liệu...
          </div>
        ) : (
          <>
            {/* KPI Cards */}
            <div className="analytics__kpis">
              <div className="kpi-card kpi-card--accent">
                <div className="kpi-card__icon">💰</div>
                <div className="kpi-card__value">
                  {(summary?.totalRevenue ?? 0).toLocaleString('vi-VN')}₫
                </div>
                <div className="kpi-card__label">Tổng doanh thu</div>
                <div className="kpi-card__detail">
                  {summary?.completedOrders ?? 0} đơn hoàn thành
                </div>
              </div>

              <div className="kpi-card kpi-card--success">
                <div className="kpi-card__icon">📦</div>
                <div className="kpi-card__value">{summary?.totalOrders ?? 0}</div>
                <div className="kpi-card__label">Tổng đơn hàng</div>
                <div className="kpi-card__detail">
                  {summary?.completedOrders ?? 0} hoàn thành / {summary?.totalOrders ?? 0} tổng
                </div>
              </div>

              <div className="kpi-card kpi-card--warning">
                <div className="kpi-card__icon">📊</div>
                <div className="kpi-card__value">
                  {(summary?.averageOrderValue ?? 0).toLocaleString('vi-VN')}₫
                </div>
                <div className="kpi-card__label">Giá trị TB / đơn</div>
              </div>

              <div className="kpi-card kpi-card--danger">
                <div className="kpi-card__icon">❌</div>
                <div className="kpi-card__value">
                  {summary && summary.totalOrders > 0
                    ? `${Math.round((summary.cancelledOrders / summary.totalOrders) * 100)}%`
                    : '0%'}
                </div>
                <div className="kpi-card__label">Tỷ lệ hủy</div>
                <div className="kpi-card__detail">
                  {summary?.cancelledOrders ?? 0} đơn bị hủy
                </div>
              </div>
            </div>

            <div className="analytics__charts">
              {/* Revenue Area Chart */}
              <div className="chart-card" style={{ animationDelay: '0.1s' }}>
                <h3 className="chart-card__title">📈 Doanh Thu Theo Ngày</h3>
                {summary?.dailyRevenue.length ? (
                  <ResponsiveContainer width="100%" height={300}>
                    <AreaChart data={summary.dailyRevenue}>
                      <defs>
                        <linearGradient id="revenueGrad" x1="0" y1="0" x2="0" y2="1">
                          <stop offset="5%" stopColor="#6366f1" stopOpacity={0.3} />
                          <stop offset="95%" stopColor="#6366f1" stopOpacity={0} />
                        </linearGradient>
                      </defs>
                      <CartesianGrid stroke="#1e1e2e" strokeDasharray="3 3" />
                      <XAxis
                        dataKey="date"
                        tick={{ fill: '#9898b8', fontSize: 11 }}
                        tickFormatter={(v: string) => v.slice(5)}
                        axisLine={{ stroke: '#1e1e2e' }}
                      />
                      <YAxis
                        tick={{ fill: '#9898b8', fontSize: 11 }}
                        tickFormatter={(v: number) => `${(v / 1000).toFixed(0)}k`}
                        axisLine={{ stroke: '#1e1e2e' }}
                      />
                      <Tooltip content={<RevenueTooltip />} />
                      <Area
                        type="monotone"
                        dataKey="revenue"
                        stroke="#6366f1"
                        strokeWidth={2}
                        fill="url(#revenueGrad)"
                      />
                    </AreaChart>
                  </ResponsiveContainer>
                ) : (
                  <div className="chart-card__empty">Chưa có dữ liệu doanh thu</div>
                )}
              </div>

              {/* Row 2: Top Món + Hourly */}
              <div className="analytics__row">
                <div className="chart-card" style={{ animationDelay: '0.2s' }}>
                  <h3 className="chart-card__title">🏆 Top Món Bán Chạy</h3>
                  {summary?.topMenuItems.length ? (
                    <ResponsiveContainer width="100%" height={250}>
                      <BarChart
                        data={summary.topMenuItems}
                        layout="vertical"
                        margin={{ left: 20 }}
                      >
                        <CartesianGrid stroke="#1e1e2e" strokeDasharray="3 3" horizontal={false} />
                        <XAxis
                          type="number"
                          tick={{ fill: '#9898b8', fontSize: 11 }}
                          axisLine={{ stroke: '#1e1e2e' }}
                        />
                        <YAxis
                          type="category"
                          dataKey="name"
                          tick={{ fill: '#f0f0f8', fontSize: 12 }}
                          width={120}
                          axisLine={{ stroke: '#1e1e2e' }}
                        />
                        <Tooltip content={<BarTooltip />} />
                        <Bar dataKey="totalQuantity" fill="#8b5cf6" radius={[0, 6, 6, 0]} />
                      </BarChart>
                    </ResponsiveContainer>
                  ) : (
                    <div className="chart-card__empty">Chưa có dữ liệu</div>
                  )}
                </div>

                <div className="chart-card" style={{ animationDelay: '0.3s' }}>
                  <h3 className="chart-card__title">⏰ Phân Bố Theo Giờ</h3>
                  <ResponsiveContainer width="100%" height={250}>
                    <AreaChart data={summary?.hourlyDistribution ?? []}>
                      <defs>
                        <linearGradient id="hourlyGrad" x1="0" y1="0" x2="0" y2="1">
                          <stop offset="5%" stopColor="#10b981" stopOpacity={0.3} />
                          <stop offset="95%" stopColor="#10b981" stopOpacity={0} />
                        </linearGradient>
                      </defs>
                      <CartesianGrid stroke="#1e1e2e" strokeDasharray="3 3" />
                      <XAxis
                        dataKey="hour"
                        tick={{ fill: '#9898b8', fontSize: 11 }}
                        tickFormatter={(v: number) => `${v}h`}
                        axisLine={{ stroke: '#1e1e2e' }}
                      />
                      <YAxis
                        tick={{ fill: '#9898b8', fontSize: 11 }}
                        axisLine={{ stroke: '#1e1e2e' }}
                      />
                      <Tooltip content={<HourlyTooltip />} />
                      <Area
                        type="monotone"
                        dataKey="orderCount"
                        stroke="#10b981"
                        strokeWidth={2}
                        fill="url(#hourlyGrad)"
                      />
                    </AreaChart>
                  </ResponsiveContainer>
                </div>
              </div>

              {/* Row 3: Status Pie + AI Performance */}
              <div className="analytics__row">
                <div className="chart-card" style={{ animationDelay: '0.4s' }}>
                  <h3 className="chart-card__title">🎯 Trạng Thái Đơn Hàng</h3>
                  {summary?.statusBreakdown.length ? (
                    <ResponsiveContainer width="100%" height={280}>
                      <PieChart>
                        <Pie
                          data={summary.statusBreakdown}
                          dataKey="count"
                          nameKey="status"
                          cx="50%"
                          cy="50%"
                          innerRadius={60}
                          outerRadius={100}
                          paddingAngle={3}
                          strokeWidth={0}
                          label={({ name, value }) => `${name} (${value})`}
                        >
                          {summary.statusBreakdown.map((entry) => (
                            <Cell
                              key={entry.status}
                              fill={STATUS_COLORS[entry.status] ?? '#5a5a78'}
                            />
                          ))}
                        </Pie>
                        <Tooltip contentStyle={TOOLTIP_STYLE} />
                      </PieChart>
                    </ResponsiveContainer>
                  ) : (
                    <div className="chart-card__empty">Chưa có dữ liệu</div>
                  )}
                </div>

                <div className="chart-card" style={{ animationDelay: '0.5s' }}>
                  <h3 className="chart-card__title">🤖 AI Performance</h3>
                  <div className="ai-perf">
                    {/* Circular progress — Parse success rate */}
                    <div className="ai-perf__metric">
                      <div className="ai-perf__circle">
                        <svg width="64" height="64" viewBox="0 0 64 64">
                          <circle
                            cx="32" cy="32" r="26"
                            fill="none"
                            stroke="#1e1e2e"
                            strokeWidth="6"
                          />
                          <circle
                            cx="32" cy="32" r="26"
                            fill="none"
                            stroke="#10b981"
                            strokeWidth="6"
                            strokeDasharray={`${(parseRate / 100) * 163.36} 163.36`}
                            strokeLinecap="round"
                          />
                        </svg>
                        <span className="ai-perf__circle-text">{parseRate}%</span>
                      </div>
                      <div>
                        <div className="ai-perf__value">
                          {aiStats?.parsedSuccessfully ?? 0} / {aiStats?.totalMessages ?? 0}
                        </div>
                        <div className="ai-perf__label">Parse thành công (≥ 0.8)</div>
                      </div>
                    </div>

                    {/* Intent Distribution bars */}
                    <div className="ai-perf__intents">
                      {aiStats?.intentDistribution.map((intent, idx) => (
                        <div key={intent.intent} className="intent-bar">
                          <span className="intent-bar__label">{intent.intent}</span>
                          <div className="intent-bar__track">
                            <div
                              className="intent-bar__fill"
                              style={{
                                width: `${(intent.count / maxIntentCount) * 100}%`,
                                background: INTENT_COLORS[idx % INTENT_COLORS.length],
                              }}
                            />
                          </div>
                          <span className="intent-bar__count">{intent.count}</span>
                        </div>
                      ))}
                    </div>

                    {/* Complaints badge */}
                    {(aiStats?.complaintsNeedingAttention ?? 0) > 0 && (
                      <div className="ai-perf__complaints-badge">
                        ⚠️ {aiStats?.complaintsNeedingAttention} khiếu nại cần xử lý
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>
          </>
        )}
      </main>
    </div>
  );
}
