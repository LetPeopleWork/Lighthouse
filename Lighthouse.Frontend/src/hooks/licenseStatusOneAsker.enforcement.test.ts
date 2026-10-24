import { readdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const sourceRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");

/**
 * The service itself, and two pages that read the licence for something other than gating: the survey nudge
 * and the licence card in System Settings. Every other module that needs the licence asks through one shared
 * query, so a page with many gates sends one request.
 */
const READ_THE_LICENCE_FOR_OTHER_REASONS = new Set([
	"services/Api/LicensingService.ts",
	"components/SurveyNudge/SurveyNudge.tsx",
	"pages/Settings/System/SystemSettingsTab.tsx",
]);

const productionModules = (): string[] =>
	readdirSync(sourceRoot, { recursive: true, encoding: "utf8" })
		.filter((file) => /\.tsx?$/.test(file) && !/\.test\.tsx?$/.test(file))
		.filter((file) => !file.startsWith("tests/"));

describe("Story 6406 (slice 02): one page asks for the licence once", () => {
	it.skip("only one module asks the server for the licence on behalf of every gate", () => {
		const askers = productionModules()
			.filter((file) => !READ_THE_LICENCE_FOR_OTHER_REASONS.has(file))
			.filter((file) =>
				readFileSync(resolve(sourceRoot, file), "utf8").includes(
					".getLicenseStatus(",
				),
			);

		expect(askers).toHaveLength(1);
	});
});
