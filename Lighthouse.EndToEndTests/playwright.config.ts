import { defineConfig, devices } from '@playwright/test';

export class TestConfig {
  static readonly ADOTOKENNAME: string = 'AzureDevOpsLighthouseE2ETestToken';
  static readonly JIRATOKENNAME: string = 'JiraLighthouseIntegrationTestToken'
  static readonly LIGHTHOUSEURLNAME: string = 'LIGHTHOUSEURL';

  private static getEnvVariable(name: string, defaultValue: string): string  {
    const value = process.env[name];

    if (!value) {
      console.log(`No value found for ${name} - using default`);
      return defaultValue;
    }

    return value;
  };

  public static get LighthouseUrl(): string {
    return TestConfig.getEnvVariable(TestConfig.LIGHTHOUSEURLNAME, "http://localhost:8080");
  }

  public static get AzureDevOpsToken(): string {
    return TestConfig.getEnvVariable(TestConfig.ADOTOKENNAME, "");
  }

  public static get JiraToken(): string {
    return TestConfig.getEnvVariable(TestConfig.JIRATOKENNAME, "");
  }
}


/**
 * See https://playwright.dev/docs/test-configuration.
 */
export default defineConfig({
  testDir: './tests',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: 'html',
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: TestConfig.LighthouseUrl,

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',
    video: 'retain-on-failure',
  },

  /* Configure projects for major browsers */
  projects: process.env.CI ? [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    }
  ] : [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    }
  ]
});
