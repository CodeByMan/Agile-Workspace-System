export interface UserSummary {
  Id: string;
  UserName: string;
  Email: string;
  FullName: string;
  JobTitle: string | null;
  PhoneNumber: string | null;
  Role: string;
  IsActive: boolean;
  AssignedTasks: number;
  CompletedTasks: number;
  CreatedAt: string;
}

export interface UserListResult {
  TotalCount: number;
  PageNumber: number;
  PageSize: number;
  Items: UserSummary[];
}

export interface UpdateUserPayload {
  fullName: string;
  email: string;
  role: string;
  jobTitle: string | null;
  phoneNumber: string | null;
}
