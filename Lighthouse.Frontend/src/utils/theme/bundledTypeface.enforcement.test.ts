import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));
const frontendRoot = resolve(here, "../../..");
const sourceRoot = resolve(frontendRoot, "src");

const THE_PACKAGE = "@fontsource-variable/quicksand";
const ITS_FAMILY = "Quicksand Variable";
/** The package's own entry or one of its weight files, in either quote style. */
const ITS_IMPORT =
	/import\s+["']@fontsource-variable\/quicksand(\/[^"']*)?["']/;
const SHIPPED_LICENCE = resolve(
	frontendRoot,
	"public/licenses/quicksand-OFL.txt",
);
const THE_PACKAGES_LICENCE = resolve(
	frontendRoot,
	`node_modules/${THE_PACKAGE}/LICENSE`,
);

const productionSourcesUnder = (directory: string): string[] =>
	readdirSync(directory).flatMap((entry) => {
		const path = join(directory, entry);
		if (statSync(path).isDirectory()) return productionSourcesUnder(path);
		return /\.(ts|tsx)$/.test(entry) && !/\.test\.tsx?$/.test(entry)
			? [path]
			: [];
	});

/** Every font stack in the app that names the Lighthouse typeface, with the file it is in. */
const quicksandStacks = (): { file: string; stack: string }[] =>
	productionSourcesUnder(sourceRoot).flatMap((file) =>
		Array.from(
			readFileSync(file, "utf8").matchAll(/fontFamily:\s*(["'`])(.*?)\1/g),
		)
			.map(([, , stack]) => stack)
			.filter((stack) => /quicksand/i.test(stack))
			.map((stack) => ({
				file: relative(sourceRoot, file).replaceAll("\\", "/"),
				stack,
			})),
	);

/**
 * The Lighthouse typeface travels inside the app, so a report printed on a machine with no internet, or in the
 * Lighthouse app, is set in the same letters as everywhere else. Nothing reaches out to a font host, the font's
 * licence ships beside it word for word, and every place that names the typeface names the one the app carries.
 */
describe("the Lighthouse typeface ships with the app", () => {
	it(`loads no font from the internet`, () => {
		const page = readFileSync(resolve(frontendRoot, "index.html"), "utf8");

		expect(page).toContain("<html");
		expect(page).not.toMatch(/fonts\.googleapis\.com|fonts\.gstatic\.com/);
	});

	it(`brings the typeface in from the package the app carries`, () => {
		const importers = productionSourcesUnder(sourceRoot).filter((file) =>
			ITS_IMPORT.test(readFileSync(file, "utf8")),
		);

		expect(importers).toHaveLength(1);
	});

	// A stack that names the typeface by any other family falls back to a machine font without a word.
	it(`names the carried family first wherever the app asks for the typeface`, () => {
		const stacks = quicksandStacks();

		expect(stacks.length).toBeGreaterThan(0);
		for (const { file, stack } of stacks) {
			expect(
				stack.replaceAll(/["']/g, "").split(",")[0].trim(),
				`${file} asks for "${stack}"`,
			).toBe(ITS_FAMILY);
		}
	});

	// The licence asks to travel with the font, unchanged; a package update that changes it must change the copy.
	it(`ships the typeface's licence word for word as the package carries it`, () => {
		expect(
			existsSync(THE_PACKAGES_LICENCE),
			"the package is not installed",
		).toBe(true);
		expect(existsSync(SHIPPED_LICENCE), "the licence is not shipped").toBe(
			true,
		);

		expect(readFileSync(SHIPPED_LICENCE)).toEqual(
			readFileSync(THE_PACKAGES_LICENCE),
		);
	});
});
