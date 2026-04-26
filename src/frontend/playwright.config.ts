import { defineConfig, devices } from '@playwright/test';

const frontendBaseUrl = process.env.PLAYWRIGHT_FRONTEND_URL ?? 'http://127.0.0.1:4315';

export default defineConfig({
  testDir: './playwright/tests',
  fullyParallel: true,
  timeout: 45_000,
  expect: {
    timeout: 10_000
  },
  reporter: [
    ['list'],
    ['html', { open: 'never' }]
  ],
  use: {
    baseURL: frontendBaseUrl,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  },
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome']
      }
    }
  ]
});
