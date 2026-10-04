import { describe, expect, it, vi } from "vitest";
import type { RefinementStage } from "../../../../models/Refinement/Refinement";
import { createRefinementColumns } from "./refinementColumns";
import { describeDisagreement } from "./StageCell";

const columnsFor = (stagesConfigured: boolean) =>
	createRefinementColumns(
		"Work Item",
		new Map(),
		vi.fn(),
		new Set(),
		vi.fn(),
		stagesConfigured,
	);

describe("the Refinement tab's columns", () => {
	it("sorts by name, state, vote count and readiness, but not by parent or by the reader's own vote", () => {
		expect(
			columnsFor(false)
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

	it.each([
		{
			stagesConfigured: false,
			columns: [
				["parentReferenceId", "Parent"],
				["state", "State"],
				["myVote", "Your vote"],
				["voteCount", "Votes"],
				["readiness", "Readiness"],
			],
		},
		{
			stagesConfigured: true,
			columns: [
				["parentReferenceId", "Parent"],
				["state", "State"],
				["stage", "Stage"],
				["myVote", "Your vote"],
				["voteCount", "Votes"],
				["readiness", "Votes say"],
			],
		},
	])(
		"lays out the columns in order when stage rules are set: $stagesConfigured",
		({ stagesConfigured, columns }) => {
			expect(
				columnsFor(stagesConfigured)
					.slice(1)
					.map(({ field, headerName }) => [field, headerName]),
			).toEqual(columns);
		},
	);
});

describe("the vote column's header", () => {
	const voteColumn = (
		question: { question: string; tooltip: string } | undefined,
	) =>
		createRefinementColumns(
			"Work Item",
			new Map(),
			vi.fn(),
			new Set(),
			vi.fn(),
			false,
			question,
		).find(({ field }) => field === "myVote");

	it("is the yardstick question when there is one to ask, wide enough not to cut it off", () => {
		const column = voteColumn({
			question: "Doable within 12 days?",
			tooltip: "SLE 85% of work items in 12 days or less",
		});

		expect(column?.headerName).toBe("Doable within 12 days?");
		expect(column?.renderHeader).toBeDefined();
		expect(column?.minWidth).toBeGreaterThanOrEqual(220);
	});

	it("falls back to Your vote when there is no question to ask", () => {
		const column = voteColumn(undefined);

		expect(column?.headerName).toBe("Your vote");
		expect(column?.renderHeader).toBeUndefined();
	});
});

describe("the words behind the disagreement marker", () => {
	it.each([
		{
			stage: "Ready",
			words: "The stage says Ready, but the votes don't agree yet.",
		},
		{
			stage: "BeingRefined",
			words: "The votes say Ready, but the stage is still Being refined.",
		},
		{
			stage: "Waiting",
			words: "The votes say Ready, but the stage is still Waiting.",
		},
	] as { stage: RefinementStage; words: string }[])(
		"reads '$words' for a row in the $stage stage",
		({ stage, words }) => {
			expect(describeDisagreement(stage)).toBe(words);
		},
	);
});
