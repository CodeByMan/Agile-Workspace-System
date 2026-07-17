export interface TaskStatusCount {
  Status: string;
  Count: number;
}

export interface DashboardSummary {
  TotalProjects: number;
  ActiveProjects: number;
  TotalTasks: number;
  ToDoTasks: number;
  InProgressTasks: number;
  DoneTasks: number;
  OverdueTasks: number;
  DueThisWeekTasks: number;
  CompletedThisWeekTasks: number;
  TotalStoryPoints: number;
  CompletedStoryPoints: number;
  CompletionRate: number;
  TasksByStatus: TaskStatusCount[];
  TopProjects: ProjectProgress[];
}

export interface ProjectProgress {
  ProjectId: number;
  ProjectName: string;
  IsArchived: boolean;
  TotalTasks: number;
  ToDoTasks: number;
  InProgressTasks: number;
  DoneTasks: number;
  OverdueTasks: number;
  TotalStoryPoints: number;
  CompletedStoryPoints: number;
  CompletionRate: number;
  StartDate: string;
  EndDate: string | null;
}

export interface RecentActivity {
  Id: number;
  TaskId: number;
  ActivityType: string;
  Description: string;
  OldValue: string | null;
  NewValue: string | null;
  CreatedAt: string;
  PerformedByUserId: string;
  PerformedByUserName: string | null;
  TaskTitle: string | null;
  ProjectName: string | null;
}

export interface UserProductivity {
  UserId: string;
  UserName: string;
  FullName: string;
  AssignedTasks: number;
  CompletedTasks: number;
  InProgressTasks: number;
  OverdueTasks: number;
  TotalStoryPoints: number;
  CompletedStoryPoints: number;
  CompletionRate: number;
}

export interface BurndownPoint {
  Date: string;
  RemainingTasks: number;
  RemainingStoryPoints: number;
  CompletedTasks: number;
}

export interface BurndownChart {
  ProjectId: number;
  ProjectName: string;
  StartDate: string;
  EndDate: string;
  InitialTaskCount: number;
  InitialStoryPoints: number;
  Points: BurndownPoint[];
}
