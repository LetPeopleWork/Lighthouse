import { defineConfig, devices } from "@playwright/test";

// The measurement harness runs by hand against an instance a developer has already started, so there
// is no webServer here, and CI never calls it: its readings depend on the machine they were taken on.
export default defineConfig({
	testDir: "./perf",
	outputDir: "./perf/out/test-results",
	fullyParallel: false,
	workers: 1,
	retries: 0,
	reporter: [["list"]],

	// A reading over many Features can run for minutes; a timeout would cut the measurement short.
	timeout: 30 * 60_000,
	expect: {
		timeout: 5 * 60_000,
	},

	use: {
		baseURL: process.env.LIGHTHOUSEURL ?? "http://localhost:5169/",
		ignoreHTTPSErrors: true,
		timezoneId: "Europe/Zurich",
	},

	projects: [
		{
			name: "chromium",
			use: {
				...devices["Desktop Chrome"],
				viewport: { width: 1920, height: 1080 },
			},
		},
	],
});
