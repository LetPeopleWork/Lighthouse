import fc from "fast-check";
import { describe, expect, it } from "vitest";

type Edges = "none" | "left" | "right" | "both";
type OverflowEdges = (
	scrollLeft: number,
	viewportWidth: number,
	contentWidth: number,
) => Edges;

const MODULE = "./horizontalOverflow";

// A missing module must show as a failed assertion about the behaviour, not as a load error.
const loadOverflowEdges = async (): Promise<OverflowEdges> => {
	const loaded: { overflowEdges?: OverflowEdges } = await import(
		/* @vite-ignore */ MODULE
	).catch(() => ({}));
	expect(
		typeof loaded.overflowEdges,
		"the grid must decide which sides have hidden columns with an overflowEdges(scrollLeft, viewportWidth, contentWidth) function exported from horizontalOverflow, so the cue follows the scroll position; none exists yet",
	).toBe("function");
	return loaded.overflowEdges as OverflowEdges;
};

/** The sides written out from the plain comparisons, one pixel of tolerance. */
const expectedEdges = (
	scrollLeft: number,
	viewportWidth: number,
	contentWidth: number,
): Edges => {
	if (contentWidth <= viewportWidth + 1) return "none";
	const hiddenLeft = scrollLeft > 1;
	const hiddenRight = contentWidth - (scrollLeft + viewportWidth) > 1;
	if (hiddenLeft && hiddenRight) return "both";
	if (hiddenLeft) return "left";
	if (hiddenRight) return "right";
	return "none";
};

type Measures = [
	scrollLeft: number,
	viewportWidth: number,
	contentWidth: number,
];

const measures: fc.Arbitrary<Measures> = fc
	.tuple(fc.integer({ min: 0, max: 2000 }), fc.integer({ min: 0, max: 4000 }))
	.chain(([viewportWidth, contentWidth]) =>
		fc
			.integer({ min: 0, max: Math.max(0, contentWidth - viewportWidth) })
			.map((scrollLeft): Measures => [scrollLeft, viewportWidth, contentWidth]),
	);

describe("Which side of a table has hidden columns", () => {
	it("L3: names exactly the sides with hidden columns, and none when the content fits within a pixel of the viewport", async () => {
		const overflowEdges = await loadOverflowEdges();

		fc.assert(
			fc.property(measures, ([scrollLeft, viewportWidth, contentWidth]) => {
				expect(
					overflowEdges(scrollLeft, viewportWidth, contentWidth),
					`scrolled ${scrollLeft} px in a ${viewportWidth} px viewport over ${contentWidth} px of content, the cue must show hidden columns only on the side where some are, and nowhere on a table that fits`,
				).toBe(expectedEdges(scrollLeft, viewportWidth, contentWidth));
			}),
			{
				examples: [
					[[0, 400, 900]],
					[[500, 400, 900]],
					[[250, 400, 900]],
					[[0, 1000, 1000]],
					[[0, 1000, 1001]],
					[[1, 1000, 1002]],
					[[2, 1000, 1002]],
				],
			},
		);
	});

	it("shows nothing for a table that fits, even when the scroll position is left over from a wider table", async () => {
		const overflowEdges = await loadOverflowEdges();

		expect(
			overflowEdges(500, 1000, 900),
			"a window widened until everything fits must lose both cues, whatever scroll position it had before",
		).toBe("none");
	});
});
