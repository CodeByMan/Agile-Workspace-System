export interface NotificationItem {
  Id: number;
  Type: string;
  Title: string;
  Message: string;
  Link: string | null;
  IsRead: boolean;
  TaskItemId: number | null;
  CreatedAt: string;
}

export interface NotificationSummary {
  Total: number;
  Unread: number;
  Items: NotificationItem[];
}
