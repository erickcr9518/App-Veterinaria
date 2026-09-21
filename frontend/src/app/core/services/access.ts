import { CurrentUser } from '../models/auth.models';

const VETHECA_PERMISSION = 'vetheca.ask';

// A user whose only permission is Vetheca (the "Veterinario Vetheca" role) has
// no clinical-management screens to land on - their home is Vetheca itself.
export function userHasClinicalAccess(user: CurrentUser | null): boolean {
  return user?.permissions.some((code) => code !== VETHECA_PERMISSION) ?? false;
}

export function homeRouteFor(user: CurrentUser | null): string {
  if (userHasClinicalAccess(user)) {
    return '/dashboard';
  }

  return user?.permissions.includes(VETHECA_PERMISSION) ? '/vetheca' : '/account';
}
