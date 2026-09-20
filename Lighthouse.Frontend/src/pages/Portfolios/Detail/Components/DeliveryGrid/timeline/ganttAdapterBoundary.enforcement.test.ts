import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));
const sourceRoot = resolve(here, "../../../../../..");

/**
 * The single file allowed to know which Gantt library this product uses.
 *
 * Confining the import is what makes the choice reversible: replacing the library should be a
 * rewrite of one component, not a search across the application. That property decays silently —
 * the second import compiles, passes review and is only expensive later — so it is asserted here
 * rather than agreed in a document.
 */
const THE_ADAPTER =
	"pages/Portfolios/Detail/Components/DeliveryGrid/timeline/DeliveryGanttChart.tsx";

const VENDOR = "@svar-ui/";

const sourceFilesUnder = (directory: string): string[] =>
	readdirSync(directory).flatMap((entry) => {
		const path = join(directory, entry);

		if (statSync(path).isDirectory()) {
			return sourceFilesUnder(path);
		}

		return /\.(ts|tsx)$/.test(entry) ? [path] : [];
	});

describe("the Gantt adapter boundary", () => {
	const importers = sourceFilesUnder(sourceRoot)
		.filter((path) => readFileSync(path, "utf8").includes(VENDOR))
		.map((path) => relative(sourceRoot, path).replaceAll("\\", "/"))
		// Production files only. A test names the vendor in order to stand it in, which is how the
		// contract handed across this boundary gets asserted at all; this file names it in order to
		// police it. What the rule protects is shipped code — a second importer there is what turns
		// replacing the library from a rewrite of one file into a search across the app.
		.filter((path) => !/\.test\.tsx?$/.test(path));

	it("is the only place the Gantt library is named", () => {
		expect(importers).toEqual([THE_ADAPTER]);
	});

	it("keeps the vendor's vocabulary out of its own public surface", () => {
		const adapter = readFileSync(resolve(sourceRoot, THE_ADAPTER), "utf8");
		const exportedProps =
			/export interface DeliveryGanttChartProps \{([\s\S]*?)\n\}/.exec(adapter);

		expect(exportedProps).not.toBeNull();
		// A vendor type reaching the props is the boundary leaking outward: every caller would then
		// depend on the library through a type it never imports, and the swap stops being local.
		expect(exportedProps?.[1]).not.toContain(VENDOR);
		expect(exportedProps?.[1]).not.toMatch(/\bITask\b|\bIGantt|\bILink\b/);
	});
});
