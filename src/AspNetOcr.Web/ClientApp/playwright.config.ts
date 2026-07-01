import { defineConfig, devices } from '@playwright/test';

const dotnet = 'DOTNET_CLI_HOME=/tmp/dotnet-home /tmp/dotnet9/dotnet';
const uiCommand = process.env['ASP_OCR_THEME'] === 'contrast-lab' ? 'npm run start:contrast-lab' : 'npm run start';
const uiPort = process.env['ASP_OCR_THEME'] === 'contrast-lab' ? 4204 : 4203;

export default defineConfig({
  testDir: './e2e',
  timeout: 45_000,
  expect: {
    timeout: 8_000
  },
  fullyParallel: false,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: `http://127.0.0.1:${uiPort}`,
    trace: 'retain-on-failure',
    channel: 'chrome'
  },
  webServer: [
    {
      command: `${dotnet} run --no-build --project ../../AspNetOcr.Api/AspNetOcr.Api.csproj --urls http://127.0.0.1:5195`,
      url: 'http://127.0.0.1:5195/health',
      reuseExistingServer: true,
      timeout: 120_000
    },
    {
      command: uiCommand,
      url: `http://127.0.0.1:${uiPort}`,
      reuseExistingServer: true,
      timeout: 120_000
    }
  ],
  projects: [
    {
      name: 'desktop',
      use: { ...devices['Desktop Chrome'] }
    },
    {
      name: 'mobile',
      use: { ...devices['Pixel 5'] }
    }
  ]
});
