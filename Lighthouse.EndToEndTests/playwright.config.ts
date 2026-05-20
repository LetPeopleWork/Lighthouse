import { defineConfig, devices } from "@playwright/test";

// biome-ignore lint/complexity/noStaticOnlyClass: <explanation>
export class TestConfig {
	static readonly ADOTOKENNAME: string = "AzureDevOpsLighthouseE2ETestToken";
	static readonly JIRATOKENNAME: string = "JiraLighthouseIntegrationTestToken";
	static readonly LIGHTHOUSEURLNAME: string = "LIGHTHOUSEURL";
	static readonly LINEARAPITOKENNAME: string = "LinearAPIKey";
	static readonly BACKUPPASSWORDNAME: string = "LighthouseBackupPassword";

	static readonly AUTH_TEST_USER_USERNAME: string = "test@user.com";
	static readonly AUTH_TEST_USER_PASSWORD: string = "Test123!!?lsdkaflaskdf";

	static readonly AUTHZ_TEST_SYSTEMADMIN_USERNAME : string = "systemadmin@user.com";
	static readonly AUTHZ_TEST_PORTFOLIOADMIN_USERNAME: string = "portfolioadmin@user.com";
	static readonly AUTHZ_TEST_PORTFOLIOREADER_USERNAME: string = "portfolioreader@user.com";
	static readonly AUTHZ_TEST_TEAMADMIN_USERNAME: string = "teamadmin@user.com";
	static readonly AUTHZ_TEST_TEAMREADER_USERNAME: string = "teamreader@user.com";

	static readonly SYSTEMADMIN_GROUP_NAME: string = "system-admins";
	static readonly PORTFOLIOADMIN_GROUP_NAME: string = "portfolio-admins";
	static readonly PORTFOLIOREADER_GROUP_NAME: string = "portfolio-readers";
	static readonly TEAMADMIN_GROUP_NAME: string = "team-admins";
	static readonly TEAMREADER_GROUP_NAME: string = "team-readers";

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

	public static get BackupPassword(): string {
		return TestConfig.getEnvVariable(TestConfig.BACKUPPASSWORDNAME, "");
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

	/* Set longer timeouts as we depend for some tests on 3rd party software we don't have control over (ADO/Jira).
	   expect.timeout was 30s and sat exactly on the edge of real Jira/ADO refresh latency (25-35s on a warm run,
	   longer under runner load), producing chronic flake on PortfolioDetail / ado / TeamsDetail specs.
	   60s gives 2x headroom for the default; specs that wait on external-IO completion can still raise individually
	   (see {{ timeout: 90_000 }} call-sites). */
	timeout: 120000,
	expect: {
		timeout: 60000,
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
