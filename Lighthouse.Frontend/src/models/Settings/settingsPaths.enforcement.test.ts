import { existsSync, readdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const sourceRoot = resolve(dirname(fileURLToPath(import.meta.url)), "../..");

/** The only modules that may spell out where a settings page lives. */
const THE_DEFINITION = "models/Settings/settingsAreas.ts";
const THE_ROUTES = "pages/Settings/settingsRoutes.tsx";

/**
 * Modules that still hold a connection address of their own. They move to the definition when the connection pages
 * move; the list may shrink, and a module that is not on it may not start holding one.
 */
const STILL_HOLDING_A_CONNECTION_ADDRESS = new Set<string>();

const productionModules = (): string[] =>
	readdirSync(sourceRoot, { recursive: true, encoding: "utf8" })
		.map((file) => file.replaceAll("\\", "/"))
		.filter((file) => /\.tsx?$/.test(file) && !/\.test\.tsx?$/.test(file))
		.filter((file) => !file.startsWith("tests/"));

/** A string or template literal that starts with the given address root. */
const literalStartingWith = (root: string): RegExp =>
	new RegExp(`["'\`]/${root}(?![A-Za-z0-9])`);

const modulesHoldingALiteralFor = (root: string): string[] =>
	productionModules().filter((file) =>
		literalStartingWith(root).test(
			readFileSync(resolve(sourceRoot, file), "utf8"),
		),
	);

describe("one place knows where the settings pages live", () => {
	it("scans the whole production source, so a clean result means something", () => {
		const scanned = productionModules();

		expect(
			scanned,
			"The scan must see the header and the app shell, or a clean result only means it looked at nothing. Check the source root the scan reads.",
		).toEqual(
			expect.arrayContaining(["components/App/Header/Header.tsx", "App.tsx"]),
		);
	});

	it("no module outside the definition and the settings routes writes a settings address", () => {
		const offenders = modulesHoldingALiteralFor("settings").filter(
			(file) => file !== THE_DEFINITION && file !== THE_ROUTES,
		);

		expect(
			offenders,
			`These modules write a /settings address by hand: ${offenders.join(", ")}. A hand-written address is how the license link came to lead nowhere. Take the address from ${THE_DEFINITION} instead.`,
		).toEqual([]);
	});

	it("no module that did not already hold a connection address starts writing one", () => {
		const offenders = modulesHoldingALiteralFor("connections").filter(
			(file) =>
				file !== THE_DEFINITION &&
				file !== THE_ROUTES &&
				!STILL_HOLDING_A_CONNECTION_ADDRESS.has(file),
		);

		expect(
			offenders,
			`These modules write a /connections address by hand: ${offenders.join(", ")}. Take it from ${THE_DEFINITION} instead.`,
		).toEqual([]);
	});

	it("the definition and the routes exist and do hold the addresses, so the links did not simply disappear", () => {
		const missing = [THE_DEFINITION, THE_ROUTES].filter(
			(file) => !existsSync(resolve(sourceRoot, file)),
		);

		expect(
			missing,
			`The settings definition and routes are missing: ${missing.join(", ")}. Without them nothing owns the settings addresses.`,
		).toEqual([]);
		expect(
			modulesHoldingALiteralFor("settings"),
			"The definition must spell out the settings addresses; a scan that finds none anywhere proves nothing was moved to it.",
		).toContain(THE_DEFINITION);
	});
});
