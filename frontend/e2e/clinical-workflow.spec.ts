import { test, expect } from '@playwright/test';
import { ApiFixtures, loginViaUi } from './helpers';

test.describe('clinical workflow', () => {
  test('a veterinarian can draft and finalize a consultation, then a prescription from it', async ({ page }) => {
    const admin = await ApiFixtures.loginAsDemoAdmin();
    const vetPassword = 'E2ePassword123!';
    const { email: vetEmail } = await admin.createClinicUser('Veterinario', vetPassword);
    const ownerId = await admin.createOwner();
    const patientName = `E2E Patient ${Date.now()}`;
    const patientId = await admin.createPatient(ownerId, patientName);
    await admin.dispose();

    await loginViaUi(page, vetEmail, vetPassword);

    // --- Consultation: draft, then finalize ---
    await page.goto(`/patients/${patientId}/consultations/new`);
    await page.getByLabel('Motivo de consulta').fill('Chequeo E2E de rutina');
    await page.getByLabel('A — Evaluacion').fill('Paciente estable, sin hallazgos relevantes.');
    await page.getByLabel('P — Plan').fill('Control en 6 meses.');
    await page.getByRole('button', { name: 'Guardar borrador' }).click();

    await expect(page).toHaveURL(/\/consultations\/[0-9a-f-]+$/);
    await expect(page.locator('.status')).toHaveText('Borrador');

    await page.getByRole('button', { name: 'Finalizar consulta' }).click();
    await page.getByRole('button', { name: 'Si, finalizar' }).click();
    await expect(page.locator('.status')).toHaveText('Finalizada');

    const consultationUrl = page.url();
    const consultationId = consultationUrl.split('/').pop();

    // --- Prescription tied to that consultation: draft, then finalize ---
    await page.goto(`/consultations/${consultationId}/prescriptions/new`);
    await page.getByLabel('Nombre del producto').fill('Meloxicam E2E');
    await page.getByLabel('Cantidad').fill('1 frasco');
    await page.getByLabel('Via').fill('Oral');
    await page.getByLabel('Frecuencia').fill('Cada 24 horas');
    await page.getByLabel('Duracion').fill('5 dias');
    await page.getByRole('button', { name: 'Guardar borrador' }).click();

    await expect(page).toHaveURL(/\/prescriptions\/[0-9a-f-]+$/);
    await expect(page.locator('.status')).toHaveText('Borrador');

    await page.getByRole('button', { name: 'Finalizar receta' }).click();
    await page.getByRole('button', { name: 'Si, finalizar' }).click();
    await expect(page.locator('.status')).toHaveText('Finalizada');
    await expect(page.getByText('Meloxicam E2E')).toBeVisible();
  });
});
