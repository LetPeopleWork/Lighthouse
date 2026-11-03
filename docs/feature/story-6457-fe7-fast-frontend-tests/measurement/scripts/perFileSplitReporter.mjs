// A small Vitest reporter that writes the per-file time split the stock JSON reporter leaves out.
// Usage: pnpm exec vitest run --reporter=default --reporter=/abs/path/perFileSplitReporter.mjs
// Output: file named by env SPLIT_OUT (default ./per-file-split.json), one entry per test file:
// environmentSetupDuration, prepareDuration, collectDuration (import + suite bodies), setupDuration,
// duration (tests and hooks), all in milliseconds. Read it with split.py.
import { writeFileSync } from "node:fs";

export default class PerFileSplitReporter {
	rows = [];

	onTestModuleEnd(testModule) {
		const d = testModule.diagnostic();
		this.rows.push({
			file: testModule.moduleId,
			environmentSetupMs: d.environmentSetupDuration,
			prepareMs: d.prepareDuration,
			collectMs: d.collectDuration,
			setupMs: d.setupDuration,
			testsMs: d.duration,
		});
	}

	onTestRunEnd() {
		writeFileSync(process.env.SPLIT_OUT ?? "per-file-split.json", JSON.stringify(this.rows));
	}
}
