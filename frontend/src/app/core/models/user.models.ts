export interface UserSummary {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  isActive: boolean;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  fullName: string;
  role: string;
  clinicId?: string | null;
}
