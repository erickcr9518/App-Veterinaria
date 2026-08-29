import { test, expect } from '@playwright/test';
import { loginViaUi } from './helpers';

test.describe('login', () => {
  test('valid credentials land on the dashboard', async ({ page }) => {
    await loginViaUi(page, 'admin@vetplatform.dev', 'Admin123!');

    await expect(page.getByRole('heading', { name: /^Hola,/ })).toBeVisible();
  });

  test('invalid credentials show an error and stay on the login page', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Correo electrónico').fill('admin@vetplatform.dev');
    await page.getByLabel('Contraseña').fill('wrong-password');
    await page.getByRole('button', { name: 'Ingresar' }).click();

    await expect(page.locator('.error-banner')).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });

  test('logout clears the session and blocks going back to a protected page', async ({ page }) => {
    await loginViaUi(page, 'admin@vetplatform.dev', 'Admin123!');

    await page.getByRole('button', { name: 'Cerrar sesión' }).click();
    await expect(page).toHaveURL(/\/login$/);

    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login$/);
  });
});
