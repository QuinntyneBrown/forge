import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  workers: 1,
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:4321',
    trace: 'on-first-retry',
    ignoreHTTPSErrors: true
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    }
  ],
  webServer: {
    command: 'npx ng serve forge --host 127.0.0.1 --port 4321',
    url: 'http://localhost:4321',
    reuseExistingServer: !process.env['CI'],
    timeout: 180000
  }
});
