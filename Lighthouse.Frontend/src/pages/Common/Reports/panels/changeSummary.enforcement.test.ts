import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));

const sourceOf = (file: string): string =>
	readFileSync(resolve(here, file), "utf8");

const importsOf = (source: string): string[] =>
	Array.from(source.matchAll(/^import[^;]*?from\s+"([^"]+)";/gms)).map(
		([, specifier]) => specifier,
	);

/**
 * The summary header and the panels must judge a line the same way, and a report rendered away from the browser
 * must judge it the same way again. That holds only while the judgement stays a plain function of its arguments:
 * the moment it reaches for React, MUI or the instance's words, it can no longer be called on its own.
 */
describe("the judgement behind the summary's CHANGE side", () => {
	it.each(["changeSummary.ts", "thenNowComparison.ts"])(
		"keeps %s free of React, MUI and the instance's words",
		(file) => {
			const imports = importsOf(sourceOf(file));

			expect(imports.length).toBeGreaterThan(0);
			for (const specifier of imports) {
				expect(specifier).not.toMatch(/^react($|\/|-)|^@mui\/|Terminology/);
			}
		},
	);

	// The words and marks the reader sees belong to the header; the tally only sorts metrics into kinds.
	it("leaves every word and mark of the CHANGE side to the header", () => {
		expect(sourceOf("changeSummary.ts")).not.toMatch(
			/✔|✘|±|Improved|Got worse|Mixed|Unchanged/,
		);
	});
});
