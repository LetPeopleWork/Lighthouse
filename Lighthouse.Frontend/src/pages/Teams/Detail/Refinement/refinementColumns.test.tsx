import type { GridColumnHeaderParams } from "@mui/x-data-grid";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { RefinementStage } from "../../../../models/Refinement/Refinement";
import {
	createRefinementColumns,
	type RefinementColumnsOptions,
	type RefinementGridRow,
} from "./refinementColumns";
import { describeDisagreement } from "./StageCell";

const VOTE_QUESTION = {
	question: "Doable within 12 days?",
	tooltip: "SLE 85% of work items in 12 days or less",
};

const columnsWith = (options: Partial<RefinementColumnsOptions>) =>
	createRefinementColumns({
		workItemTerm: "Work Item",
		parentMap: new Map(),
		voteQuestion: VOTE_QUESTION,
		stagesConfigured: false,
		numbersNeeded: false,
		votesBeingSent: new Set(),
		onVote: vi.fn(),
		onOpenVotes: vi.fn(),
		...options,
	});

const columnsFor = (stagesConfigured: boolean) =>
	columnsWith({ stagesConfigured });

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
				["myVote", VOTE_QUESTION.question],
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
				["myVote", VOTE_QUESTION.question],
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

describe("the # column", () => {
	// It numbers the rows as they are shown, so sorting or filtering by it would have nothing to go by.
	it("comes first, headed #, and offers no sorting, filtering or column menu", () => {
		const [first] = columnsWith({ numbersNeeded: true });

		expect(first).toMatchObject({
			field: "neededNumber",
			headerName: "#",
			sortable: false,
			filterable: false,
			disableColumnMenu: true,
		});
	});
});

describe("the vote column's header", () => {
	const voteColumn = (question: { question: string; tooltip: string }) =>
		columnsWith({ voteQuestion: question }).find(
			({ field }) => field === "myVote",
		);

	it("is the yardstick question when there is one to ask, wide enough not to cut it off", () => {
		const column = voteColumn({
			question: "Doable within 12 days?",
			tooltip: "SLE 85% of work items in 12 days or less",
		});

		expect(column?.headerName).toBe("Doable within 12 days?");
		expect(column?.renderHeader).toBeDefined();
		expect(column?.minWidth).toBeGreaterThanOrEqual(220);
	});

	it("keeps a click on its info icon from reaching the header around it", async () => {
		const column = voteColumn(VOTE_QUESTION);
		const headerClicked = vi.fn();
		render(
			// biome-ignore lint/a11y/noStaticElementInteractions: stands in for the grid's clickable column header
			// biome-ignore lint/a11y/useKeyWithClickEvents: only the click is under test
			<div onClick={headerClicked}>
				{column?.renderHeader?.(
					{} as GridColumnHeaderParams<RefinementGridRow>,
				)}
			</div>,
		);

		await userEvent.click(
			screen.getByRole("button", { name: VOTE_QUESTION.tooltip }),
		);

		expect(headerClicked).not.toHaveBeenCalled();
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
