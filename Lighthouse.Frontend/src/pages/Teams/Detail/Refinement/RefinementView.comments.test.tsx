import { act, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IRefinementRow,
	ISizingLog,
	ISizingLogEntry,
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
 * records in one click like Yes and No; its condition is a comment like any other. Anybody adds a comment
 * from a Work Item's votes and comments, without voting; from somebody without a vote it is an open
 * question, which the Work Item's Warnings cell names until they vote. Each Work Item's log reads oldest first and says
 * who said what and on which day, never where it came from. Everybody reads it, voted or not.
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

const theOpenDialog = () =>
	screen.getByRole("dialog", { name: VOTES_AND_COMMENTS });

/** Lets every answer already given reach the screen. */
const settle = () =>
	act(async () => {
		await new Promise((resolve) => setTimeout(resolve, 0));
	});

const aQuestionFrom = (voterName: string): ISizingLog => ({
	entries: [
		anEntry({
			voterName,
			kind: "Comment",
			answer: null,
			comment: QUESTION,
			isOpenQuestion: true,
		}),
	],
});

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
	it("records Yes, if… in one click, like Yes and No, with no condition asked", async () => {
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

		await waitFor(() =>
			expect(sizingLogService.castVote).toHaveBeenCalledTimes(1),
		);
		const [teamId, referenceId, vote, voterKey] = vi.mocked(
			sizingLogService.castVote,
		).mock.calls[0];
		expect([teamId, referenceId, voterKey]).toEqual([
			GRAVITY_TEAM_ID,
			ADVANCED_REPORTING,
			key,
		]);
		expect(vote).toStrictEqual({
			answer: "YesBut",
			channel: "Web",
			voterName: ANA,
		});
		expect(vote).not.toHaveProperty("comment");
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
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
				.mockResolvedValueOnce({ entries: [] } satisfies ISizingLog)
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
	it("opens a Work Item's log oldest first, naming who said what and on which day", async () => {
		const key = aBrowserThatVotedBefore(ANA);
		const log: ISizingLog = {
			entries: [
				anEntry({ voterName: JONAS, answer: "Yes" }),
				anEntry({
					voterName: ANA,
					answer: "YesBut",
					comment: CONDITION,
					isMine: true,
				}),
				anEntry({
					voterName: "Mo Okafor",
					kind: "Comment",
					answer: null,
					comment: QUESTION,
					isOpenQuestion: true,
				}),
			],
		};
		const sizingLogService = aSizingLogService({
			getLog: vi.fn().mockResolvedValue(log),
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
		expect(entries).toHaveLength(3);
		expect(entries[0]).toHaveTextContent(`${JONAS} voted Yes`);
		expect(entries[0]).toHaveTextContent("Wed 7 Oct");
		expect(entries[0]).not.toHaveTextContent(/open question/i);
		expect(entries[1]).toHaveTextContent(`${ANA} voted ${YES_IF}`);
		expect(entries[1]).toHaveTextContent(CONDITION);
		expect(entries[1]).not.toHaveTextContent(/open question/i);
		expect(entries[2]).toHaveTextContent("Mo Okafor");
		expect(entries[2]).toHaveTextContent(QUESTION);
		expect(entries[2]).toHaveTextContent(/open question/i);
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
			getLog: vi.fn().mockResolvedValue({
				entries: [
					anEntry({ voterName: JONAS, answer: "Yes" }),
					anEntry({
						voterName: JONAS,
						kind: "Comment",
						answer: null,
						comment: QUESTION,
						isOpenQuestion: true,
					}),
					anEntry({
						voterName: "Mo Okafor",
						kind: "Comment",
						answer: null,
						comment: CONDITION,
						isOpenQuestion: false,
					}),
				],
			} satisfies ISizingLog),
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
			getLog: vi.fn().mockResolvedValue({
				entries: [
					anEntry({ comment: "<b>urgent</b> & <script>alert(1)</script>" }),
				],
			} satisfies ISizingLog),
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
			getLog: vi.fn().mockResolvedValue({
				entries: [
					anEntry({ voterName: ANA, channel: "Cli" }),
					anEntry({ voterName: JONAS, channel: "Assistant" }),
					anEntry({ voterName: "Mo Okafor", channel: "Web" }),
				],
			} satisfies ISizingLog),
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
	it.skip("shows a vote that was taken back as taken back", async () => {
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			getLog: vi.fn().mockResolvedValue({
				entries: [
					anEntry({ voterName: JONAS, answer: "Yes" }),
					anEntry({ voterName: JONAS, kind: "Revocation", answer: null }),
				],
			} satisfies ISizingLog),
		});
		const { user } = renderTheRefinementTab(
			advancedReportingWithComments(),
			sizingLogService,
		);

		const entries = await within(
			await openTheLogOf(user, ADVANCED_REPORTING),
		).findAllByRole("listitem");

		expect(entries[1]).toHaveTextContent(`${JONAS} took back their vote`);
	});

	// @us-11 @us-12 @slice-12 @boundary @contract-shape:pure-function
	it("lets a reader who never voted read how the votes split and every comment", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			getLog: vi.fn().mockResolvedValue({
				entries: [
					anEntry({ voterName: ANA, answer: "YesBut", comment: CONDITION }),
					anEntry({ voterName: "Mo Okafor", answer: "No" }),
					anEntry({
						voterName: "Priya Sharma",
						kind: "Comment",
						answer: null,
						comment: QUESTION,
					}),
				],
			} satisfies ISizingLog),
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

		expect(dialog).toHaveTextContent("0 Yes · 1 Yes, if… · 1 No");
		expect(entries).toHaveLength(3);
		expect(entries[0]).toHaveTextContent(CONDITION);
		expect(entries[2]).toHaveTextContent(QUESTION);
	});

	// @us-12 @slice-12 @boundary @contract-shape:pure-function
	it("says so when a Work Item's log is empty", async () => {
		aBrowserThatVotedBefore(ANA);
		const { user } = renderTheRefinementTab(
			advancedReportingWithComments(),
			aSizingLogService(),
		);

		const dialog = await openTheLogOf(user, ADVANCED_REPORTING);

		expect(
			await within(dialog).findByText("No votes or comments yet."),
		).toBeVisible();
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
