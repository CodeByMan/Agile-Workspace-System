export interface Project {
  Id: number;
  Name: string;
  Description: string;
  StartDate: string;
  EndDate: string | null;
  IsArchived: boolean;
  CreatedByUserId: string;
  CreatedByUserName: string | null;
  TotalTasks: number;
  CompletedTasks: number;
  RowVersion: string;
}

export interface ProjectPayload {
  name: string;
  description: string;
  startDate: string;
  endDate: string | null;
  isArchived?: boolean;
  rowVersion?: string;
}
