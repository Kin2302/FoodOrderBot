import { useState, useRef } from 'react';
import { useMutation } from '@tanstack/react-query';
import { testAi } from '../api/endpoints';
import type { AiResponse, AiIntent } from '../types';
import './AiTestPage.css';

const DEFAULT_SHOP_ID = '00000000-0000-0000-0000-000000000001';

const INTENT_COLORS: Record<AiIntent, string> = {
  PlaceOrder: '#22c55e',
  AskMenu: '#3b82f6',
  AskOrderStatus: '#f59e0b',
  Greeting: '#a78bfa',
  Complaint: '#ef4444',
  Compliment: '#10b981',
  Other: '#6b7280',
};

const INTENT_ICONS: Record<AiIntent, string> = {
  PlaceOrder: '🛒',
  AskMenu: '📋',
  AskOrderStatus: '📦',
  Greeting: '👋',
  Complaint: '😤',
  Compliment: '😍',
  Other: '💬',
};

const INTENT_LABELS: Record<AiIntent, string> = {
  PlaceOrder: 'Đặt hàng',
  AskMenu: 'Hỏi menu',
  AskOrderStatus: 'Hỏi đơn',
  Greeting: 'Chào hỏi',
  Complaint: 'Khiếu nại',
  Compliment: 'Khen ngợi',
  Other: 'Khác',
};

const SENTIMENT_ICONS = { positive: '🟢', neutral: '🟡', negative: '🔴' };

interface ChatEntry {
  id: string;
  role: 'user' | 'ai';
  text: string;
  response?: AiResponse;
}

export default function AiTestPage() {
  const shopId = DEFAULT_SHOP_ID;
  const [inputText, setInputText] = useState('');
  const [sessionId] = useState(() => `test-${Date.now()}`);
  const [chat, setChat] = useState<ChatEntry[]>([]);
  const bottomRef = useRef<HTMLDivElement>(null);

  const mutation = useMutation({
    mutationFn: (text: string) =>
      testAi({ text, shopId, fbSenderId: sessionId }),
    onSuccess: (res, text) => {
      setChat((prev) => [
        ...prev,
        { id: `u-${Date.now()}`, role: 'user', text },
        { id: `a-${Date.now()}`, role: 'ai', text: res.replyText, response: res },
      ]);
      setTimeout(() => bottomRef.current?.scrollIntoView({ behavior: 'smooth' }), 50);
    },
  });

  const handleSend = () => {
    const text = inputText.trim();
    if (!text || mutation.isPending) return;
    setInputText('');
    mutation.mutate(text);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const quickTests = [
    'chào shop!',
    'có gì ngon không shop?',
    '2 phở tái + 1 bún bò, giao Q3 sđt 0901234567',
    'đơn tui tới đâu rồi?',
    'sao giao chậm vậy, tui đợi 1 tiếng rồi!',
    'ngon lắm shop ơi 😍',
  ];

  return (
    <div className="ai-test-page">
      {/* Header */}
      <div className="ai-test-header">
        <div className="ai-test-header-left">
          <div className="ai-test-icon">🤖</div>
          <div>
            <h1>AI Test Console</h1>
            <p>Test AI pipeline — phân tích intent, parse đơn, sentiment</p>
          </div>
        </div>
        <div className="ai-test-session">
          <span className="session-badge">Session: {sessionId.slice(-8)}</span>
          <span className="model-badge">8b + 70b Multi-Model</span>
        </div>
      </div>

      <div className="ai-test-layout">
        {/* Chat Area */}
        <div className="ai-chat-section">
          <div className="ai-chat-messages">
            {chat.length === 0 && (
              <div className="ai-chat-empty">
                <div className="empty-icon">💬</div>
                <p>Gõ tin nhắn bên dưới để test AI pipeline</p>
                <p className="empty-hint">Tin nhắn trong cùng session sẽ có conversation context</p>
              </div>
            )}

            {chat.map((entry) => (
              <div key={entry.id} className={`chat-entry ${entry.role}`}>
                {entry.role === 'user' ? (
                  <div className="chat-bubble user-bubble">
                    <span className="chat-avatar">👤</span>
                    <div className="bubble-content">{entry.text}</div>
                  </div>
                ) : (
                  <div className="chat-ai-block">
                    <div className="chat-bubble ai-bubble">
                      <span className="chat-avatar">🤖</span>
                      <div className="bubble-content">{entry.text}</div>
                    </div>

                    {entry.response && (
                      <AiResultCard response={entry.response} />
                    )}
                  </div>
                )}
              </div>
            ))}

            {mutation.isPending && (
              <div className="chat-entry ai">
                <div className="chat-bubble ai-bubble">
                  <span className="chat-avatar">🤖</span>
                  <div className="bubble-content typing">
                    <span /><span /><span />
                  </div>
                </div>
              </div>
            )}

            <div ref={bottomRef} />
          </div>

          {/* Quick Test Buttons */}
          <div className="quick-tests">
            <span className="quick-label">Quick test:</span>
            {quickTests.map((t) => (
              <button
                key={t}
                className="quick-btn"
                onClick={() => { setInputText(t); }}
                disabled={mutation.isPending}
              >
                {t.length > 30 ? t.slice(0, 30) + '…' : t}
              </button>
            ))}
          </div>

          {/* Input */}
          <div className="ai-input-bar">
            <textarea
              className="ai-input"
              value={inputText}
              onChange={(e) => setInputText(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Nhập tin nhắn để test AI... (Enter để gửi)"
              rows={2}
              disabled={mutation.isPending}
            />
            <button
              className="ai-send-btn"
              onClick={handleSend}
              disabled={mutation.isPending || !inputText.trim()}
            >
              {mutation.isPending ? '⏳' : '➤'}
            </button>
          </div>
        </div>

        {/* Stats Panel */}
        <div className="ai-stats-panel">
          <h3>📊 Session Stats</h3>
          <div className="stat-grid">
            <StatItem label="Tin nhắn" value={chat.filter((c) => c.role === 'user').length} />
            <StatItem
              label="PlaceOrder"
              value={chat.filter((c) => c.response?.intent === 'PlaceOrder').length}
              color="#22c55e"
            />
            <StatItem
              label="Complaints"
              value={chat.filter((c) => c.response?.intent === 'Complaint').length}
              color="#ef4444"
            />
            <StatItem
              label="Cần chú ý"
              value={chat.filter((c) => c.response?.sentiment?.needsAttention).length}
              color="#f59e0b"
            />
          </div>

          <h3 style={{ marginTop: '1.5rem' }}>🗺️ Intent Map</h3>
          <div className="intent-breakdown">
            {(Object.keys(INTENT_LABELS) as AiIntent[]).map((intent) => {
              const count = chat.filter((c) => c.response?.intent === intent).length;
              return (
                <div key={intent} className="intent-row">
                  <span className="intent-icon">{INTENT_ICONS[intent]}</span>
                  <span className="intent-name">{INTENT_LABELS[intent]}</span>
                  <span
                    className="intent-count"
                    style={{ color: count > 0 ? INTENT_COLORS[intent] : 'var(--text-muted)' }}
                  >
                    {count}
                  </span>
                </div>
              );
            })}
          </div>

          {chat.length > 0 && (
            <button
              className="clear-btn"
              onClick={() => setChat([])}
            >
              🗑️ Xoá lịch sử
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

// ─── Sub-components ───────────────────────────────────────────────────────────

function AiResultCard({ response }: { response: AiResponse }) {
  const [expanded, setExpanded] = useState(false);

  return (
    <div className="ai-result-card">
      {/* Intent + Confidence */}
      <div className="result-header">
        <span
          className="intent-tag"
          style={{ background: INTENT_COLORS[response.intent] + '22', color: INTENT_COLORS[response.intent] }}
        >
          {INTENT_ICONS[response.intent]} {INTENT_LABELS[response.intent]}
        </span>

        <div className="confidence-bar-wrap">
          <div
            className="confidence-bar"
            style={{ width: `${response.confidence * 100}%`, background: INTENT_COLORS[response.intent] }}
          />
        </div>
        <span className="confidence-text">{(response.confidence * 100).toFixed(0)}%</span>

        {response.sentiment && (
          <span
            className="sentiment-tag"
            title={`Score: ${(response.sentiment.score * 100).toFixed(0)}%`}
          >
            {SENTIMENT_ICONS[response.sentiment.label]}
            {response.sentiment.needsAttention && (
              <span className="attention-badge">⚠️ Cần chú ý</span>
            )}
          </span>
        )}

        <button className="expand-btn" onClick={() => setExpanded(!expanded)}>
          {expanded ? '▲ Thu gọn' : '▼ Chi tiết'}
        </button>
      </div>

      {/* Expanded Detail */}
      {expanded && (
        <div className="result-detail">
          {/* Parse Result */}
          {response.parseResult && (
            <div className="detail-section">
              <h4>📦 Parse Result</h4>
              <div className="parse-items">
                {response.parseResult.items.map((item, i) => (
                  <div key={i} className="parse-item">
                    <span className="item-qty">×{item.quantity}</span>
                    <span className="item-name">{item.name}</span>
                    {item.note && <span className="item-note">{item.note}</span>}
                  </div>
                ))}
              </div>
              {response.parseResult.deliveryAddress && (
                <p className="parse-meta">📍 {response.parseResult.deliveryAddress}</p>
              )}
              {response.parseResult.receiverPhone && (
                <p className="parse-meta">📞 {response.parseResult.receiverPhone}</p>
              )}
              {response.parseResult.unclearParts.length > 0 && (
                <div className="unclear-parts">
                  ⚠️ Unclear: {response.parseResult.unclearParts.join(', ')}
                </div>
              )}
            </div>
          )}

          {/* Upsell Suggestions */}
          {response.suggestions.length > 0 && (
            <div className="detail-section">
              <h4>💡 Upsell Suggestions</h4>
              {response.suggestions.map((s, i) => (
                <div key={i} className="upsell-item">
                  <span className="upsell-name">{s.itemName}</span>
                  <span className="upsell-price">{s.price.toLocaleString()}đ</span>
                  <span className="upsell-reason">{s.reason}</span>
                </div>
              ))}
            </div>
          )}

          {/* Sentiment Detail */}
          {response.sentiment && (
            <div className="detail-section">
              <h4>💭 Sentiment</h4>
              <p>
                {SENTIMENT_ICONS[response.sentiment.label]} {response.sentiment.label} —
                {(response.sentiment.score * 100).toFixed(0)}% confidence
                {response.sentiment.needsAttention && ' — ⚠️ Cần chủ quán xử lý!'}
              </p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function StatItem({ label, value, color }: { label: string; value: number; color?: string }) {
  return (
    <div className="stat-item">
      <span className="stat-value" style={{ color: color || 'var(--text-primary)' }}>{value}</span>
      <span className="stat-label">{label}</span>
    </div>
  );
}
