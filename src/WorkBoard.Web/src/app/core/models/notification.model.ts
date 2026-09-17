/** One in-app notification. */
export interface NotificationView {
  id: string;
  type: string;
  message: string;
  taskId: string | null;
  isRead: boolean;
  createdAtUtc: string;
}
