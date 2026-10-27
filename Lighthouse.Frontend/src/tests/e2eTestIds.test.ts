import { existsSync, readdirSync, readFileSync } from "node:fs";
import { join, resolve } from "node:path";
import { describe, expect, it } from "vitest";

const e2eTests = resolve(__dirname, "../../../Lighthouse.EndToEndTests/tests");
const frontendSource = resolve(__dirname, "..");

/**
 * Test ids the page objects ask for that the frontend does not spell out in one piece. Each needs
 * a reason, because an id missing from the frontend otherwise only shows up as a 120 second
 * Playwright timeout (or, worse, as a "not visible" check that passes on an id nothing renders).
 */
const NOT_SPELLED_OUT_IN_SOURCE: Readonly<Record<string, string>> = {
	"recurring-interval-weeks":
		"built from the dialog's prefix and 'interval-weeks' by WeeklyRecurrenceFields",
	"import-configuration-button":
		"stale: the frontend no longer renders it, so the E2E page object has to change",
	"import-configuration-dialog":
		"stale: the frontend no longer renders it, so the E2E page object has to change",
	"validation-results":
		"stale: the frontend no longer renders it, so the E2E page object has to change",
	"log-level-section":
		"stale: the frontend no longer renders it, so the RBAC spec's not-visible check proves nothing",
};

const sourceFilesUnder = (folder: string): string[] =>
	readdirSync(folder, { withFileTypes: true }).flatMap((entry) => {
		const path = join(folder, entry.name);
		if (entry.isDirectory()) {
			return entry.name === "tests" || entry.name === "node_modules"
				? []
				: sourceFilesUnder(path);
		}
		return /\.tsx?$/.test(entry.name) && !/\.test\.tsx?$/.test(entry.name)
			? [path]
			: [];
	});

const allFilesUnder = (folder: string): string[] =>
	readdirSync(folder, { withFileTypes: true }).flatMap((entry) => {
		const path = join(folder, entry.name);
		if (entry.isDirectory()) return allFilesUnder(path);
		return entry.name.endsWith(".ts") ? [path] : [];
	});

const read = (path: string) => readFileSync(path, "utf8");

/** What a single getByTestId argument pins down: a whole id, or only the start of one. */
type AskedFor = { text: string; whole: boolean };

const constantsIn = (e2eSource: string) =>
	new Map(
		[
			...e2eSource.matchAll(/\b([A-Z][A-Z0-9_]*)\s*=\s*(["'`])([^"'`$]+)\2/g),
		].map((match) => [match[1], match[3]] as const),
	);

const askedFor = (argument: string, constants: Map<string, string>) => {
	const quoted = /^(["'`])([^"'`]*)\1$/.exec(argument);
	if (quoted && !quoted[2].includes("${")) {
		return { text: quoted[2], whole: true } satisfies AskedFor;
	}
	const template = /^`([^`$]*)\$\{/.exec(argument);
	if (template) return { text: template[1], whole: false } satisfies AskedFor;
	const pattern = /^\/\^([\w-]+)/.exec(argument);
	if (pattern) return { text: pattern[1], whole: false } satisfies AskedFor;
	const constant = constants.get(argument);
	if (constant !== undefined) return { text: constant, whole: true };
	return undefined;
};

const e2eAvailable = existsSync(e2eTests);

describe.skipIf(!e2eAvailable)("E2E test ids", () => {
	const e2eSources = e2eAvailable ? allFilesUnder(e2eTests).map(read) : [];
	const constants = new Map(e2eSources.flatMap((s) => [...constantsIn(s)]));
	const asked = e2eSources
		.flatMap((s) => [...s.matchAll(/getByTestId\(\s*([^)]*?)\s*\)/g)])
		.map((match) => askedFor(match[1], constants))
		.filter((id): id is AskedFor => id !== undefined && id.text.length > 0);
	const frontend = e2eAvailable
		? sourceFilesUnder(frontendSource).map(read).join("\n")
		: "";

	it("reads a believable number of ids out of the page objects", () => {
		expect(asked.length).toBeGreaterThan(100);
	});

	it("finds every test id a page object asks for in the frontend source", () => {
		const missing = asked
			.filter(({ text }) => !(text in NOT_SPELLED_OUT_IN_SOURCE))
			.filter(({ text }) => !frontend.includes(text))
			.filter(
				({ text }) => !frontend.includes(`${text.replace(/\d+$/, "")}\${`),
			)
			.map(({ text, whole }) => (whole ? text : `${text}…`));

		expect([...new Set(missing)].sort()).toEqual([]);
	});
});
