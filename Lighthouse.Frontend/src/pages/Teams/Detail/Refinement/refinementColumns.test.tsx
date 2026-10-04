import { describe, expect, it, vi } from "vitest";
import { createRefinementColumns } from "./refinementColumns";

describe("the Refinement tab's columns", () => {
	it("sorts by name, state, vote count and readiness, but not by parent or by the reader's own vote", () => {
		const columns = createRefinementColumns(
			"Work Item",
			new Map(),
			vi.fn(),
			new Set(),
			vi.fn(),
		);

		expect(
			columns
				.slice(1)
				.map(({ field, sortable }) => ({ field, sortable: sortable ?? true })),
		).toEqual([
			{ field: "parentReferenceId", sortable: false },
			{ field: "state", sortable: true },
			{ field: "myVote", sortable: false },
			{ field: "voteCount", sortable: true },
			{ field: "readiness", sortable: true },
		]);
	});
});
