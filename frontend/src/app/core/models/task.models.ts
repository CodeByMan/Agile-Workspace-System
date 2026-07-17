export enum WorkItemType {
  Epic = 0,
  UserStory = 1,
  Task = 2,
  Bug = 3,
}

export enum WorkStatus {
  ToDo = 1,
  InProgress = 2,
  Done = 3,
  Blocked = 4,
}

export enum TaskPriority {
  Low = 1,
  Medium = 2,
  High = 3,
  Critical = 4,
}

export interface TaskComment {
  Id: number;
  Content: string;
  CreatedAt: string;
  CreatedByUserId: string;
  CreatedByUserName: string | null;
}

export interface TaskActivity {
  Id: number;
  ActivityType: string;
  Description: string;
  OldValue: string | null;
  NewValue: string | null;
  CreatedAt: string;
  PerformedByUserId: string;
  PerformedByUserName: string | null;
}

export interface WorkItem {
  Id: number;
  Title: string;
  Description: string;
  AcceptanceCriteria: string;
  WorkItemType: WorkItemType;
  Status: WorkStatus;
  Priority: TaskPriority;
  StartDate: string | null;
  DueDate: string | null;
  CompletedAt: string | null;
  StoryPoints: number;
  IsRecurring: boolean;
  RecurrenceRule: string | null;
  ProjectId: number;
  ProjectName: string | null;
  SprintId: number | null;
  SprintName: string | null;
  AssignedToUserId: string | null;
  AssignedToUserName: string | null;
  CreatedByUserId: string;
  CreatedByUserName: string | null;
  ParentTaskId: number | null;
  SubTaskCount: number;
  CompletedSubTaskCount: number;
  Comments: TaskComment[];
  ActivityLogs: TaskActivity[];
  RowVersion: string;
}

export interface WorkItemSummary {
  Id: number;
  Title: string;
  Description: string;
  WorkItemType: WorkItemType;
  Status: WorkStatus;
  Priority: TaskPriority;
  StartDate: string | null;
  DueDate: string | null;
  StoryPoints: number;
  ProjectId: number;
  ProjectName: string | null;
  SprintId: number | null;
  SprintName: string | null;
  AssignedToUserId: string | null;
  AssignedToUserName: string | null;
  CreatedByUserId: string;
  CreatedByUserName: string | null;
  ParentTaskId: number | null;
  CompletedAt?: string | null;
  RowVersion?: string;
}

export interface WorkItemsResponse {
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  items: WorkItemSummary[];
}

export interface WorkItemPayload {
  title: string;
  description: string;
  acceptanceCriteria: string;
  workItemType: WorkItemType;
  status: WorkStatus;
  priority: TaskPriority;
  startDate: string | null;
  dueDate: string | null;
  storyPoints: number;
  isRecurring: boolean;
  recurrenceRule: string | null;
  projectId: number;
  sprintId: number | null;
  assignedToUserId: string | null;
  parentTaskId: number | null;
  rowVersion?: string | null;
}
