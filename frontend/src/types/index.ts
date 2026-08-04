// Tất cả TypeScript types/interfaces cho dự án

export type OrderStatus =
  | 'Draft'
  | 'Confirmed'
  | 'Preparing'
  | 'Completed'
  | 'Cancelled';

export type PaymentStatus = 'Unpaid' | 'Paid' | 'Refunded';

export interface OrderItem {
  id: string;
  menuItemId: string;
  itemName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
  note?: string | null;
}

export interface Order {
  id: string;
  shopId: string;
  customerId: string;
  customerName: string;
  fbSenderId: string;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  paymentMethod: string;
  totalAmount: number;
  trackingToken: string;
  note: string | null;
  // Thông tin giao hàng (AI parse)
  receiverName: string;
  receiverPhone: string;
  deliveryAddress: string;
  // AI metadata
  parseConfidence: number | null;
  unclearParts: string[];
  rawMessageContent: string;
  createdAt: string;
  updatedAt: string;
  items: OrderItem[];
}

export interface UpdateOrderRequest {
  receiverName?: string;
  receiverPhone?: string;
  deliveryAddress?: string;
  paymentMethod?: string;
  note?: string;
  items?: UpdateOrderItemRequest[];
}

export interface UpdateOrderItemRequest {
  menuItemId: string;
  itemName: string;
  unitPrice: number;
  quantity: number;
  note?: string;
}

export interface TrackOrderResult {
  id: string;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  totalAmount: number;
  note: string | null;
  receiverName: string;
  createdAt: string;
  updatedAt: string;
  items: OrderItem[];
}

export interface MenuItem {
  id: string;
  shopId: string;
  name: string;
  description: string | null;
  price: number;
  isAvailable: boolean;
  imageUrl: string | null;
  createdAt: string;
}

export interface AuthResult {
  token: string;
  expiresAt: string;
  shopId: string; // decode từ JWT, không phải từ response
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface FacebookPage {
  pageId: string;
  pageName: string;
  pictureUrl: string | null;
}

export interface SelectPageRequest {
  pageId: string;
  pageAccessToken: string;
  pageName: string;
}

export interface CreateMenuItemRequest {
  name: string;
  description?: string;
  price: number;
  imageUrl?: string;
}

export interface UpdateMenuItemRequest {
  name?: string;
  description?: string;
  price?: number;
  isAvailable?: boolean;
  imageUrl?: string;
}

export interface UpdateOrderStatusRequest {
  status: OrderStatus;
}
// ── AI Types ──────────────────────────────────────────────────────────────────

export type AiIntent =
  | 'PlaceOrder'
  | 'AskMenu'
  | 'AskOrderStatus'
  | 'Greeting'
  | 'Complaint'
  | 'Compliment'
  | 'Other';

export interface SentimentResult {
  label: 'positive' | 'neutral' | 'negative';
  score: number;
  needsAttention: boolean;
}

export interface UpsellSuggestion {
  itemName: string;
  price: number;
  reason: string;
}

export interface AiResponse {
  intent: AiIntent;
  parseResult?: {
    items: { name: string; quantity: number; note: string | null }[];
    receiverName: string | null;
    receiverPhone: string | null;
    deliveryAddress: string | null;
    confidence: number;
    unclearParts: string[];
  };
  replyText: string;
  suggestions: UpsellSuggestion[];
  sentiment?: SentimentResult;
  confidence: number;
}

export interface AiTestRequest {
  text: string;
  shopId: string;
  fbSenderId?: string;
}

// ── Analytics Types ──────────────────────────────────────────────────────────

export interface DailyRevenue {
  date: string;
  revenue: number;
  orderCount: number;
}

export interface TopMenuItem {
  name: string;
  totalQuantity: number;
  totalRevenue: number;
}

export interface HourlyDistribution {
  hour: number;
  orderCount: number;
}

export interface OrderStatusBreakdown {
  status: string;
  count: number;
}

export interface AnalyticsSummary {
  totalRevenue: number;
  totalOrders: number;
  completedOrders: number;
  cancelledOrders: number;
  averageOrderValue: number;
  dailyRevenue: DailyRevenue[];
  topMenuItems: TopMenuItem[];
  hourlyDistribution: HourlyDistribution[];
  statusBreakdown: OrderStatusBreakdown[];
}

export interface IntentDistribution {
  intent: string;
  count: number;
}

export interface AiStats {
  totalMessages: number;
  parsedSuccessfully: number;
  needsClarification: number;
  averageConfidence: number;
  intentDistribution: IntentDistribution[];
  complaintsTotal: number;
  complaintsNeedingAttention: number;
}

// ── Complaint Types ──────────────────────────────────────────────────────────

export interface Complaint {
  id: string;
  fbSenderId: string;
  content: string;
  sentimentLabel: string | null;
  sentimentScore: number | null;
  needsAttention: boolean;
  createdAt: string;
}

export interface ConversationMessage {
  role: string;
  content: string;
  intent: string | null;
  createdAt: string;
}

export interface ConversationHistory {
  fbSenderId: string;
  messages: ConversationMessage[];
}

