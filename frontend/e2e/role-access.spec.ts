import { test, expect } from '@playwright/test';
import { ApiFixtures, loginViaUi } from './helpers';

test.describe('role-based access', () => {
  test('Administrador can reach the Usuarios screen', async ({ page }) => {
    await loginViaUi(page, 'admin@vetplatform.dev', 'Admin123!');

    await page.getByRole('link', { name: 'Usuarios' }).click();
    await expect(page).toHaveURL(/\/users$/);
    await expect(page.getByRole('heading', { name: 'Usuarios' })).toBeVisible();
  });

  test('Recepcion is redirected away from Usuarios and a consultation URL', async ({ page }) => {
    const admin = await ApiFixtures.loginAsDemoAdmin();
    const receptionPassword = 'E2ePassword123!';
    const { email } = await admin.createClinicUser('Recepcion', receptionPassword);
    await admin.dispose();

    await loginViaUi(page, email, receptionPassword);

    await expect(page.getByRole('link', { name: 'Usuarios' })).toHaveCount(0);

    await page.goto('/users');
    await expect(page).toHaveURL(/\/dashboard$/);

    await page.goto('/consultations/00000000-0000-0000-0000-000000000000');
    await expect(page).toHaveURL(/\/dashboard$/);
  });
});
