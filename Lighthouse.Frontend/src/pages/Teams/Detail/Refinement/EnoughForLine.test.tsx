import type { GridRowSpacingParams } from "@mui/x-data-grid";
import { renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { type EnoughForMarking, useEnoughForLine } from "./EnoughForLine";

const needing = (high: number): EnoughForMarking => ({
	high,
	highPercentile: 85,
	terms: {
		workItem: "Work Item",
		workItems: "Work Items",
		refinement: "Refinement",
	},
});

const theRow = (index: number) =>
	({
		indexRelativeToCurrentPage: index,
		isFirstVisible: index === 0,
		isLastVisible: false,
	}) as GridRowSpacingParams;

describe("the space the grid keeps for the enough-for line", () => {
	it("moves with the number needed when the Refinement is read again", () => {
		const { result, rerender } = renderHook(
			({ marking }) => useEnoughForLine(marking),
			{ initialProps: { marking: needing(3) } },
		);

		rerender({ marking: needing(1) });

		expect(result.current.getRowSpacing?.(theRow(0))).toEqual({ bottom: 36 });
		expect(result.current.getRowSpacing?.(theRow(2))).toEqual({});
	});
});
