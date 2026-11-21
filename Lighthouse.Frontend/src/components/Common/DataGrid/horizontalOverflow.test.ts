import fc from "fast-check";
import { describe, expect, it } from "vitest";
import { type OverflowEdges, overflowEdges } from "./horizontalOverflow";

type Row = [
	scrollLeft: number,
	viewportWidth: number,
	contentWidth: number,
	hidden: OverflowEdges,
];

// Written from the rule in words: a side has hidden columns when more than one pixel of the table lies
// beyond that edge of the viewport; a table at most one pixel wider than its viewport fits.
const worked: Row[] = [
	[0, 400, 900, "right"],
	[250, 400, 900, "both"],
	[500, 400, 900, "left"],
	[1, 400, 900, "right"],
	[2, 400, 900, "both"],
	[498, 400, 900, "both"],
	[499, 400, 900, "left"],
	[0, 1000, 1000, "none"],
	[0, 1000, 1001, "none"],
	[0, 1000, 1002, "right"],
	[1, 1000, 1002, "none"],
	[2, 1000, 1002, "left"],
	[500, 1000, 900, "none"],
];

const hasLeft = (edges: OverflowEdges) => edges === "left" || edges === "both";
const hasRight = (edges: OverflowEdges) =>
	edges === "right" || edges === "both";
const mirrored = (edges: OverflowEdges): OverflowEdges => {
	if (edges === "left") return "right";
	if (edges === "right") return "left";
	return edges;
};

const viewport = fc.integer({ min: 0, max: 2000 });
const overflowing = fc
	.tuple(viewport, fc.integer({ min: 2, max: 2000 }))
	.chain(([viewportWidth, extra]) =>
		fc
			.integer({ min: 0, max: extra })
			.map((scrollLeft) => ({ scrollLeft, viewportWidth, extra })),
	);

describe("Which side of a table has hidden columns", () => {
	it.each(worked)(
		"scrolled %i px in a %i px viewport over %i px of content, hidden columns are: %s",
		(scrollLeft, viewportWidth, contentWidth, hidden) => {
			expect(
				overflowEdges(scrollLeft, viewportWidth, contentWidth),
				`the cue must name "${hidden}" here: it shows only the sides with more than a pixel of table beyond the edge, and nothing for a table that fits`,
			).toBe(hidden);
		},
	);

	it("never names a side for a table that fits, wherever the scroll position was left", () => {
		fc.assert(
			fc.property(
				viewport,
				fc.integer({ min: 0, max: 1 }),
				fc.integer({ min: 0, max: 3000 }),
				(viewportWidth, slack, scrollLeft) => {
					expect(
						overflowEdges(scrollLeft, viewportWidth, viewportWidth + slack),
						"a window widened until everything fits must lose every cue",
					).toBe("none");
				},
			),
		);
	});

	it("shows no left cue at the start and no right cue at the end of the range", () => {
		fc.assert(
			fc.property(overflowing, ({ viewportWidth, extra }) => {
				const content = viewportWidth + extra;
				expect(
					hasLeft(overflowEdges(0, viewportWidth, content)),
					"at the left edge nothing is hidden on the left",
				).toBe(false);
				expect(
					hasRight(overflowEdges(extra, viewportWidth, content)),
					"at the end of the range nothing is hidden on the right",
				).toBe(false);
			}),
		);
	});

	it("names both sides when the table is scrolled clear of both ends", () => {
		fc.assert(
			fc.property(
				viewport,
				fc.integer({ min: 5, max: 2000 }),
				fc.nat(),
				(viewportWidth, extra, pick) => {
					const scrollLeft = 2 + (pick % (extra - 3));
					expect(
						overflowEdges(scrollLeft, viewportWidth, viewportWidth + extra),
						"with columns beyond both edges, both cues show",
					).toBe("both");
				},
			),
		);
	});

	it("treats the two edges alike: scrolling to the mirror position swaps the sides", () => {
		fc.assert(
			fc.property(overflowing, ({ scrollLeft, viewportWidth, extra }) => {
				const content = viewportWidth + extra;
				expect(
					overflowEdges(extra - scrollLeft, viewportWidth, content),
					"a table scrolled as far from the end as another is from the start must show the opposite side",
				).toBe(mirrored(overflowEdges(scrollLeft, viewportWidth, content)));
			}),
		);
	});

	it("shows a left cue once it has started and keeps it, and a right cue until it ends and never brings it back", () => {
		fc.assert(
			fc.property(overflowing, fc.nat(), ({ viewportWidth, extra }, pick) => {
				const content = viewportWidth + extra;
				const earlier = pick % (extra + 1);
				const later = earlier + (pick % (extra - earlier + 1));
				const before = overflowEdges(earlier, viewportWidth, content);
				const after = overflowEdges(later, viewportWidth, content);
				expect(
					!hasLeft(before) || hasLeft(after),
					"scrolling further right never hides the left cue again",
				).toBe(true);
				expect(
					!hasRight(after) || hasRight(before),
					"scrolling further right never brings the right cue back",
				).toBe(true);
			}),
		);
	});
});
