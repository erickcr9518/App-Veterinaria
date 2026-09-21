import { CurrentUser } from '../models/auth.models';
import { homeRouteFor, userHasClinicalAccess } from './access';

describe('access helpers', () => {
  it('treats a Vetheca-only user as having no clinical access and sends them to Vetheca', () => {
    const user = createUser(['vetheca.ask']);

    expect(userHasClinicalAccess(user)).toBe(false);
    expect(homeRouteFor(user)).toBe('/vetheca');
  });

  it('sends a user with clinical permissions to the dashboard, even if they also have Vetheca', () => {
    const user = createUser(['patients.read', 'vetheca.ask']);

    expect(userHasClinicalAccess(user)).toBe(true);
    expect(homeRouteFor(user)).toBe('/dashboard');
  });

  it('sends a user with no permissions at all to their account page', () => {
    expect(homeRouteFor(createUser([]))).toBe('/account');
    expect(homeRouteFor(null)).toBe('/account');
  });

  function createUser(permissions: string[]): CurrentUser {
    return {
      userId: 'user-1',
      email: 'qa@vetplatform.test',
      fullName: 'QA User',
      clinicId: 'clinic-1',
      clinicName: 'Clinica Demo',
      role: 'Veterinario Vetheca',
      roles: ['Veterinario Vetheca'],
      permissions,
    };
  }
});
