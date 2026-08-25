export interface Clinic {
  id: string;
  name: string;
  legalId?: string | null;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  timeZone: string;
  isActive: boolean;
}
