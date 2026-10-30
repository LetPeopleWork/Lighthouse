import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));
const sourceRoot = resolve(here, "../../../..");
/** The kits tests are built from; they name what they check for, and ship nowhere. */
const testSupport = resolve(sourceRoot, "tests");

const THE_PRINT_PAGES = resolve(here, "reportPrintRoutes.tsx");
const THE_BINDING = "pages/Common/Reports/print/browserPrintEffects.ts";

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

/** Every source file a module reaches through its imports, the print pages unless told otherwise, relative to src. */
const whatThePrintPagesReach = (from: string = THE_PRINT_PAGES): string[] => {
	const reached = new Set<string>();
	const toVisit = [from];
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
	it("draws the report with the same summary and panels as the report view", () => {
		const reached = whatThePrintPagesReach();

		expect(reached).toContain("pages/Common/Reports/ThenAndNowSummary.tsx");
		expect(reached).toContain(
			"pages/Common/Reports/panels/reportPanelRegistry.ts",
		);
	});

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

const THE_BROWSER = "(?:globalThis|window|self)";

/** Every way a module can reach the browser's print dialog: called, called if present, indexed, or taken apart. */
const ASKS_FOR_THE_PRINT_DIALOG = [
	new RegExp(String.raw`\b${THE_BROWSER}\s*\??\.\s*print\b`),
	new RegExp(
		String.raw`\b${THE_BROWSER}\s*(?:\?\.)?\s*\[\s*["'\`]print["'\`]\s*\]`,
	),
	new RegExp(String.raw`\{[^{}]*\bprint\b[^{}]*\}\s*=\s*${THE_BROWSER}\b`),
];

/** The production modules whose source matches, relative to src. */
const productionModulesNaming = (
	pattern: RegExp,
	under: string = sourceRoot,
): string[] =>
	productionSourcesUnder(under)
		.filter((file) => pattern.test(readFileSync(file, "utf8")))
		.map((file) => relative(sourceRoot, file).replaceAll("\\", "/"));

describe("who asks the browser for its print dialog", () => {
	// One caller, so the dialog can open once and only once; a second is a second dialog.
	it("is the browser binding alone, however the dialog is spelled", () => {
		const askers = productionSourcesUnder(sourceRoot)
			.filter((file) =>
				ASKS_FOR_THE_PRINT_DIALOG.some((spelling) =>
					spelling.test(readFileSync(file, "utf8")),
				),
			)
			.map((file) => relative(sourceRoot, file).replaceAll("\\", "/"));
		const theWindowsOwn = productionModulesNaming(
			/\bwindow\s*(?:\??\.\s*print\b|\[\s*["'`]print["'`]\s*\])/,
		);

		expect(askers.filter((file) => file !== THE_BINDING)).toEqual([]);
		expect(theWindowsOwn).toEqual([]);
	});

	// The dialog closing is heard on the window that was asked, so only the module that asks may listen for it.
	it("is the only one that hears the dialog close", () => {
		expect(productionModulesNaming(/["'`]afterprint["'`]/)).toEqual([
			THE_BINDING,
		]);
	});
});

describe("who loads fonts into a document", () => {
	// The report's fonts go through its effect port, so a test can stand in for them; one binding names the browser's.
	it("is the browser binding alone", () => {
		expect(productionModulesNaming(/\.fonts\s*\.\s*(?:load|ready)\b/)).toEqual([
			THE_BINDING,
		]);
	});
});

describe("who makes a frame", () => {
	// One way to make the print frame, so a second, unwatched frame fails by name.
	it("is the browser binding, and nothing else", () => {
		expect(
			productionModulesNaming(
				/\biframe\b|\bsrcdoc\b|\bcontentWindow\b|\bcontentDocument\b/i,
			).filter((file) => file !== THE_BINDING),
		).toEqual([]);
	});
});

describe("what prints without moving the reader", () => {
	it(`nothing under print/ navigates`, () => {
		expect(productionModulesNaming(/\buseNavigate\b/, here)).toEqual([]);
	});

	// Export PDF prints where the reader is; the starter can never slip back into taking them somewhere.
	it(`the export starter neither navigates nor holds the router's navigation`, () => {
		const starters = productionModulesNaming(/ReportPdfExportStarted/).filter(
			(file) => file !== "services/Api/UsageDataService.ts",
		);
		expect(starters).toHaveLength(1);

		const starter = readFileSync(resolve(sourceRoot, starters[0]), "utf8");
		expect(starter).not.toMatch(/\buseNavigate\b/);
		expect(starter).not.toMatch(/\bnavigate\s*\(/);
	});

	// In the frame the global document is the app's, so a title written there would name the wrong page.
	it(`nothing under print/ but the binding writes a document's title`, () => {
		expect(
			productionModulesNaming(/\bdocument\.title\s*=(?!=)/, here).filter(
				(file) => file !== THE_BINDING,
			),
		).toEqual([]);
	});

	// The frame host draws the same content as the route, so it must reach none of the app's chrome either.
	it(`the frame host, alone in giving the frame a style cache of its own, draws the report's summary and panels and reaches none of the app's chrome`, () => {
		const hosts = productionModulesNaming(/from\s+["']@emotion\/cache["']/);
		expect(hosts).toHaveLength(1);
		expect(hosts[0]).toMatch(/^pages\/Common\/Reports\/print\//);

		const reached = whatThePrintPagesReach(resolve(sourceRoot, hosts[0]));
		expect(reached).toContain("pages/Common/Reports/ThenAndNowSummary.tsx");
		expect(reached).toContain(
			"pages/Common/Reports/panels/reportPanelRegistry.ts",
		);
		for (const chrome of [
			"components/App/Header/Header.tsx",
			"components/App/Footer/Footer.tsx",
			"components/SurveyNudge/SurveyNudge.tsx",
			"components/UsageData/UsageDataAsk.tsx",
			"services/UsageData/usageDataReporter.ts",
			"pages/Common/Reports/useReportOpenedReporter.ts",
		]) {
			expect(reached).not.toContain(chrome);
		}
	});
});

describe("what the print page reads through", () => {
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
