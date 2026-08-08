export interface AuthResult {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  userId: string;
  email: string;
  fullName: string;
  clinicId: string | null;
  clinicName: string | null;
  role: string;
  permissions: string[];
}

export interface CurrentUser {
  userId: string;
  email: string;
  fullName: string;
  clinicId: string | null;
  clinicName: string | null;
  role: string;
  permissions: string[];
}
