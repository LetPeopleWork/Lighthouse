import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IRefinementRow } from "../../../../models/Refinement/Refinement";
import type { SizingLogState } from "./useSizingLog";
import VotesAndCommentsDialog from "./VotesAndCommentsDialog";

const AN_EMPTY_LOG: SizingLogState = { status: "read", entries: [] };

const aWorkItem = (
	overrides: Partial<IRefinementRow> = {},
): IRefinementRow => ({
	referenceId: "GR-073",
	name: "Configuration management",
	url: null,
	state: "Backlog",
	parentReferenceId: "",
	voteCount: 0,
	myVote: null,
	...overrides,
});

describe("the votes and comments of one Work Item", () => {
	it("reads a Work Item the server sent no split for as nobody having voted", () => {
		render(
			<VotesAndCommentsDialog
				workItem={aWorkItem({ split: undefined })}
				log={AN_EMPTY_LOG}
				describeFailure={vi.fn()}
				isSendingAComment={false}
				voterName={null}
				onChangeName={vi.fn()}
				onAddComment={vi.fn()}
				onClose={vi.fn()}
			/>,
		);

		expect(screen.getByRole("dialog")).toHaveTextContent(
			"0 Yes · 0 Yes, if… · 0 No",
		);
	});

	it("offers no name to change to a reader without one", () => {
		render(
			<VotesAndCommentsDialog
				workItem={aWorkItem({ split: { yes: 1, yesBut: 0, no: 0 } })}
				log={AN_EMPTY_LOG}
				describeFailure={vi.fn()}
				isSendingAComment={false}
				voterName={null}
				onChangeName={vi.fn()}
				onAddComment={vi.fn()}
				onClose={vi.fn()}
			/>,
		);

		expect(screen.queryByText(/Voting as/)).toBeNull();
		expect(
			screen.queryByRole("button", { name: "Change your name" }),
		).toBeNull();
	});
});
