import { act, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IRefinementRow,
	ISizingLog,
	ISizingLogEntry,
	ISizingVoters,
} from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiError } from "../../../../services/Api/ApiError";
import {
	aBrowserThatVotedBefore,
	aRow,
	aSizingLogService,
	defaultRefinementTerms,
	GRAVITY_TEAM_ID,
	gravitysRefinement,
	OPEN_QUESTION_WARNING,
	openTheVotesAndCommentsOf,
	REFINEMENT_KEY,
	renderTheRefinementTab,
	theButton,
	theRowOf,
	theStoredVoter,
	theWarningsCellOf,
	VOTES_AND_COMMENTS,
} from "../../../../tests/RefinementTabTestKit";

/**
 * Comments, conditions and questions on the Refinement tab, and the log each Work Item keeps. "Yes, if…"
 * asks for its condition and sends it with the vote; Yes and No stay one click. Anybody adds a comment
 * from a Work Item's votes and comments, without voting; from somebody without a vote it is an open
 * question, which the Work Item's Warnings cell names until they vote. A Work Item's votes and comments name who
 * holds each answer, and list what people wrote, oldest first: who, on which day, never where it came from; a vote
 * without words is not listed. Everybody reads it, voted or not.
 */

const { terms, mockUseLicenseRestrictions, reporter } = vi.hoisted(() => ({
	terms: { current: {} as Record<string, string> },
	mockUseLicenseRestrictions: vi.fn(),
	reporter: { current: vi.fn() },
}));

vi.mock("../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.current[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: mockUseLicenseRestrictions,
}));

vi.mock("../../../../services/UsageData/usageDataReporter", () => ({
	useUsageDataReporter: () => reporter.current,
}));

const ADVANCED_REPORTING = "GR-051";
const API_VERSIONING = "GR-054";
const JONAS = "Jonas Weber";
const ANA = "Ana Lima";
const MO = "Mo Okafor";
const CONDITION = "only if the PDF export moves to its own Work Item";
const QUESTION = "Which API version?";
const RECORDED = "2026-10-07T09:00:00Z";
const YES_IF = "Yes, if…";

const anEntry = (entry: Partial<ISizingLogEntry>): ISizingLogEntry => ({
	kind: "Vote",
	answer: "Yes",
	comment: null,
	voterName: JONAS,
	channel: "Web",
	recordedAt: RECORDED,
	isMine: false,
	isOpenQuestion: false,
	...entry,
});

const NOBODY: ISizingVoters = { yes: [], yesBut: [], no: [] };

const aLog = (
	entries: ISizingLogEntry[],
	voters: ISizingVoters = NOBODY,
): ISizingLog => ({ entries, voters });

const advancedReportingWithComments = () =>
	gravitysRefinement({}, [
		aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
			voteCount: 2,
			myVote: "Yes",
			hasComments: true,
		}),
		aRow(API_VERSIONING, "Public API versioning", "Analysing"),
	]);

const openTheLogOf = openTheVotesAndCommentsOf;

/** Starts a comment from the Work Item's votes and comments, wherever the comment box then opens. */
const startACommentOn = async (
	user: ReturnType<typeof renderTheRefinementTab>["user"],
	referenceId: string,
) => {
	const votes = await openTheVotesAndCommentsOf(user, referenceId);
	await user.click(
		within(votes).getByRole("button", { name: "Add a comment" }),
	);
	return await screen.findByRole("textbox", { name: "Comment" });
};

const theConditionPrompt = () => screen.findByRole("dialog", { name: YES_IF });

const theOpenDialog = () =>
	screen.getByRole("dialog", { name: VOTES_AND_COMMENTS });

/** Lets every answer already given reach the screen. */
const settle = () =>
	act(async () => {
		await new Promise((resolve) => setTimeout(resolve, 0));
	});

const aQuestionFrom = (voterName: string): ISizingLog =>
	aLog([
		anEntry({
			voterName,
			kind: "Comment",
			answer: null,
			comment: QUESTION,
			isOpenQuestion: true,
		}),
	]);

const apiVersioningAsked = () =>
	aRow(API_VERSIONING, "Public API versioning", "Analysing", {
		hasComments: true,
		hasOpenQuestion: true,
	});

describe("Comments, conditions and questions on the Refinement tab", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		reporter.current = vi.fn();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	// @us-12 @slice-12 @driving_port @contract-shape:bounded-change
	it("asks for the condition of a Yes, if… and sends it, trimmed, with the vote in one request", async () => {
		const key = aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 1,
					myVote: "YesBut",
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await user.click(theButton(await theRowOf(ADVANCED_REPORTING), YES_IF));
		const prompt = await theConditionPrompt();
		const condition = within(prompt).getByRole("textbox", {
			name: "Condition",
		});

		expect(prompt).toHaveTextContent(
			`${ADVANCED_REPORTING} Advanced reporting module`,
		);
		expect(condition).toHaveAttribute(
			"placeholder",
			"What has to be true for a Yes?",
		);
		expect(condition).toHaveAttribute("maxlength", "2000");
		expect(sizingLogService.castVote).not.toHaveBeenCalled();

		await user.click(condition);
		await user.paste(`  ${CONDITION}  `);
		await user.click(within(prompt).getByRole("button", { name: "Vote" }));

		await waitFor(() =>
			expect(sizingLogService.castVote).toHaveBeenCalledTimes(1),
		);
		expect(sizingLogService.castVote).toHaveBeenCalledWith(
			GRAVITY_TEAM_ID,
			ADVANCED_REPORTING,
			{
				answer: "YesBut",
				channel: "Web",
				voterName: ANA,
				comment: CONDITION,
			},
			key,
		);
		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
	});

	// @us-12 @slice-12 @error @contract-shape:unbounded-preservation
	it("will not cast a Yes, if… whose condition is blank", async () => {
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService();
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await user.click(theButton(await theRowOf(ADVANCED_REPORTING), YES_IF));
		const prompt = await theConditionPrompt();
		const vote = within(prompt).getByRole("button", { name: "Vote" });

		expect(vote).toBeDisabled();
		await user.type(
			within(prompt).getByRole("textbox", { name: "Condition" }),
			"   ",
		);
		expect(vote).toBeDisabled();
		expect(sizingLogService.castVote).not.toHaveBeenCalled();
	});

	// @us-12 @slice-12 @error @contract-shape:unbounded-preservation
	it("casts nothing when the condition of a Yes, if… is cancelled", async () => {
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService();
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await user.click(theButton(await theRowOf(ADVANCED_REPORTING), YES_IF));
		const prompt = await theConditionPrompt();
		await user.type(
			within(prompt).getByRole("textbox", { name: "Condition" }),
			CONDITION,
		);
		await user.click(within(prompt).getByRole("button", { name: "Cancel" }));

		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
		await settle();
		expect(sizingLogService.castVote).not.toHaveBeenCalled();
	});

	// @us-12 @slice-12 @boundary @contract-shape:bounded-change
	it("asks a browser without a name who is voting first, then the condition of its Yes, if…", async () => {
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 1,
					myVote: "YesBut",
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await user.click(theButton(await theRowOf(ADVANCED_REPORTING), YES_IF));
		const namePrompt = await screen.findByRole("dialog", {
			name: "Who is voting?",
		});
		expect(screen.getAllByRole("dialog")).toHaveLength(1);
		await user.type(
			within(namePrompt).getByRole("textbox", { name: "Your name" }),
			ANA,
		);
		await user.click(within(namePrompt).getByRole("button", { name: "Vote" }));

		const prompt = await theConditionPrompt();
		await waitFor(() => expect(screen.getAllByRole("dialog")).toHaveLength(1));
		expect(sizingLogService.castVote).not.toHaveBeenCalled();
		await user.type(
			within(prompt).getByRole("textbox", { name: "Condition" }),
			CONDITION,
		);
		await user.click(within(prompt).getByRole("button", { name: "Vote" }));

		await waitFor(() =>
			expect(sizingLogService.castVote).toHaveBeenCalledWith(
				GRAVITY_TEAM_ID,
				ADVANCED_REPORTING,
				{
					answer: "YesBut",
					channel: "Web",
					voterName: ANA,
					comment: CONDITION,
				},
				theStoredVoter()?.key,
			),
		);
		expect(sizingLogService.castVote).toHaveBeenCalledTimes(1);
	});

	// @us-12 @slice-12 @driving_port @contract-shape:bounded-change
	it("lets anybody add a comment without voting", async () => {
		const key = aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			addComment: vi.fn().mockResolvedValue(
				aRow(API_VERSIONING, "Public API versioning", "Analysing", {
					hasComments: true,
					hasOpenQuestion: true,
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(API_VERSIONING, "Public API versioning", "Analysing"),
			]),
			sizingLogService,
		);

		await user.type(await startACommentOn(user, API_VERSIONING), QUESTION);
		await user.click(screen.getByRole("button", { name: "Send" }));

		await waitFor(async () =>
			expect(
				within(await theWarningsCellOf(API_VERSIONING)).getByLabelText(
					OPEN_QUESTION_WARNING,
				),
			).toBeInTheDocument(),
		);
		expect(sizingLogService.addComment).toHaveBeenCalledWith(
			GRAVITY_TEAM_ID,
			API_VERSIONING,
			{ comment: QUESTION, channel: "Web", voterName: JONAS },
			key,
		);
		expect(sizingLogService.castVote).not.toHaveBeenCalled();
	});

	// @us-12 @slice-12 @driving_port @contract-shape:bounded-change
	it("reads the log again once a comment is sent, so the comment shows in it", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			addComment: vi.fn().mockResolvedValue(apiVersioningAsked()),
			getLog: vi
				.fn()
				.mockResolvedValueOnce(aLog([]))
				.mockResolvedValue(aQuestionFrom(JONAS)),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(API_VERSIONING, "Public API versioning", "Analysing"),
			]),
			sizingLogService,
		);

		await user.type(await startACommentOn(user, API_VERSIONING), QUESTION);
		await user.click(screen.getByRole("button", { name: "Send" }));

		expect(await within(theOpenDialog()).findByText(QUESTION)).toBeVisible();
		expect(within(theOpenDialog()).getAllByRole("listitem")).toHaveLength(1);
	});

	// @us-12 @slice-12 @error @contract-shape:unbounded-preservation
	it("shows the log of the Work Item now open when a comment sent on another one is answered late", async () => {
		aBrowserThatVotedBefore(JONAS);
		let answerTheComment: (row: IRefinementRow) => void = () => {};
		const sizingLogService = aSizingLogService({
			addComment: vi.fn().mockReturnValue(
				new Promise<IRefinementRow>((resolve) => {
					answerTheComment = resolve;
				}),
			),
			getLog: vi
				.fn()
				.mockImplementation((_teamId: number, referenceId: string) =>
					Promise.resolve(
						aQuestionFrom(referenceId === API_VERSIONING ? "Mo Okafor" : ANA),
					),
				),
		});
		const { user } = renderTheRefinementTab(
			advancedReportingWithComments(),
			sizingLogService,
		);

		await user.type(await startACommentOn(user, ADVANCED_REPORTING), QUESTION);
		await user.click(screen.getByRole("button", { name: "Send" }));
		await user.click(
			within(theOpenDialog()).getByRole("button", { name: "Close" }),
		);
		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
		const apiVersioning = await openTheLogOf(user, API_VERSIONING);
		expect(await within(apiVersioning).findByText("Mo Okafor")).toBeVisible();

		await act(async () => {
			answerTheComment(
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 2,
					myVote: "Yes",
					hasComments: true,
				}),
			);
		});
		await settle();

		expect(within(apiVersioning).getByText("Mo Okafor")).toBeVisible();
		expect(within(apiVersioning).queryByText(ANA)).toBeNull();
	});

	// @us-12 @slice-12 @boundary @contract-shape:bounded-change
	it("reads the log once more, not twice, when somebody names themselves to comment", async () => {
		const sizingLogService = aSizingLogService({
			addComment: vi.fn().mockResolvedValue(apiVersioningAsked()),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(API_VERSIONING, "Public API versioning", "Analysing"),
			]),
			sizingLogService,
		);

		await user.type(await startACommentOn(user, API_VERSIONING), QUESTION);
		await user.click(screen.getByRole("button", { name: "Send" }));
		const prompt = await screen.findByRole("dialog", {
			name: "Who is voting?",
		});
		await user.type(
			within(prompt).getByRole("textbox", { name: "Your name" }),
			JONAS,
		);
		await user.click(within(prompt).getByRole("button", { name: "Send" }));
		await waitFor(async () =>
			expect(
				within(await theWarningsCellOf(API_VERSIONING)).getByLabelText(
					OPEN_QUESTION_WARNING,
				),
			).toBeInTheDocument(),
		);
		await settle();

		expect(
			vi
				.mocked(sizingLogService.getLog)
				.mock.calls.map(([, , voterKey]) => voterKey),
		).toEqual([null, theStoredVoter()?.key]);
	});

	// @us-12 @slice-12 @error @contract-shape:bounded-change
	it("sends a comment once, however often Send is pressed while it is on its way", async () => {
		aBrowserThatVotedBefore(JONAS);
		let answerTheComment: (row: IRefinementRow) => void = () => {};
		const sizingLogService = aSizingLogService({
			addComment: vi.fn().mockReturnValue(
				new Promise<IRefinementRow>((resolve) => {
					answerTheComment = resolve;
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(API_VERSIONING, "Public API versioning", "Analysing"),
			]),
			sizingLogService,
		);

		await user.type(await startACommentOn(user, API_VERSIONING), QUESTION);
		await user.dblClick(screen.getByRole("button", { name: "Send" }));

		expect(sizingLogService.addComment).toHaveBeenCalledTimes(1);
		expect(screen.getByRole("button", { name: "Send" })).toBeDisabled();
		expect(screen.getByRole("textbox", { name: "Comment" })).toBeDisabled();

		await act(async () => {
			answerTheComment(apiVersioningAsked());
		});
		await settle();

		expect(sizingLogService.addComment).toHaveBeenCalledTimes(1);
	});

	// @us-12 @slice-12 @error @contract-shape:bounded-change
	it("keeps a refused comment so it can be sent again", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			addComment: vi.fn().mockRejectedValue(new Error("Network Error")),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(API_VERSIONING, "Public API versioning", "Analysing"),
			]),
			sizingLogService,
		);

		await user.type(await startACommentOn(user, API_VERSIONING), QUESTION);
		await user.click(screen.getByRole("button", { name: "Send" }));

		expect(
			await screen.findByRole("alert", { hidden: true }),
		).toHaveTextContent("Network Error");
		expect(screen.getByRole("textbox", { name: "Comment" })).toHaveValue(
			QUESTION,
		);
		await waitFor(() =>
			expect(screen.getByRole("button", { name: "Send" })).toBeEnabled(),
		);
	});

	// @us-12 @slice-12 @boundary @contract-shape:pure-function
	it("takes a comment of at most 2000 characters", async () => {
		aBrowserThatVotedBefore(JONAS);
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(API_VERSIONING, "Public API versioning", "Analysing"),
			]),
		);

		expect(await startACommentOn(user, API_VERSIONING)).toHaveAttribute(
			"maxlength",
			"2000",
		);
	});

	// @us-12 @slice-12 @error @contract-shape:unbounded-preservation
	it("will not send an empty comment", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService();
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(API_VERSIONING, "Public API versioning", "Analysing"),
			]),
			sizingLogService,
		);

		await user.type(await startACommentOn(user, API_VERSIONING), "   ");

		expect(screen.getByRole("button", { name: "Send" })).toBeDisabled();
		expect(sizingLogService.addComment).not.toHaveBeenCalled();
	});

	// @us-12 @slice-12 @contract-shape:pure-function
	it("marks a Work Item's comments in its Votes cell and warns of its open question in its Warnings cell, leaving the others unmarked", async () => {
		renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(API_VERSIONING, "Public API versioning", "Analysing", {
					voteCount: 3,
					hasComments: true,
					hasOpenQuestion: true,
				}),
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 2,
					hasComments: true,
				}),
				aRow("GR-060", "Audit trail", "Analysing", { voteCount: 1 }),
			]),
		);

		const asked = await theRowOf(API_VERSIONING);
		const commented = await theRowOf(ADVANCED_REPORTING);
		const quiet = await theRowOf("GR-060");

		expect(asked).toHaveTextContent("Comments");
		expect(
			within(asked).getByRole("button", {
				name: "3 votes, comments - Votes and comments",
			}),
		).toBeInTheDocument();
		expect(
			within(await theWarningsCellOf(API_VERSIONING)).getByRole("button", {
				name: OPEN_QUESTION_WARNING,
			}),
		).toBeInTheDocument();
		expect(asked).not.toHaveTextContent("Open question");
		expect(commented).toHaveTextContent("Comments");
		expect(
			within(commented).queryByLabelText(OPEN_QUESTION_WARNING),
		).not.toBeInTheDocument();
		expect(
			within(commented).getByRole("button", {
				name: "2 votes, comments - Votes and comments",
			}),
		).toBeInTheDocument();
		expect(quiet).not.toHaveTextContent("Comments");
		expect(
			within(quiet).queryByLabelText(OPEN_QUESTION_WARNING),
		).not.toBeInTheDocument();
		expect(
			within(quiet).getByRole("button", {
				name: "1 vote - Votes and comments",
			}),
		).toBeInTheDocument();
	});

	// @us-12 @slice-12 @driving_port @contract-shape:pure-function
	it("lists what people wrote oldest first, naming who and on which day, and leaves out votes without words", async () => {
		const key = aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			getLog: vi.fn().mockResolvedValue(
				aLog([
					anEntry({ voterName: JONAS, answer: "Yes" }),
					anEntry({
						voterName: ANA,
						answer: "YesBut",
						comment: CONDITION,
						isMine: true,
					}),
					anEntry({ voterName: JONAS, kind: "Revocation", answer: null }),
					anEntry({
						voterName: MO,
						kind: "Comment",
						answer: null,
						comment: QUESTION,
						isOpenQuestion: true,
						recordedAt: "2026-10-08T09:00:00Z",
					}),
				]),
			),
		});
		const { user } = renderTheRefinementTab(
			advancedReportingWithComments(),
			sizingLogService,
		);

		const dialog = await openTheLogOf(user, ADVANCED_REPORTING);
		const entries = await within(dialog).findAllByRole("listitem");

		expect(sizingLogService.getLog).toHaveBeenCalledWith(
			GRAVITY_TEAM_ID,
			ADVANCED_REPORTING,
			key,
		);
		expect(entries).toHaveLength(2);
		expect(entries[0]).toHaveTextContent(`${ANA} · ${YES_IF}`);
		expect(entries[0]).toHaveTextContent(CONDITION);
		expect(entries[0]).toHaveTextContent("Wed 7 Oct");
		expect(entries[0]).not.toHaveTextContent(/open question/i);
		expect(entries[1]).toHaveTextContent(MO);
		expect(entries[1]).not.toHaveTextContent("·");
		expect(entries[1]).toHaveTextContent(QUESTION);
		expect(entries[1]).toHaveTextContent(/open question/i);
		expect(entries[1]).toHaveTextContent("Thu 8 Oct");
		expect(dialog).not.toHaveTextContent(JONAS);
		expect(dialog).not.toHaveTextContent(/voted|took back/);
	});

	// @us-12 @slice-12 @driving_port @contract-shape:pure-function
	it("names the people whose current vote a count in the split is, on hovering or focusing it", async () => {
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			getLog: vi
				.fn()
				.mockResolvedValue(
					aLog([], { yes: [JONAS, MO], yesBut: [ANA], no: [] }),
				),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 3,
					myVote: "YesBut",
					split: { yes: 2, yesBut: 1, no: 0 },
				}),
			]),
			sizingLogService,
		);

		const dialog = await openTheLogOf(user, ADVANCED_REPORTING);
		await within(dialog).findByText("No comments yet.");
		const noCount = within(dialog).getByText("0 No");
		await user.hover(noCount);
		await settle();
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
		await waitFor(() => expect(screen.queryByRole("tooltip")).toBeNull());

		const yesIfCount = within(dialog).getByText(`1 ${YES_IF}`);
		act(() => yesIfCount.focus());
		expect(await screen.findByRole("tooltip")).toHaveTextContent(ANA);
		expect(yesIfCount).toHaveAccessibleDescription(ANA);
	});

	// @us-12 @slice-12 @boundary @contract-shape:pure-function
	it("reads the log without the browser's key for somebody signed in", async () => {
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService();
		const { user } = renderTheRefinementTab(
			gravitysRefinement({ voterIdentity: "Account" }, [
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing"),
			]),
			sizingLogService,
		);

		await openTheLogOf(user, ADVANCED_REPORTING);

		await waitFor(() =>
			expect(sizingLogService.getLog).toHaveBeenCalledWith(
				GRAVITY_TEAM_ID,
				ADVANCED_REPORTING,
				null,
			),
		);
	});

	// @us-12 @slice-12 @contract-shape:pure-function
	it("marks as open questions the entries the server calls open, even between two people sharing a name", async () => {
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			getLog: vi.fn().mockResolvedValue(
				aLog([
					anEntry({ voterName: JONAS, answer: "Yes", comment: CONDITION }),
					anEntry({
						voterName: JONAS,
						kind: "Comment",
						answer: null,
						comment: QUESTION,
						isOpenQuestion: true,
					}),
					anEntry({
						voterName: MO,
						kind: "Comment",
						answer: null,
						comment: CONDITION,
						isOpenQuestion: false,
					}),
				]),
			),
		});
		const { user } = renderTheRefinementTab(
			advancedReportingWithComments(),
			sizingLogService,
		);

		const entries = await within(
			await openTheLogOf(user, ADVANCED_REPORTING),
		).findAllByRole("listitem");

		expect(entries).toHaveLength(3);
		expect(entries[0]).not.toHaveTextContent(/open question/i);
		expect(entries[1]).toHaveTextContent(/open question/i);
		expect(entries[2]).not.toHaveTextContent(/open question/i);
	});

	// @us-12 @slice-12 @error @contract-shape:pure-function
	it("shows a comment as the text it is, never as markup", async () => {
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			getLog: vi
				.fn()
				.mockResolvedValue(
					aLog([
						anEntry({ comment: "<b>urgent</b> & <script>alert(1)</script>" }),
					]),
				),
		});
		const { user } = renderTheRefinementTab(
			advancedReportingWithComments(),
			sizingLogService,
		);

		const dialog = await openTheLogOf(user, ADVANCED_REPORTING);

		expect(
			await within(dialog).findByText(
				"<b>urgent</b> & <script>alert(1)</script>",
			),
		).toBeVisible();
		expect(dialog.querySelector("b, script")).toBeNull();
	});

	// @us-12 @us-17b @slice-12 @contract-shape:pure-function
	it("never says where an entry came from", async () => {
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			getLog: vi.fn().mockResolvedValue(
				aLog([
					anEntry({ voterName: ANA, channel: "Cli", comment: CONDITION }),
					anEntry({
						voterName: JONAS,
						kind: "Comment",
						answer: null,
						channel: "Assistant",
						comment: QUESTION,
					}),
					anEntry({ voterName: MO, channel: "Web", comment: CONDITION }),
				]),
			),
		});
		const { user } = renderTheRefinementTab(
			advancedReportingWithComments(),
			sizingLogService,
		);

		const entries = await within(
			await openTheLogOf(user, ADVANCED_REPORTING),
		).findAllByRole("listitem");

		expect(entries).toHaveLength(3);
		for (const entry of entries) {
			expect(entry).not.toHaveTextContent(/via |command line|assistant|cli\b/i);
		}
	});

	// @us-12 @us-16 @slice-16 @contract-shape:pure-function
	it("shows a vote that was taken back as taken back", async () => {
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			getLog: vi
				.fn()
				.mockResolvedValue(
					aLog([
						anEntry({ voterName: JONAS, answer: "YesBut", comment: CONDITION }),
						anEntry({ voterName: JONAS, kind: "Revocation", answer: null }),
					]),
				),
		});
		const { user } = renderTheRefinementTab(
			advancedReportingWithComments(),
			sizingLogService,
		);

		const dialog = await openTheLogOf(user, ADVANCED_REPORTING);
		const entries = await within(dialog).findAllByRole("listitem");

		expect(entries).toHaveLength(1);
		expect(entries[0]).toHaveTextContent(CONDITION);
		expect(dialog).toHaveTextContent(`0 Yes · 0 ${YES_IF} · 0 No`);
		expect(dialog).not.toHaveTextContent(/took back/i);
		expect(
			within(dialog).queryByRole("button", { name: /take back/i }),
		).toBeNull();
	});

	// @us-11 @us-12 @slice-12 @boundary @contract-shape:pure-function
	it("lets a reader who never voted read how the votes split, who cast them, and every comment", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			getLog: vi.fn().mockResolvedValue(
				aLog(
					[
						anEntry({ voterName: ANA, answer: "YesBut", comment: CONDITION }),
						anEntry({ voterName: MO, answer: "No" }),
						anEntry({
							voterName: "Priya Sharma",
							kind: "Comment",
							answer: null,
							comment: QUESTION,
						}),
					],
					{ yes: [], yesBut: [ANA], no: [MO] },
				),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 2,
					myVote: null,
					split: { yes: 0, yesBut: 1, no: 1 },
					hasComments: true,
				}),
			]),
			sizingLogService,
		);

		const dialog = await openTheLogOf(user, ADVANCED_REPORTING);
		const entries = await within(dialog).findAllByRole("listitem");

		expect(dialog).toHaveTextContent(`0 Yes · 1 ${YES_IF} · 1 No`);
		expect(entries).toHaveLength(2);
		expect(entries[0]).toHaveTextContent(CONDITION);
		expect(entries[1]).toHaveTextContent(QUESTION);
		await user.hover(within(dialog).getByText("1 No"));
		expect(await screen.findByRole("tooltip")).toHaveTextContent(MO);
	});

	// @us-12 @slice-12 @boundary @contract-shape:pure-function
	it("says so when nobody has written anything about a Work Item, even when it has votes", async () => {
		aBrowserThatVotedBefore(ANA);
		const { user } = renderTheRefinementTab(
			advancedReportingWithComments(),
			aSizingLogService({
				getLog: vi.fn().mockResolvedValue(
					aLog([anEntry({ voterName: JONAS, answer: "Yes" })], {
						yes: [JONAS],
						yesBut: [],
						no: [],
					}),
				),
			}),
		);

		const dialog = await openTheLogOf(user, ADVANCED_REPORTING);

		expect(await within(dialog).findByText("No comments yet.")).toBeVisible();
		expect(within(dialog).queryAllByRole("listitem")).toHaveLength(0);
	});

	// @us-12 @slice-12 @error @contract-shape:pure-function
	it.each([
		{
			why: "the Work Item left refinement",
			refusal: new ApiError(404, "Request failed with status code 404"),
			said: "That ticket is no longer in grooming.",
		},
		{
			why: "the log could not be read",
			refusal: new ApiError(500, "The sizing log could not be read"),
			said: "The sizing log could not be read",
		},
	])(
		"says why when a Work Item's log cannot be read because $why",
		async ({ refusal, said }) => {
			terms.current = {
				...defaultRefinementTerms,
				[TERMINOLOGY_KEYS.WORK_ITEM]: "Ticket",
				[REFINEMENT_KEY]: "Grooming",
			};
			aBrowserThatVotedBefore(ANA);
			const sizingLogService = aSizingLogService({
				getLog: vi.fn().mockRejectedValue(refusal),
			});
			const { user } = renderTheRefinementTab(
				advancedReportingWithComments(),
				sizingLogService,
			);

			await user.click(
				theButton(await theRowOf(ADVANCED_REPORTING), VOTES_AND_COMMENTS),
			);

			expect(await screen.findByRole("alert")).toHaveTextContent(said);
		},
	);
});
