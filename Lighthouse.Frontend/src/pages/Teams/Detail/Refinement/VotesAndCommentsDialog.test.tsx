import { act, fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ComponentProps } from "react";
import { describe, expect, it, vi } from "vitest";
import type {
	IRefinementRow,
	ISizingLogEntry,
	ISizingVoters,
} from "../../../../models/Refinement/Refinement";
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

const JONAS = "Jonas Weber";
const ANA = "Ana Lima";
const MO = "Mo Okafor";
const CONDITION = "only if the PDF export moves to its own Work Item";
const QUESTION = "Which API version?";
const YES_IF = "Yes, if…";
const NOBODY: ISizingVoters = { yes: [], yesBut: [], no: [] };

const anEntry = (entry: Partial<ISizingLogEntry>): ISizingLogEntry => ({
	kind: "Vote",
	answer: "Yes",
	comment: null,
	voterName: JONAS,
	channel: "Web",
	recordedAt: "2026-10-07T09:00:00Z",
	isMine: false,
	isOpenQuestion: false,
	...entry,
});

const aCommentBy = (
	voterName: string,
	comment: string,
	entry: Partial<ISizingLogEntry> = {},
) => anEntry({ voterName, kind: "Comment", answer: null, comment, ...entry });

const aLogRead = (
	entries: ISizingLogEntry[],
	voters: ISizingVoters = NOBODY,
): SizingLogState => ({ status: "read", entries, voters });

const renderTheDialog = (
	overrides: Partial<ComponentProps<typeof VotesAndCommentsDialog>> = {},
) => {
	const props: ComponentProps<typeof VotesAndCommentsDialog> = {
		workItem: aWorkItem(),
		log: AN_EMPTY_LOG,
		describeFailure: vi.fn(),
		isSendingAComment: false,
		voterName: null,
		onChangeName: vi.fn(),
		onAddComment: vi.fn(),
		onClose: vi.fn(),
		...overrides,
	};
	const user = userEvent.setup();
	const view = render(<VotesAndCommentsDialog {...props} />);
	return {
		user,
		props,
		dialog: screen.getByRole("dialog"),
		showWith: (changes: Partial<typeof props>) =>
			view.rerender(<VotesAndCommentsDialog {...props} {...changes} />),
	};
};

const startAComment = async (user: ReturnType<typeof userEvent.setup>) => {
	await user.click(screen.getByRole("button", { name: "Add a comment" }));
	return screen.getByRole("textbox", { name: "Comment" });
};

describe("what the votes and comments of a Work Item list", () => {
	it("lists only what people wrote, in the order of the log, naming who, on which day, and the condition", () => {
		const { dialog } = renderTheDialog({
			log: aLogRead([
				anEntry({ voterName: JONAS, answer: "Yes" }),
				anEntry({
					voterName: ANA,
					answer: "YesBut",
					comment: CONDITION,
					isMine: true,
				}),
				anEntry({ voterName: JONAS, kind: "Revocation", answer: null }),
				aCommentBy(MO, QUESTION, { recordedAt: "2026-10-08T09:00:00Z" }),
			]),
		});

		const entries = within(dialog).getAllByRole("listitem");

		expect(entries).toHaveLength(2);
		expect(entries[0]).toHaveTextContent(`${ANA} · ${YES_IF}`);
		expect(entries[0]).toHaveTextContent(CONDITION);
		expect(entries[0]).toHaveTextContent("Wed 7 Oct");
		expect(entries[1]).toHaveTextContent(MO);
		expect(entries[1]).not.toHaveTextContent("·");
		expect(entries[1]).toHaveTextContent(QUESTION);
		expect(entries[1]).toHaveTextContent("Thu 8 Oct");
		expect(dialog).not.toHaveTextContent(JONAS);
		expect(dialog).not.toHaveTextContent(/voted|took back/);
	});

	it("marks as an open question only the entries the log calls open, also between two people sharing a name", () => {
		const { dialog } = renderTheDialog({
			log: aLogRead([
				anEntry({ voterName: JONAS, answer: "Yes", comment: CONDITION }),
				aCommentBy(JONAS, QUESTION, { isOpenQuestion: true }),
				aCommentBy(MO, CONDITION),
			]),
		});

		const entries = within(dialog).getAllByRole("listitem");

		expect(entries).toHaveLength(3);
		expect(entries[0]).not.toHaveTextContent(/open question/i);
		expect(entries[1]).toHaveTextContent(/open question/i);
		expect(entries[2]).not.toHaveTextContent(/open question/i);
	});

	it("never says where an entry came from", () => {
		const { dialog } = renderTheDialog({
			log: aLogRead([
				anEntry({ voterName: ANA, channel: "Cli", comment: CONDITION }),
				aCommentBy(JONAS, QUESTION, { channel: "Assistant" }),
				anEntry({ voterName: MO, channel: "Web", comment: CONDITION }),
			]),
		});

		const entries = within(dialog).getAllByRole("listitem");

		expect(entries).toHaveLength(3);
		for (const entry of entries) {
			expect(entry).not.toHaveTextContent(/via |command line|assistant|cli\b/i);
		}
	});

	it("shows a comment as the text it is, never as markup", () => {
		const markup = "<b>urgent</b> & <script>alert(1)</script>";
		const { dialog } = renderTheDialog({
			log: aLogRead([anEntry({ comment: markup })]),
		});

		expect(within(dialog).getByText(markup)).toBeVisible();
		expect(dialog.querySelector("b, script")).toBeNull();
	});

	it("adds nothing when a vote is taken back, even one that still carries its words", () => {
		const { dialog } = renderTheDialog({
			log: aLogRead([
				anEntry({ voterName: JONAS, answer: "YesBut", comment: CONDITION }),
				anEntry({
					voterName: JONAS,
					kind: "Revocation",
					answer: null,
					comment: "never mind",
				}),
			]),
		});

		const entries = within(dialog).getAllByRole("listitem");

		expect(entries).toHaveLength(1);
		expect(entries[0]).toHaveTextContent(CONDITION);
		expect(dialog).not.toHaveTextContent(/took back|never mind/i);
		expect(
			within(dialog).queryByRole("button", { name: /^take back/i }),
		).toBeNull();
	});

	it("says so when nobody has written anything, even when the Work Item has votes", () => {
		const { dialog } = renderTheDialog({
			log: aLogRead([anEntry({ voterName: JONAS, answer: "Yes" })], {
				yes: [JONAS],
				yesBut: [],
				no: [],
			}),
		});

		expect(within(dialog).getByText("No comments yet.")).toBeVisible();
		expect(within(dialog).queryAllByRole("listitem")).toHaveLength(0);
	});

	it("shows a reader who never voted how the votes split, who cast them, and every comment", async () => {
		const { user, dialog } = renderTheDialog({
			voterName: null,
			workItem: aWorkItem({ split: { yes: 5, yesBut: 5, no: 5 } }),
			log: aLogRead(
				[
					anEntry({ voterName: ANA, answer: "YesBut", comment: CONDITION }),
					anEntry({ voterName: MO, answer: "No" }),
					aCommentBy("Priya Sharma", QUESTION),
				],
				{ yes: [], yesBut: [ANA], no: [MO] },
			),
		});

		const entries = within(dialog).getAllByRole("listitem");

		expect(dialog).toHaveTextContent(`0 Yes · 1 ${YES_IF} · 1 No`);
		expect(entries).toHaveLength(2);
		expect(entries[0]).toHaveTextContent(CONDITION);
		expect(entries[1]).toHaveTextContent("Priya Sharma");
		expect(entries[1]).toHaveTextContent(QUESTION);
		await user.hover(within(dialog).getByText("1 No"));
		expect(await screen.findByRole("tooltip")).toHaveTextContent(MO);
	});

	it("names the people whose current vote a count in the split is, on hovering or focusing it, and none for a count of 0", async () => {
		const { user, dialog } = renderTheDialog({
			workItem: aWorkItem({ split: { yes: 1, yesBut: 0, no: 0 } }),
			log: aLogRead([], { yes: [JONAS, MO], yesBut: [ANA], no: [] }),
		});

		expect(dialog).toHaveTextContent(`2 Yes · 1 ${YES_IF} · 0 No`);
		const noCount = within(dialog).getByText("0 No");
		await user.hover(noCount);
		expect(screen.queryByRole("tooltip")).toBeNull();
		expect(noCount).not.toHaveAttribute("tabindex");

		const yesCount = within(dialog).getByText("2 Yes");
		await user.hover(yesCount);
		const yesVoters = await screen.findByRole("tooltip");
		expect(yesVoters).toHaveTextContent(JONAS);
		expect(yesVoters).toHaveTextContent(MO);
		expect(yesVoters).not.toHaveTextContent(ANA);
		expect(yesCount).toHaveAccessibleDescription(
			new RegExp(`${JONAS}\\s+${MO}`),
		);
		await user.unhover(yesCount);

		const yesIfCount = within(dialog).getByText(`1 ${YES_IF}`);
		act(() => yesIfCount.focus());
		expect(await screen.findByRole("tooltip")).toHaveTextContent(ANA);
		expect(yesIfCount).toHaveAccessibleDescription(ANA);
	});

	it("shows why the log could not be read in the words it is given", () => {
		const refusal = new Error("the server said no");
		const describeFailure = vi.fn(
			() => "That Work Item is no longer in Refinement.",
		);
		renderTheDialog({
			log: { status: "failed", error: refusal },
			describeFailure,
		});

		expect(screen.getByRole("alert")).toHaveTextContent(
			"That Work Item is no longer in Refinement.",
		);
		expect(describeFailure).toHaveBeenCalledWith(refusal);
	});

	it("shows no comments, not even the line that says there are none, while the log is still being read", () => {
		const { dialog } = renderTheDialog({ log: { status: "reading" } });

		expect(
			within(dialog).queryByText("No comments yet."),
		).not.toBeInTheDocument();
		expect(within(dialog).queryAllByRole("listitem")).toHaveLength(0);
	});
});

describe("sending a comment from the votes and comments of a Work Item", () => {
	it("takes at most 2000 characters and keeps Send disabled for a blank comment", async () => {
		const { user, props } = renderTheDialog();
		const box = await startAComment(user);
		const send = screen.getByRole("button", { name: "Send" });

		expect(box).toHaveAttribute("maxlength", "2000");
		expect(send).toBeDisabled();
		await user.type(box, "   ");
		expect(send).toBeDisabled();
		fireEvent.click(send);

		expect(props.onAddComment).not.toHaveBeenCalled();
	});

	it("sends the trimmed comment, keeps the draft until the server has taken it and clears it then", async () => {
		const { user, props } = renderTheDialog();
		await user.type(await startAComment(user), `  ${QUESTION}  `);

		await user.click(screen.getByRole("button", { name: "Send" }));

		expect(props.onAddComment).toHaveBeenCalledExactlyOnceWith(
			QUESTION,
			expect.any(Function),
		);
		expect(screen.getByRole("textbox", { name: "Comment" })).toHaveValue(
			`  ${QUESTION}  `,
		);

		const [, onSent] = vi.mocked(props.onAddComment).mock.calls[0];
		act(() => onSent());

		expect(screen.queryByRole("textbox", { name: "Comment" })).toBeNull();
		expect(screen.getByRole("button", { name: "Add a comment" })).toBeVisible();
	});

	it("disables Send and the comment box while a comment is on its way", async () => {
		const { user, showWith } = renderTheDialog();
		await user.type(await startAComment(user), QUESTION);
		expect(screen.getByRole("button", { name: "Send" })).toBeEnabled();

		showWith({ isSendingAComment: true });

		expect(screen.getByRole("button", { name: "Send" })).toBeDisabled();
		expect(screen.getByRole("textbox", { name: "Comment" })).toBeDisabled();
	});
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

	it("reads a Work Item the server sent no split for as nobody having voted while the voters are still being read", () => {
		render(
			<VotesAndCommentsDialog
				workItem={aWorkItem({ split: undefined })}
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
			"0 Yes · 0 Yes, if… · 0 No",
		);
	});

	// A count only becomes a keyboard stop once there are names behind it to show.
	it("names nobody behind a count while the voters are still being read", () => {
		render(
			<VotesAndCommentsDialog
				workItem={aWorkItem({ split: { yes: 1, yesBut: 2, no: 3 } })}
				log={{ status: "reading" }}
				describeFailure={vi.fn()}
				isSendingAComment={false}
				voterName={null}
				onChangeName={vi.fn()}
				onAddComment={vi.fn()}
				onClose={vi.fn()}
			/>,
		);

		const dialog = screen.getByRole("dialog");
		for (const count of ["1 Yes", "2 Yes, if…", "3 No"]) {
			expect(within(dialog).getByText(count)).not.toHaveAttribute("tabindex");
		}
	});
});
