import { appendFileSync } from "node:fs";
import { relative } from "node:path";
import type {
	FullConfig,
	Reporter,
	Suite,
	TestCase,
} from "@playwright/test/reporter";

/**
 * A test that fails and then passes on a retry leaves the CI job green, so
 * without this nobody sees it. Names such tests in the GitHub Actions job
 * summary; writes nothing when not running under GitHub Actions.
 */
class FlakySummaryReporter implements Reporter {
	private rootSuite: Suite | undefined;

	onBegin(_config: FullConfig, suite: Suite): void {
		this.rootSuite = suite;
	}

	onEnd(): void {
		const summaryFile = process.env.GITHUB_STEP_SUMMARY;
		if (!summaryFile || !this.rootSuite) {
			return;
		}

		const tests = this.rootSuite.allTests();
		const flaky = tests.filter((test) => test.outcome() === "flaky");
		const failed = tests.filter((test) => test.outcome() === "unexpected");

		appendFileSync(summaryFile, this.summaryOf(flaky, failed));
	}

	printsToStdio(): boolean {
		return false;
	}

	private summaryOf(flaky: TestCase[], failed: TestCase[]): string {
		const lines = ["## Playwright: tests that needed a retry", ""];

		if (flaky.length === 0) {
			lines.push("No flaky tests: nothing needed a retry to pass.");
		} else {
			lines.push(
				`${flaky.length} test(s) passed only after a retry:`,
				"",
				...flaky.map(
					(test) =>
						`- ${this.describe(test)} - passed on attempt ${test.results.length}`,
				),
			);
		}

		if (failed.length > 0) {
			lines.push(
				"",
				`${failed.length} test(s) failed on every attempt:`,
				"",
				...failed.map((test) => `- ${this.describe(test)}`),
			);
		}

		return `${lines.join("\n")}\n\n`;
	}

	private describe(test: TestCase): string {
		const project = test.parent.project();
		const file = relative(
			project?.testDir ?? process.cwd(),
			test.location.file,
		);
		const titles = test.titlePath().slice(3).join(" › ");
		const projectName = project?.name ? ` (${project.name})` : "";
		return `\`${file}\` › ${titles}${projectName}`;
	}
}

export default FlakySummaryReporter;
