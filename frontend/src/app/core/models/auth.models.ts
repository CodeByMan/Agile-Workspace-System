export interface AuthUser {
  Id: string;
  UserName: string;
  Email: string;
  FullName: string;
  Role: string;
  Token: string;
}
export interface LoginRequest {
  username: string;
  password: string;
}
export interface RegisterRequest {
  username: string;
  email: string;
  fullName: string;
  password: string;
}
