import { renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { RowInWholeList } from "../../../../components/Common/DataGrid/types";
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

const theRow = (index: number): RowInWholeList => ({
	index,
	isLastShown: false,
});

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

	it.each([
		[20, {}],
		[131, { bottom: 36 }],
	])(
		"the 131st row of the whole list keeps space for the line only when 131 are needed (with %i needed keeps %o)",
		(high, space) => {
			const { result } = renderHook(() => useEnoughForLine(needing(high)));

			expect(result.current.getRowSpacing?.(theRow(130))).toEqual(space);
		},
	);
});
