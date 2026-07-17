export interface Profile {
  Id: string;
  UserName: string;
  Email: string;
  FullName: string;
  JobTitle: string | null;
  PhoneNumber: string | null;
  Role: string;
  IsActive: boolean;
  CreatedAt: string;
}

export interface UpdateProfilePayload {
  fullName: string;
  email: string;
  jobTitle: string | null;
  phoneNumber: string | null;
}

export interface ChangePasswordPayload {
  currentPassword: string;
  newPassword: string;
}
