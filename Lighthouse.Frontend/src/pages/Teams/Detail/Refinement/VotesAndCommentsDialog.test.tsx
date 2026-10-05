import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { IRefinementRow } from "../../../../models/Refinement/Refinement";
import type { SizingLogState } from "./useSizingLog";
import VotesAndCommentsDialog from "./VotesAndCommentsDialog";

const AN_EMPTY_LOG: SizingLogState = {
	status: "read",
	entries: [],
	voters: { yes: [], yesBut: [], no: [] },
};

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

	it("counts the votes from the same reading that names the voters, so the count and the names agree", async () => {
		const user = userEvent.setup();
		render(
			<VotesAndCommentsDialog
				workItem={aWorkItem({ split: { yes: 1, yesBut: 0, no: 0 } })}
				log={{
					status: "read",
					entries: [],
					voters: { yes: ["Jonas", "Mo"], yesBut: [], no: [] },
				}}
				describeFailure={vi.fn()}
				isSendingAComment={false}
				voterName={null}
				onChangeName={vi.fn()}
				onAddComment={vi.fn()}
				onClose={vi.fn()}
			/>,
		);

		const dialog = screen.getByRole("dialog");
		expect(dialog).toHaveTextContent("2 Yes · 0 Yes, if… · 0 No");
		await user.hover(within(dialog).getByText("2 Yes"));
		const yesVoters = await screen.findByRole("tooltip");
		expect(yesVoters).toHaveTextContent("Jonas");
		expect(yesVoters).toHaveTextContent("Mo");
	});

	it("shows the Work Item's own count while the voters are still being read", () => {
		render(
			<VotesAndCommentsDialog
				workItem={aWorkItem({ split: { yes: 1, yesBut: 2, no: 0 } })}
				log={{ status: "reading" }}
				describeFailure={vi.fn()}
				isSendingAComment={false}
				voterName={null}
				onChangeName={vi.fn()}
				onAddComment={vi.fn()}
				onClose={vi.fn()}
			/>,
		);

		expect(screen.getByRole("dialog")).toHaveTextContent(
			"1 Yes · 2 Yes, if… · 0 No",
		);
	});
});
