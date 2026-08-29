import { defineConfig, devices } from '@playwright/test';

const API_URL = process.env.E2E_API_URL ?? 'http://localhost:5033';
const FRONTEND_URL = process.env.E2E_FRONTEND_URL ?? 'http://localhost:4200';

export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  reporter: 'list',
  use: {
    baseURL: FRONTEND_URL,
    trace: 'on-first-retry',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: [
    {
      command: 'dotnet run --launch-profile http',
      cwd: '../backend/src/VetPlatform.Api',
      env: {
        RateLimiting__Auth__PermitLimit: '1000',
        RateLimiting__Auth__WindowSeconds: '60',
      },
      url: `${API_URL}/swagger/index.html`,
      reuseExistingServer: true,
      timeout: 120_000,
      stdout: 'pipe',
      stderr: 'pipe',
    },
    {
      command: 'npm run start',
      url: FRONTEND_URL,
      reuseExistingServer: true,
      timeout: 120_000,
      stdout: 'pipe',
      stderr: 'pipe',
    },
  ],
});
