import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));
const sourceRoot = resolve(here, "..");

/**
 * Lighthouse decides which Work Items are stale, once, on the server; the dashboard shows that answer.
 * A second copy of the rule in the browser is how the dashboard and `lh` came to disagree, and it
 * would come back the same way: a comparison of days against a staleness threshold, written beside a
 * badge or a count. So that comparison is not allowed anywhere in the shipped app.
 *
 * Reading a threshold to ask "is staleness set up at all" (it is compared with zero) is not the rule
 * and stays; nor is the Blocked overview's health, which grades the oldest blocker against the
 * blocked threshold and lives in its own file.
 */
const ALLOWED = ["pages/Common/MetricsView/blockedMaxAgeRag.ts"];

const THE_DELETED_RULE = "deriveStaleness";

// A staleness threshold on either side of a <, <=, > or >= whose other side is anything but zero.
// Its limit: it only sees a threshold under a name containing "stalenessThreshold", so a copy that
// renamed it (`limit`, `maxDays`) would slip past; review still has to catch that.
const THRESHOLD_ON_THE_LEFT =
	/stalenessthreshold\w*\s*(?:\?\?\s*0\s*\)\s*)?(?<!=)[<>]=?\s*(?!0\b)[\w.(]/i;
const THRESHOLD_ON_THE_RIGHT =
	/[\w.)\]]\s*(?<![=])[<>]=?\s*[\w.]*stalenessthreshold/i;

const isComment = (line: string): boolean => /^\s*(\/\/|\*|\/\*)/.test(line);

const comparesDaysWithAStalenessThreshold = (source: string): boolean =>
	source
		.split("\n")
		.filter((line) => !isComment(line))
		.some(
			(line) =>
				THRESHOLD_ON_THE_LEFT.test(line) || THRESHOLD_ON_THE_RIGHT.test(line),
		);

const shippedSourceFiles = (directory: string): string[] =>
	readdirSync(directory).flatMap((entry) => {
		const path = join(directory, entry);

		if (statSync(path).isDirectory()) {
			return shippedSourceFiles(path);
		}

		return /\.(ts|tsx)$/.test(entry) && !/\.test\.tsx?$/.test(entry)
			? [path]
			: [];
	});

const relativeToSource = (path: string): string =>
	relative(sourceRoot, path).replaceAll("\\", "/");

describe("one staleness rule, and it is Lighthouse's", () => {
	// @kpi @contract-shape:unbounded-preservation
	it("the browser's copy of the rule is gone", () => {
		const stillNamingIt = shippedSourceFiles(sourceRoot)
			.filter((path) => readFileSync(path, "utf8").includes(THE_DELETED_RULE))
			.map(relativeToSource);

		expect(stillNamingIt).toEqual([]);
	});

	// @kpi @contract-shape:unbounded-preservation
	it("nothing in the app compares days with a staleness threshold", () => {
		const offenders = shippedSourceFiles(sourceRoot)
			.map(relativeToSource)
			.filter((path) => !ALLOWED.includes(path))
			.filter((path) =>
				comparesDaysWithAStalenessThreshold(
					readFileSync(resolve(sourceRoot, path), "utf8"),
				),
			);

		expect(offenders).toEqual([]);
	});

	// The two checks above are only as good as the pattern they share, so it is held to the shapes it
	// has to catch and the ones it has to let through.
	// @contract-shape:pure-function
	it.each([
		"if (days > stalenessThreshold) {",
		"\tdaysInState >= blockedStalenessThresholdDays &&",
		"const stale = stalenessThresholdDays < item.days;",
		"return age > props.stalenessThresholdDays;",
	])("the guard recognises a copy of the rule: %s", (line) => {
		expect(comparesDaysWithAStalenessThreshold(line)).toBe(true);
	});

	// @contract-shape:pure-function
	it.each([
		"(stalenessThresholdDays ?? 0) > 0 ||",
		"setIsStalenessEnabled(settings.stalenessThresholdDays > 0);",
		"onChange={(e) => handleStalenessThresholdChange(e.target.value)}",
		"// time-in-state: daysInState > stalenessThresholdDays",
		"const isStale = item.isStale === true;",
	])("the guard lets through what is not the rule: %s", (line) => {
		expect(comparesDaysWithAStalenessThreshold(line)).toBe(false);
	});
});
