import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));
const sourceRoot = resolve(here, "../../../..");
/** The kits tests are built from; they name what they check for, and ship nowhere. */
const testSupport = resolve(sourceRoot, "tests");

const THE_PRINT_PAGES = resolve(here, "reportPrintRoutes.tsx");

/** Static imports and re-exports, and the `import("./…")` a lazily loaded page is reached through. */
const relativeImportsOf = (file: string): string[] => {
	const source = readFileSync(file, "utf8");
	return [
		...source.matchAll(
			/^\s*(?:import|export)[^;]*?from\s+"(\.{1,2}\/[^"]+)";/gms,
		),
		...source.matchAll(/\bimport\(\s*"(\.{1,2}\/[^"]+)"\s*\)/g),
	].map(([, specifier]) => specifier);
};

const fileOf = (from: string, specifier: string): string | undefined =>
	["", ".ts", ".tsx", "/index.ts", "/index.tsx"]
		.map((ending) => resolve(dirname(from), specifier + ending))
		.find((candidate) => existsSync(candidate) && statSync(candidate).isFile());

/** Every source file the print pages reach through their imports, relative to src. */
const whatThePrintPagesReach = (): string[] => {
	const reached = new Set<string>();
	const toVisit = [THE_PRINT_PAGES];
	while (toVisit.length > 0) {
		const file = toVisit.pop() as string;
		if (reached.has(file)) continue;
		reached.add(file);
		for (const specifier of relativeImportsOf(file)) {
			const next = fileOf(file, specifier);
			if (next !== undefined) toVisit.push(next);
		}
	}
	return [...reached].map((file) =>
		relative(sourceRoot, file).replaceAll("\\", "/"),
	);
};

const productionSourcesUnder = (directory: string): string[] =>
	readdirSync(directory).flatMap((entry) => {
		const path = join(directory, entry);
		if (path === testSupport) return [];
		if (statSync(path).isDirectory()) return productionSourcesUnder(path);
		return /\.(ts|tsx)$/.test(entry) && !/\.test\.tsx?$/.test(entry)
			? [path]
			: [];
	});

/**
 * A printed report is the report and nothing around it. The print pages draw the same summary and panels as
 * the report view, so the two can never print different numbers, and they never reach the app's navigation,
 * footer, prompts or usage reporting: a page drawn for paper is not a page anyone opened.
 */
describe("what the print pages are built from", () => {
	// @us-01 @slice-01 @contract-shape:unbounded-preservation
	it("draws the report with the same summary and panels as the report view", () => {
		const reached = whatThePrintPagesReach();

		expect(reached).toContain("pages/Common/Reports/ThenAndNowSummary.tsx");
		expect(reached).toContain(
			"pages/Common/Reports/panels/reportPanelRegistry.ts",
		);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it.each([
		"components/App/Header/Header.tsx",
		"components/App/Footer/Footer.tsx",
		"components/SurveyNudge/SurveyNudge.tsx",
		"components/UsageData/UsageDataAsk.tsx",
		"services/UsageData/usageDataReporter.ts",
		"pages/Common/Reports/useReportOpenedReporter.ts",
	])("never reaches %s", (file) => {
		const reached = whatThePrintPagesReach();

		expect(reached).toContain("pages/Common/Reports/print/ReportPrintPage.tsx");
		expect(reached).not.toContain(file);
	});
});

describe("who asks the browser for its print dialog", () => {
	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// One caller, so the dialog can open once and only once; a second is a second dialog.
	it("is the print page alone", () => {
		const askers = productionSourcesUnder(sourceRoot)
			.filter((file) =>
				/\bglobalThis\.print\(/.test(readFileSync(file, "utf8")),
			)
			.map((file) => relative(sourceRoot, file).replaceAll("\\", "/"));
		const theWindowsOwn = productionSourcesUnder(sourceRoot).filter((file) =>
			/\bwindow\.print\(/.test(readFileSync(file, "utf8")),
		);

		expect(askers).toHaveLength(1);
		expect(askers[0]).toMatch(/^pages\/Common\/Reports\/print\//);
		expect(theWindowsOwn).toEqual([]);
	});
});

describe("who loads fonts into the document", () => {
	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// The print page's fonts go through its effect port, so a test can stand in for them; one binding names the browser's.
	it("is the print page's browser binding alone", () => {
		const loaders = productionSourcesUnder(sourceRoot)
			.filter((file) => /\bdocument\.fonts\b/.test(readFileSync(file, "utf8")))
			.map((file) => relative(sourceRoot, file).replaceAll("\\", "/"));

		expect(loaders).toHaveLength(1);
		expect(loaders[0]).toMatch(/^pages\/Common\/Reports\/print\//);
	});
});

describe("what the print page reads through", () => {
	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// Only the routes build the page's ports; the page and what it draws never reach for every service the app has.
	it("is the read port it is handed, never the app's services", () => {
		const printFolder = dirname(THE_PRINT_PAGES);
		const pageModules = productionSourcesUnder(printFolder).filter(
			(file) => file !== THE_PRINT_PAGES,
		);
		const thePage = resolve(printFolder, "ReportPrintPage.tsx");

		expect(existsSync(thePage), "there is no print page module").toBe(true);
		expect(readFileSync(thePage, "utf8")).toContain("PrintReadPort");
		for (const file of pageModules) {
			expect(
				readFileSync(file, "utf8"),
				`${relative(sourceRoot, file)} reads the app's services`,
			).not.toContain("ApiServiceContext");
		}
	});
});

describe("who counts an export", () => {
	// @us-01 @slice-01 @error @kpi-OUT-6298-K1-pdf-export-used @contract-shape:unbounded-preservation
	// One starter for the report view's button and the list's icon, so a third surface cannot count differently.
	it(`is the one export starter, whichever surface the reader exports from`, () => {
		const theEventList = "services/Api/UsageDataService.ts";
		const counters = productionSourcesUnder(sourceRoot)
			.filter((file) =>
				readFileSync(file, "utf8").includes("ReportPdfExportStarted"),
			)
			.map((file) => relative(sourceRoot, file).replaceAll("\\", "/"))
			.filter((file) => file !== theEventList);

		expect(counters).toHaveLength(1);
		expect(counters[0]).toMatch(/^pages\/Common\/Reports\//);
	});
});
