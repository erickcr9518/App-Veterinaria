import { APIRequestContext, Page, expect, request } from '@playwright/test';

export const API_URL = process.env.E2E_API_URL ?? 'http://localhost:5033';
const DEMO_ADMIN_EMAIL = 'admin@vetplatform.dev';
const DEMO_ADMIN_PASSWORD = 'Admin123!';

export async function loginViaUi(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/login');
  await page.getByLabel('Correo electrónico').fill(email);
  await page.getByLabel('Contraseña').fill(password);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
}

/**
 * Fixture setup that goes straight through the API rather than the UI —
 * these E2E tests are about proving the real app works end to end, not
 * about re-testing forms already covered by unit specs. Each helper
 * returns the created id.
 */
export class ApiFixtures {
  private constructor(private readonly api: APIRequestContext, readonly accessToken: string) {}

  static async loginAsDemoAdmin(): Promise<ApiFixtures> {
    return ApiFixtures.login(DEMO_ADMIN_EMAIL, DEMO_ADMIN_PASSWORD);
  }

  static async login(email: string, password: string): Promise<ApiFixtures> {
    const api = await request.newContext({ baseURL: API_URL });
    const response = await api.post('/api/auth/login', { data: { email, password } });
    if (!response.ok()) {
      throw new Error(`Login failed for ${email}: ${response.status()} ${await response.text()}`);
    }
    const body = await response.json();
    return new ApiFixtures(api, body.accessToken);
  }

  private authHeaders() {
    return { Authorization: `Bearer ${this.accessToken}` };
  }

  async createClinicUser(role: string, password: string): Promise<{ userId: string; email: string }> {
    const email = `e2e-${role.toLowerCase()}-${Date.now()}@vetplatform.test`;
    const response = await this.api.post('/api/users', {
      headers: this.authHeaders(),
      data: { email, password, fullName: `E2E ${role}`, role },
    });
    if (!response.ok()) {
      throw new Error(`Create user failed: ${response.status()} ${await response.text()}`);
    }
    const userId = await response.json();
    return { userId, email };
  }

  async createOwner(): Promise<string> {
    const response = await this.api.post('/api/owners', {
      headers: this.authHeaders(),
      data: {
        fullName: `E2E Owner ${Date.now()}`,
        phone: '8888-0000',
        email: `e2e-owner-${Date.now()}@example.test`,
      },
    });
    if (!response.ok()) {
      throw new Error(`Create owner failed: ${response.status()} ${await response.text()}`);
    }
    return response.json();
  }

  async createPatient(ownerId: string, name: string): Promise<string> {
    const response = await this.api.post('/api/patients', {
      headers: this.authHeaders(),
      data: {
        ownerId,
        name,
        species: 'Perro',
        sex: 'Macho',
        status: 'Activo',
      },
    });
    if (!response.ok()) {
      throw new Error(`Create patient failed: ${response.status()} ${await response.text()}`);
    }
    return response.json();
  }

  async dispose(): Promise<void> {
    await this.api.dispose();
  }
}
