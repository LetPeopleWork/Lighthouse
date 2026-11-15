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
import { actAndSettle } from "../../../../tests/actAndSettle";
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
 * How the Refinement tab wires comments, conditions and questions: the condition prompt of a "Yes, if…" and
 * the name asked first, a comment sent from a Work Item's votes and comments, the log read again for the
 * Work Item now open, and the marks in the Votes and Warnings cells. The rules behind each wiring are proven
 * where they live: ConditionPrompt, VotesAndCommentsDialog, useVoteCasting, useSizingLog, useCommentAdding
 * and sizingLogWording.
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
		await user.click(
			within(prompt).getByRole("textbox", { name: "Condition" }),
		);
		await user.paste(`  ${CONDITION}  `);
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
		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
	});

	it("lets anybody add a comment without voting", async () => {
		const key = aBrowserThatVotedBefore(JONAS);
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
		expect(await within(theOpenDialog()).findByText(QUESTION)).toBeVisible();
		expect(within(theOpenDialog()).getAllByRole("listitem")).toHaveLength(1);
		const keysOfTheReads = vi
			.mocked(sizingLogService.getLog)
			.mock.calls.map(([, , voterKey]) => voterKey);
		expect(keysOfTheReads).toEqual([key, key]);
	});

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

		await actAndSettle(() => {
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

	it("says why when a Work Item's log cannot be read because the Work Item left refinement", async () => {
		terms.current = {
			...defaultRefinementTerms,
			[TERMINOLOGY_KEYS.WORK_ITEM]: "Ticket",
			[REFINEMENT_KEY]: "Grooming",
		};
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			getLog: vi
				.fn()
				.mockRejectedValue(
					new ApiError(404, "Request failed with status code 404"),
				),
		});
		const { user } = renderTheRefinementTab(
			advancedReportingWithComments(),
			sizingLogService,
		);

		await user.click(
			theButton(await theRowOf(ADVANCED_REPORTING), VOTES_AND_COMMENTS),
		);

		expect(await screen.findByRole("alert")).toHaveTextContent(
			"That ticket is no longer in grooming.",
		);
	});
});
