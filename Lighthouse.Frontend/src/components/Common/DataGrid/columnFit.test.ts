import fc from "fast-check";
import { describe, expect, it } from "vitest";
import { DEFAULT_TEXT_MIN_WIDTH, fitColumn } from "./columnFit";
import type { DataGridColumn } from "./types";

type Row = { id: number };

const aColumn = (shape: Partial<DataGridColumn<Row>>): DataGridColumn<Row> => ({
	field: "column",
	headerName: "Column",
	...shape,
});

const notFlex = (flex: number | undefined) => flex ?? 0;

describe("A number, date or action column is never cut", () => {
	type StoredCase =
		| { tag: "absent" }
		| { tag: "below"; below: number }
		| { tag: "atOrAbove"; above: number };

	const storedFor = (declared: number, stored: StoredCase) => {
		if (stored.tag === "absent") return undefined;
		if (stored.tag === "below") return Math.max(1, declared - stored.below);
		return Math.min(800, declared + stored.above);
	};

	const storedCase: fc.Arbitrary<StoredCase> = fc.oneof(
		fc.constant<StoredCase>({ tag: "absent" }),
		fc
			.integer({ min: 1, max: 399 })
			.map((below) => ({ tag: "below", below }) as const),
		fc
			.integer({ min: 0, max: 760 })
			.map((above) => ({ tag: "atOrAbove", above }) as const),
	);

	it("keeps width and minWidth at or above the declared width, whatever width is stored, including one below it", () => {
		fc.assert(
			fc.property(
				fc.integer({ min: 40, max: 400 }),
				storedCase,
				fc.constantFrom("content" as const, "actions" as const),
				(declared, stored, kind) => {
					const fitted = fitColumn(
						aColumn({ width: declared, fit: { kind } }),
						storedFor(declared, stored),
					);
					expect(
						fitted.width,
						`a ${kind} column declared ${declared} px got width ${fitted.width}; such a column must show its whole content, so fitColumn may never size it below the declared width`,
					).toBeGreaterThanOrEqual(declared);
					expect(
						fitted.minWidth,
						`a ${kind} column declared ${declared} px got minWidth ${fitted.minWidth}; the grid shrinks it to minWidth, so minWidth must be at least the declared width`,
					).toBeGreaterThanOrEqual(declared);
				},
			),
		);
	});

	it("uses a content width stored above the declared width as stored, and the declared width when the stored one is smaller", () => {
		const content = aColumn({ width: 160, fit: { kind: "content" } });

		expect(
			fitColumn(content, 240).width,
			"a person widened a date column to 240 px; that width is theirs and must be used",
		).toBe(240);
		expect(
			fitColumn(content, 90).width,
			"a stored 90 px would cut a 160 px date; the declared width must win",
		).toBe(160);
		expect(
			notFlex(fitColumn(content, undefined).flex),
			"a content column must never share leftover width as flex",
		).toBe(0);
	});

	it("keeps the actions column at its declared width, ignores a stored width and offers no resize", () => {
		const fitted = fitColumn(
			aColumn({ width: 150, fit: { kind: "actions" } }),
			400,
		);

		expect(
			fitted.width,
			"the pinned actions column must ignore a stored width and keep its declared 150 px",
		).toBe(150);
		expect(
			fitted.resizable,
			"the pinned actions column must not be resizable, so nobody can clip Delete",
		).toBe(false);
	});
});

describe("A text column shrinks to its minimum unless the person set its width", () => {
	const textColumn = fc.record({
		minWidth: fc.option(fc.integer({ min: 40, max: 300 }), { nil: undefined }),
		flex: fc.option(fc.integer({ min: 1, max: 3 }), { nil: undefined }),
	});

	it("shares the leftover width as flex with its declared or default minimum when no width is stored", () => {
		fc.assert(
			fc.property(textColumn, ({ minWidth, flex }) => {
				const fitted = fitColumn(
					aColumn({ flex, fit: { kind: "text", minWidth } }),
					undefined,
				);
				expect(
					fitted.flex,
					"a text column without a stored width must take a flex share (its own flex, or 1)",
				).toBe(flex ?? 1);
				expect(
					fitted.minWidth,
					"a text column must not shrink below its declared minimum, or 120 px when none is declared",
				).toBe(minWidth ?? 120);
			}),
		);
	});

	it("uses a stored width as set, with flex off and the minimum kept", () => {
		fc.assert(
			fc.property(
				textColumn,
				fc.integer({ min: 40, max: 800 }),
				({ minWidth, flex }, stored) => {
					const fitted = fitColumn(
						aColumn({ flex, fit: { kind: "text", minWidth } }),
						stored,
					);
					expect(
						fitted.width,
						`the person set ${stored} px; reopening the grid must show exactly that width`,
					).toBe(stored);
					expect(
						notFlex(fitted.flex),
						"flex left on a resized column lets the grid replace the person's width with a share",
					).toBe(0);
					expect(
						fitted.minWidth,
						"the minimum of a resized text column stays as declared, or 120 px",
					).toBe(minWidth ?? 120);
				},
			),
		);
	});

	it("treats a column that declares no fit as text when it is flex and as content otherwise", () => {
		const derivedText = fitColumn(aColumn({ flex: 1 }), undefined);
		const derivedContent = fitColumn(aColumn({ width: 200 }), undefined);

		expect(
			DEFAULT_TEXT_MIN_WIDTH,
			"the default text minimum agreed in the sketch is 120 px",
		).toBe(120);
		expect(
			derivedText.flex,
			"a flex column with no fit is a text column and keeps its flex",
		).toBe(1);
		expect(
			derivedText.minWidth,
			"a flex column with no fit shrinks no further than 120 px",
		).toBe(120);
		expect(
			derivedContent.width,
			"a fixed-width column with no fit is content and keeps its width",
		).toBe(200);
		expect(
			derivedContent.minWidth,
			"a fixed-width column with no fit never shrinks below its width",
		).toBe(200);
		expect(
			notFlex(derivedContent.flex),
			"a fixed-width column with no fit never becomes flex",
		).toBe(0);
	});
});
