import fc from "fast-check";
import { describe, expect, it } from "vitest";
import { quickSearchValues, shownTextQuickFilter } from "./quickSearch";

/**
 * What a person typing into a list's search box means: the whole typed text, trimmed, is one literal
 * piece of text, and a row stays when the text it shows in a column contains it, in any letter case.
 */

/** Whether a cell showing `shown` stays in the list when the person types `typed`. */
const staysWhenTyping = (typed: string, shown: unknown): boolean => {
	const values = quickSearchValues(typed);
	if (values.length === 0) return true;
	const test = shownTextQuickFilter(values[0]);
	return test?.(shown) ?? false;
};

const lettersDigitsSpacesAndSpecials = fc.constantFrom(
	..."abcXYZ019 ()[].*+?^$\\|:-".split(""),
);

const shownText = fc.string({
	unit: lettersDigitsSpacesAndSpecials,
	maxLength: 40,
});

const withCaseFlipped = (text: string, flips: boolean[]): string =>
	[...text]
		.map((letter, at) =>
			flips[at] ? letter.toUpperCase() : letter.toLowerCase(),
		)
		.join("");

describe("The search text and the cells it is tested against", () => {
	it("keeps a cell whose shown text contains the typed text, in any letter case", () => {
		fc.assert(
			fc.property(
				shownText,
				fc.nat(40),
				fc.nat(40),
				fc.array(fc.boolean(), { minLength: 40, maxLength: 40 }),
				(shown, start, length, flips) => {
					const typed = withCaseFlipped(
						shown.slice(start, start + length),
						flips,
					);

					expect(
						staysWhenTyping(typed, shown),
						`typing ${JSON.stringify(typed)} must keep a cell showing ${JSON.stringify(shown)}: it is a piece of that text in other letter case, and special characters count as plain letters`,
					).toBe(true);
				},
			),
			{ numRuns: 100, seed: 6711 },
		);
	});

	it("keeps a cell showing a reference ID when the ID is typed in lower case", () => {
		expect(staysWhenTyping("proj-12", "PROJ-12: Checkout")).toBe(true);
	});

	it("drops a cell whose shown text does not contain the typed text", () => {
		expect(staysWhenTyping("proj-13", "PROJ-12: Checkout")).toBe(false);
	});

	it("takes a typed special character literally", () => {
		expect(staysWhenTyping("a.c", "abc")).toBe(false);
		expect(staysWhenTyping("(", "PROJ-12 (draft)")).toBe(true);
	});

	it.each([
		["an object", { name: "Checkout" }, "obj"],
		["a list of objects", [{ a: 1 }], "object"],
		["true", true, "true"],
		["false", false, "false"],
		["nothing at all", undefined, "undefined"],
		["a null", null, "null"],
	])("never keeps a cell holding %s, whatever is typed", (_, value, typed) => {
		expect(
			staysWhenTyping(typed, value),
			`a cell holding ${JSON.stringify(value)} has no shown text, so typing ${JSON.stringify(typed)} must not keep it`,
		).toBe(false);
	});

	it("keeps a cell holding a number when its digits are typed", () => {
		expect(staysWhenTyping("12", 12)).toBe(true);
		expect(staysWhenTyping("13", 12)).toBe(false);
	});

	it("hands the grid the typed text without its surrounding spaces", () => {
		expect(quickSearchValues("  Proj-12 ")).toEqual(["Proj-12"]);
	});

	it.each(["", " ", "   \t "])(
		"hands the grid nothing for %j, so every row stays",
		(typed) => {
			expect(quickSearchValues(typed)).toEqual([]);
		},
	);

	it("never brings back a row when more text is typed, and keeps exactly the rows whose shown text contains it", () => {
		const denseText = fc.string({
			unit: fc.constantFrom("a", "b", "A", "B", " ", "(", ".", "1"),
			maxLength: 6,
		});
		fc.assert(
			fc.property(
				fc.array(denseText, { maxLength: 8 }),
				denseText,
				denseText,
				(rows, typed, more) => {
					const stayingFor = (text: string) =>
						rows.filter((row) => staysWhenTyping(text, row));
					const expectedFor = (text: string) =>
						rows.filter((row) =>
							row.toLowerCase().includes(text.trim().toLowerCase()),
						);

					expect(
						stayingFor(typed),
						`typing ${JSON.stringify(typed)} must keep exactly the rows containing it (or every row when it is blank)`,
					).toEqual(expectedFor(typed));
					const wider = stayingFor(typed);
					for (const row of stayingFor(typed + more)) {
						expect(
							wider,
							`typing ${JSON.stringify(typed + more)} kept ${JSON.stringify(row)}, which typing ${JSON.stringify(typed)} had already dropped`,
						).toContain(row);
					}
				},
			),
			{ numRuns: 100, seed: 6711 },
		);
	});
});
