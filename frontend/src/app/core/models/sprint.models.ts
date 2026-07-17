export interface Sprint {
  Id: number;
  Name: string;
  Goal: string;
  ProjectId: number;
  ProjectName: string | null;
  StartDate: string;
  EndDate: string;
  IsClosed: boolean;
  PlannedItemsCount: number;
  CompletedItemsCount: number;
  RowVersion: string;
}

export interface SprintPayload {
  name: string;
  goal: string;
  projectId: number;
  startDate: string;
  endDate: string;
  isClosed?: boolean;
  rowVersion?: string;
}
