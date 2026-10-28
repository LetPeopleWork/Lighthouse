import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));
const sourceRoot = resolve(here, "../..");
/** The kits tests are built from; they ship nowhere. */
const testSupport = resolve(sourceRoot, "tests");
const THE_SERVICE = "services/Api/ReportService.ts";

const productionSourcesUnder = (directory: string): string[] =>
	readdirSync(directory).flatMap((entry) => {
		const path = join(directory, entry);
		if (path === testSupport) return [];
		if (statSync(path).isDirectory()) return productionSourcesUnder(path);
		return /\.(ts|tsx)$/.test(entry) && !/\.test\.tsx?$/.test(entry)
			? [path]
			: [];
	});

const sourcesCalling = (member: string): string[] =>
	productionSourcesUnder(sourceRoot)
		.filter((file) =>
			new RegExp(String.raw`\.${member}\s*\(`).test(readFileSync(file, "utf8")),
		)
		.map((file) => relative(sourceRoot, file).replaceAll("\\", "/"))
		.filter((file) => file !== THE_SERVICE);

/**
 * A bulk run reads every report list again once it is over, created or not. One hook makes the call and does
 * that, so no other caller can create reports and leave the lists showing what was there before.
 */
describe("who creates reports for several owners", () => {
	// @us-02a @slice-02a @contract-shape:unbounded-preservation
	it("only the bulk creation hook asks the server to create reports in bulk", () => {
		expect(sourcesCalling("createReportsInBulk")).toEqual([
			"pages/Reports/useBulkReportCreation.ts",
		]);
	});
});
