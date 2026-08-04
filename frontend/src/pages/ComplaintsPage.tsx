import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getComplaints, getConversationHistory } from '../api/endpoints';
import Sidebar from '../components/Sidebar/Sidebar';
import type { Complaint, ConversationHistory } from '../types';
import './ComplaintsPage.css';

function formatTime(iso: string) {
  const d = new Date(iso);
  const now = new Date();
  const diff = now.getTime() - d.getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 60) return `${mins} phút trước`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours} giờ trước`;
  const days = Math.floor(hours / 24);
  if (days < 7) return `${days} ngày trước`;
  return d.toLocaleDateString('vi-VN');
}

function formatChatTime(iso: string) {
  return new Date(iso).toLocaleString('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
    day: '2-digit',
    month: '2-digit',
  });
}

function SentimentBadge({ label, score }: { label: string | null; score: number | null }) {
  if (!label) return null;
  const emoji = label === 'negative' ? '🔴' : label === 'neutral' ? '🟡' : '🟢';
  return (
    <span className={`sentiment-badge sentiment-badge--${label}`}>
      {emoji} {label} {score !== null ? `(${(score * 100).toFixed(0)}%)` : ''}
    </span>
  );
}

function ConversationDrawer({
  senderId,
  onClose,
}: {
  senderId: string;
  onClose: () => void;
}) {
  const { data, isLoading } = useQuery<ConversationHistory>({
    queryKey: ['conversation', senderId],
    queryFn: () => getConversationHistory(senderId),
  });

  return (
    <>
      <div className="drawer-overlay" onClick={onClose} />
      <aside className="conversation-drawer">
        <div className="drawer__header">
          <div>
            <div className="drawer__title">💬 Lịch Sử Hội Thoại</div>
            <div className="drawer__sender-id">ID: {senderId}</div>
          </div>
          <button className="drawer__close" onClick={onClose} title="Đóng">
            ✕
          </button>
        </div>

        <div className="drawer__messages">
          {isLoading ? (
            <div className="drawer__loading">Đang tải...</div>
          ) : data?.messages.length ? (
            data.messages.map((msg, idx) => (
              <div
                key={idx}
                className={`chat-bubble chat-bubble--${msg.role.toLowerCase()}`}
                style={{ animationDelay: `${idx * 0.05}s` }}
              >
                {msg.intent && (
                  <span className="chat-bubble__intent">{msg.intent}</span>
                )}
                <div>{msg.content}</div>
                <div className="chat-bubble__time">{formatChatTime(msg.createdAt)}</div>
              </div>
            ))
          ) : (
            <div className="drawer__loading">Không có tin nhắn</div>
          )}
        </div>
      </aside>
    </>
  );
}

export default function ComplaintsPage() {
  const [selectedSender, setSelectedSender] = useState<string | null>(null);

  const { data: complaints, isLoading } = useQuery({
    queryKey: ['complaints'],
    queryFn: () => getComplaints(),
  });

  const needsAttentionCount = complaints?.filter((c) => c.needsAttention).length ?? 0;

  return (
    <div className="app-layout">
      <Sidebar />

      <main className="complaints">
        {/* Header */}
        <header className="complaints__header">
          <div>
            <h1 className="complaints__title">⚠️ Khiếu Nại</h1>
            <p className="complaints__subtitle">
              Quản lý phản hồi tiêu cực từ khách hàng
            </p>
          </div>
        </header>

        {/* Summary Bar */}
        <div className="complaints__summary">
          <div className="summary-chip">
            <span className="summary-chip__icon">📩</span>
            {complaints?.length ?? 0} khiếu nại
          </div>
          {needsAttentionCount > 0 && (
            <div className="summary-chip summary-chip--danger">
              <span className="summary-chip__icon">🚨</span>
              {needsAttentionCount} cần xử lý
            </div>
          )}
        </div>

        {/* Complaint List */}
        {isLoading ? (
          <div className="complaints__loading">
            <div className="complaints__loading-spinner" />
            Đang tải khiếu nại...
          </div>
        ) : complaints?.length ? (
          <div className="complaints__list">
            {complaints.map((complaint: Complaint, idx: number) => (
              <div
                key={complaint.id}
                className={`complaint-row ${complaint.needsAttention ? 'complaint-row--attention' : ''}`}
                onClick={() => setSelectedSender(complaint.fbSenderId)}
                style={{ animationDelay: `${idx * 0.05}s` }}
              >
                <div className="complaint-row__avatar">
                  {complaint.fbSenderId.slice(-2).toUpperCase()}
                </div>

                <div className="complaint-row__content">
                  <div className="complaint-row__text">{complaint.content}</div>
                  <div className="complaint-row__meta">
                    <span className="complaint-row__sender">
                      {complaint.fbSenderId.slice(0, 12)}...
                    </span>
                    <span className="complaint-row__time">
                      {formatTime(complaint.createdAt)}
                    </span>
                  </div>
                </div>

                <SentimentBadge
                  label={complaint.sentimentLabel}
                  score={complaint.sentimentScore}
                />

                <span className="complaint-row__arrow">›</span>
              </div>
            ))}
          </div>
        ) : (
          <div className="complaints__empty">
            <div className="complaints__empty-icon">🎉</div>
            <div className="complaints__empty-text">
              Không có khiếu nại nào — tuyệt vời!
            </div>
          </div>
        )}

        {/* Conversation Drawer */}
        {selectedSender && (
          <ConversationDrawer
            senderId={selectedSender}
            onClose={() => setSelectedSender(null)}
          />
        )}
      </main>
    </div>
  );
}
