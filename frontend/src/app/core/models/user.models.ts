export interface UserSummary {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  roles: string[];
  isActive: boolean;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  fullName: string;
  role?: string;
  roles: string[];
  clinicId?: string | null;
}

export interface UpdateUserRolesRequest {
  role?: string;
  roles: string[];
}
