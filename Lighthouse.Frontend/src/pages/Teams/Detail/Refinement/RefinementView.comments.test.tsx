import { screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	ISizingLog,
	ISizingLogEntry,
} from "../../../../models/Refinement/Refinement";
import {
	aBrowserThatVotedBefore,
	aRow,
	aSizingLogService,
	defaultRefinementTerms,
	GRAVITY_TEAM_ID,
	gravitysRefinement,
	renderTheRefinementTab,
	theButton,
	theRowOf,
} from "../../../../tests/RefinementTabTestKit";

/**
 * Comments, conditions and questions on the Refinement tab, and the log each Work Item keeps. Choosing
 * "Yes, but…" asks for the condition but does not insist on it. A question can be asked without voting;
 * it marks the Work Item until the asker votes. Each Work Item's log reads oldest first and says who
 * said what, on which day and, for the command line or an assistant, through which. Until you vote, the
 * log says only how many have voted.
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
const VOTES_AND_COMMENTS = "Votes and comments";

const anEntry = (entry: Partial<ISizingLogEntry>): ISizingLogEntry => ({
	kind: "Vote",
	answer: "Yes",
	comment: null,
	voterName: JONAS,
	channel: "Web",
	recordedAt: RECORDED,
	isMine: false,
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

const openTheLogOf = async (
	user: ReturnType<typeof renderTheRefinementTab>["user"],
	referenceId: string,
) => {
	await user.click(theButton(await theRowOf(referenceId), VOTES_AND_COMMENTS));
	return await screen.findByRole("dialog");
};

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
	it.skip("asks for the condition when Yes, but… is chosen and sends it with the vote", async () => {
		const key = aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 1,
					myVote: "YesBut",
					hasComments: true,
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await user.click(
			theButton(await theRowOf(ADVANCED_REPORTING), "Yes, but…"),
		);
		const prompt = await screen.findByRole("dialog");
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
				key,
			),
		);
	});

	// @us-12 @slice-12 @boundary @contract-shape:bounded-change
	it.skip("still takes a Yes, but… whose condition is left empty", async () => {
		aBrowserThatVotedBefore(ANA);
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

		await user.click(
			theButton(await theRowOf(ADVANCED_REPORTING), "Yes, but…"),
		);
		await user.click(
			within(await screen.findByRole("dialog")).getByRole("button", {
				name: "Vote",
			}),
		);

		await waitFor(() =>
			expect(sizingLogService.castVote).toHaveBeenCalledTimes(1),
		);
		const [, , vote] = vi.mocked(sizingLogService.castVote).mock.calls[0];
		expect(vote).toEqual({ answer: "YesBut", channel: "Web", voterName: ANA });
	});

	// @us-12 @slice-12 @driving_port @contract-shape:bounded-change
	it.skip("lets a voter ask a question without voting", async () => {
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

		await user.click(
			theButton(await theRowOf(API_VERSIONING), "Ask a question"),
		);
		const prompt = await screen.findByRole("dialog");
		await user.type(
			within(prompt).getByRole("textbox", { name: "Question" }),
			QUESTION,
		);
		await user.click(within(prompt).getByRole("button", { name: "Send" }));

		await waitFor(() =>
			expect(screen.getByText("Open question")).toBeVisible(),
		);
		expect(sizingLogService.addComment).toHaveBeenCalledWith(
			GRAVITY_TEAM_ID,
			API_VERSIONING,
			{ comment: QUESTION, channel: "Web", voterName: JONAS },
			key,
		);
		expect(sizingLogService.castVote).not.toHaveBeenCalled();
	});

	// @us-12 @slice-12 @error @contract-shape:unbounded-preservation
	it.skip("will not send an empty question", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService();
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(API_VERSIONING, "Public API versioning", "Analysing"),
			]),
			sizingLogService,
		);

		await user.click(
			theButton(await theRowOf(API_VERSIONING), "Ask a question"),
		);
		const prompt = await screen.findByRole("dialog");
		await user.type(
			within(prompt).getByRole("textbox", { name: "Question" }),
			"   ",
		);

		expect(within(prompt).getByRole("button", { name: "Send" })).toBeDisabled();
		expect(sizingLogService.addComment).not.toHaveBeenCalled();
	});

	// @us-12 @slice-12 @contract-shape:pure-function
	it.skip("marks a Work Item with an open question and leaves the others unmarked", async () => {
		renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(API_VERSIONING, "Public API versioning", "Analysing", {
					hasComments: true,
					hasOpenQuestion: true,
				}),
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing"),
			]),
		);

		expect(await theRowOf(API_VERSIONING)).toHaveTextContent("Open question");
		expect(await theRowOf(ADVANCED_REPORTING)).not.toHaveTextContent(
			"Open question",
		);
	});

	// @us-12 @slice-12 @driving_port @contract-shape:pure-function
	it.skip("opens a Work Item's log oldest first, naming who said what and on which day", async () => {
		const key = aBrowserThatVotedBefore(ANA);
		const log: ISizingLog = {
			hidden: false,
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
		expect(entries[0]).toHaveTextContent(JONAS);
		expect(entries[0]).toHaveTextContent("Yes");
		expect(entries[0]).toHaveTextContent("Wed 7 Oct");
		expect(entries[1]).toHaveTextContent(ANA);
		expect(entries[1]).toHaveTextContent("Yes, but…");
		expect(entries[1]).toHaveTextContent(CONDITION);
		expect(entries[2]).toHaveTextContent("Mo Okafor");
		expect(entries[2]).toHaveTextContent(QUESTION);
	});

	// @us-12 @slice-12 @error @contract-shape:pure-function
	it.skip("shows a comment as the text it is, never as markup", async () => {
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			getLog: vi.fn().mockResolvedValue({
				hidden: false,
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
	it.skip("says when a vote came through the command line or an assistant", async () => {
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			getLog: vi.fn().mockResolvedValue({
				hidden: false,
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

		expect(entries[0]).toHaveTextContent("via the command line");
		expect(entries[1]).toHaveTextContent("via an assistant");
		expect(entries[2]).not.toHaveTextContent(/via /);
	});

	// @us-12 @us-16 @slice-16 @contract-shape:pure-function
	it.skip("shows a vote that was taken back as taken back", async () => {
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			getLog: vi.fn().mockResolvedValue({
				hidden: false,
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

	// @us-14 @slice-14 @contract-shape:pure-function
	it.skip("before the reader votes, the log says only how many have voted", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			getLog: vi
				.fn()
				.mockResolvedValue({ hidden: true, voteCount: 3 } satisfies ISizingLog),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 3,
					hasComments: true,
				}),
			]),
			sizingLogService,
		);

		const dialog = await openTheLogOf(user, ADVANCED_REPORTING);

		expect(
			await within(dialog).findByText(
				"Vote first to see the 3 votes and their comments",
			),
		).toBeVisible();
		expect(within(dialog).queryAllByRole("listitem")).toHaveLength(0);
	});

	// @us-12 @slice-12 @error @contract-shape:pure-function
	it.skip("says why when a Work Item's log cannot be read", async () => {
		aBrowserThatVotedBefore(ANA);
		const sizingLogService = aSizingLogService({
			getLog: vi
				.fn()
				.mockRejectedValue(
					new Error("That Work Item is no longer in refinement"),
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
			"That Work Item is no longer in refinement",
		);
	});
});
