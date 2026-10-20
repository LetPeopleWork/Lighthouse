import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { aRefusal } from "../../../../tests/ReportsTestKit";
import {
	type PrintFontsState,
	type PrintPageInputs,
	type PrintPageState,
	printPageStateOf,
} from "./printPageState";

/**
 * What the print page shows is decided from what it has, and from nothing else. It is ready only when a report
 * answer that arrived after the page opened, the owner's name, the instance's terms and the fonts are all in. A
 * report that no longer exists or was made by a newer version is a failure nothing can mend; any other failure of the
 * report read can be tried again, and so can a page whose report was read but whose owner's name, terms or fonts were
 * not, which says so in its own words. An answer the page was already holding when it opened is never enough.
 */

const OPENED = 1_000;

const everythingIn: PrintPageInputs = {
	fetchedAt: OPENED + 500,
	mountedAt: OPENED,
	reportError: null,
	ownerName: "Lightspeed",
	ownerError: null,
	termsLoading: false,
	termsError: null,
	fonts: "loaded",
};

describe("the print page's state", () => {
	// @us-01 @slice-01 @contract-shape:pure-function
	it.each<[string, Partial<PrintPageInputs>, PrintPageState]>([
		["everything is in", {}, "ready"],
		[
			"the answer arrived the moment the page opened",
			{ fetchedAt: OPENED },
			"ready",
		],
		["there is no answer yet", { fetchedAt: null }, "preparing"],
		[
			"the only answer is from before the page opened",
			{ fetchedAt: OPENED - 1 },
			"preparing",
		],
		["the owner's name has not arrived", { ownerName: null }, "preparing"],
		["the terms are still loading", { termsLoading: true }, "preparing"],
		["the fonts are still being asked for", { fonts: "pending" }, "preparing"],
		["a font could not be loaded", { fonts: "failed" }, "failedPart"],
		[
			"the report read failed, though an answer is held",
			{ reportError: new Error("the network dropped") },
			"failedRetryable",
		],
		[
			"the report read was refused for another reason",
			{ reportError: aRefusal(500, "unexpected") },
			"failedRetryable",
		],
		[
			"the owner's name could not be read",
			{ ownerError: new Error("the network dropped") },
			"failedPart",
		],
		[
			"the terms could not be read",
			{ termsError: new Error("the network dropped") },
			"failedPart",
		],
		[
			"the report read failed and a font failed too",
			{ reportError: new Error("the network dropped"), fonts: "failed" },
			"failedRetryable",
		],
		[
			"the report no longer exists, though an answer is held",
			{ reportError: aRefusal(404, "not-found") },
			"failedFinal",
		],
		[
			"a newer version made the report, though an answer is held",
			{ reportError: aRefusal(422, "template-not-supported") },
			"failedFinal",
		],
		[
			"the report no longer exists and a font failed too",
			{ reportError: aRefusal(404, "not-found"), fonts: "failed" },
			"failedFinal",
		],
	])("decides the state when %s", (_when, change, expected) => {
		expect(printPageStateOf({ ...everythingIn, ...change })).toBe(expected);
	});

	// @us-01 @slice-01 @property @edge @contract-shape:pure-function
	// Every combination of the four inputs without a failure: ready exactly when all four are in, else preparing.
	it("is ready exactly when all four inputs are in, and preparing otherwise", () => {
		const answers = [null, OPENED - 1, OPENED, OPENED + 1];
		const owners = [null, "Lightspeed"];
		const termsLoading = [true, false];
		const fonts: PrintFontsState[] = ["pending", "loaded"];
		let combinations = 0;

		for (const fetchedAt of answers)
			for (const ownerName of owners)
				for (const loading of termsLoading)
					for (const font of fonts) {
						combinations++;
						const allIn =
							fetchedAt !== null &&
							fetchedAt >= OPENED &&
							ownerName !== null &&
							!loading &&
							font === "loaded";
						expect(
							printPageStateOf({
								...everythingIn,
								fetchedAt,
								ownerName,
								termsLoading: loading,
								fonts: font,
							}),
						).toBe(allIn ? "ready" : "preparing");
					}

		expect(combinations).toBe(32);
	});

	// @us-01 @slice-01 @contract-shape:unbounded-preservation
	// Holds today and must keep holding: a decision that reaches for React, the router or the cache is no longer a plain function of its inputs.
	it("keeps the decision free of React, the router and the query library", () => {
		const source = readFileSync(
			resolve(dirname(fileURLToPath(import.meta.url)), "printPageState.ts"),
			"utf8",
		);

		expect(source).toContain("printPageStateOf");
		expect(source).not.toMatch(
			/from\s+"(react|react-router|@tanstack\/react-query)(\/[^"]*)?"/,
		);
	});
});
