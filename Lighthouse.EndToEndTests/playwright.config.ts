import { defineConfig, devices } from "@playwright/test";

// biome-ignore lint/complexity/noStaticOnlyClass: <explanation>
export class TestConfig {
	static readonly ADOTOKENNAME: string = "AzureDevOpsLighthouseE2ETestToken";
	static readonly JIRATOKENNAME: string = "JiraLighthouseIntegrationTestToken";
	static readonly LIGHTHOUSEURLNAME: string = "LIGHTHOUSEURL";
	static readonly LINEARAPITOKENNAME: string = "LinearAPIKey";

	private static getEnvVariable(name: string, defaultValue: string): string {
		const value = process.env[name];

		if (!value) {
			console.log(`No value found for ${name} - using default`);
			return defaultValue;
		}

		return value;
	}

	public static get LighthouseUrl(): string {
		return TestConfig.getEnvVariable(
			TestConfig.LIGHTHOUSEURLNAME,
			"http://localhost:5169/",
		);
	}

	public static get AzureDevOpsToken(): string {
		return TestConfig.getEnvVariable(TestConfig.ADOTOKENNAME, "");
	}

	public static get JiraToken(): string {
		return TestConfig.getEnvVariable(TestConfig.JIRATOKENNAME, "");
	}

	public static get LinearApiKey(): string {
		return TestConfig.getEnvVariable(TestConfig.LINEARAPITOKENNAME, "");
	}
}

/**
 * See https://playwright.dev/docs/test-configuration.
 */
export default defineConfig({
	testDir: "./tests",
	/* Run tests in files in parallel */
	fullyParallel: true,
	/* Fail the build on CI if you accidentally left test.only in the source code. */
	forbidOnly: !!process.env.CI,
	/* Retry on CI only */
	retries: process.env.CI ? 2 : 0,

	workers: 1,

	/* Reporter to use. See https://playwright.dev/docs/test-reporters */
	reporter: "html",
	/* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */

	use: {
		/* Base URL to use in actions like `await page.goto('/')`. */
		baseURL: TestConfig.LighthouseUrl,

		ignoreHTTPSErrors: true,

		/* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
		trace: "on-first-retry",
		video: "retain-on-failure",
	},

	/* Set longer timeouts as we depend for some tests on 3rd party software we don't have control over (ADO/Jira) */
	timeout: 120000,
	expect: {
		timeout: 30000,
	},

	/* Configure projects for major browsers */
	projects: [
		{
			name: "chromium",
			use: {
				...devices["Desktop Chrome"],
				viewport: { width: 1920, height: 1080 },
				// Override the default viewport specifically for chromium
			},
		},
	],
});
