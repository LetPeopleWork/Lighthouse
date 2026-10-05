import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IRefinementRow,
	IVotedRow,
} from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiError } from "../../../../services/Api/ApiError";
import {
	aBrowserThatVotedBefore,
	aNeedOfFiveToEight,
	aRow,
	aSizingLogService,
	defaultRefinementTerms,
	GRAVITY_TEAM_ID,
	gravitysRefinement,
	openTheVotesAndCommentsOf,
	REFINEMENT_KEY,
	renderTheRefinementTab,
	THURSDAY_THE_EIGHTH,
	theButton,
	theRowOf,
	theStoredVoter,
	VOTER_STORAGE_KEY,
} from "../../../../tests/RefinementTabTestKit";

/**
 * Casting, seeing and taking back a sizing vote on the Refinement tab. Every row in refinement offers
 * Yes, "Yes, if…" and No in its own column; once a name is known one click casts a Yes or a No, while a
 * "Yes, if…" first asks for its condition. Without
 * sign-in the first vote asks who is voting, and the browser keeps that name and a random key so later
 * votes need no name and stay this browser's. With sign-in nobody is asked anything. The grid shows how
 * many have voted; clicking that opens the votes and comments, where everybody, voted or not, reads how
 * the votes split, and where a voter changes their name or takes back their own vote.
 */

const { terms, mockUseLicenseRestrictions, reporter } = vi.hoisted(() => ({
	terms: { current: {} as Record<string, string> },
	mockUseLicenseRestrictions: vi.fn(),
	reporter: { current: vi.fn() },
}));

// Like the real provider, a render hands out a lookup over the words as they stood then, so a callback
// that held on to an earlier lookup still speaks the earlier words.
vi.mock("../../../../services/TerminologyContext", () => ({
	useTerminology: () => {
		const wordsNow = { ...terms.current };
		return {
			getTerm: (key: string) => wordsNow[key] ?? key,
			isLoading: false,
			error: null,
			refetchTerminology: () => {},
		};
	},
}));

vi.mock("../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: mockUseLicenseRestrictions,
}));

vi.mock("../../../../services/UsageData/usageDataReporter", () => ({
	useUsageDataReporter: () => reporter.current,
}));

const CONFIGURATION_MANAGEMENT = "GR-073";
const ADVANCED_REPORTING = "GR-051";
const JONAS = "Jonas Weber";
const YES_IF = "Yes, if…";
const TAKE_BACK = "Take back my vote";
const ONE_VOTE = "1 vote";
const VOTE_CAST = "TeamSizingVoteCast";

const theRowAfter = (
	referenceId: string,
	votes: Partial<IRefinementRow>,
): IRefinementRow =>
	aRow(referenceId, "Configuration management", "Backlog", votes);

const A_HEX_KEY = /^[\da-f]{64}$/;

const theNamePrompt = () =>
	screen.findByRole("dialog", { name: "Who is voting?" });

const closeTheVotesAndComments = async (
	user: ReturnType<typeof renderTheRefinementTab>["user"],
	dialog: HTMLElement,
) => {
	await user.click(within(dialog).getByRole("button", { name: "Close" }));
	await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
};

const voteUnderTheName = async (
	user: ReturnType<typeof renderTheRefinementTab>["user"],
	referenceId: string,
	answer: string,
	name: string,
) => {
	await user.click(theButton(await theRowOf(referenceId), answer));
	const prompt = await theNamePrompt();
	await user.type(
		within(prompt).getByRole("textbox", { name: "Your name" }),
		name,
	);
	await user.click(within(prompt).getByRole("button", { name: "Vote" }));
};

describe("A voter casts a sizing vote from the list", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		reporter.current = vi.fn();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	afterEach(() => {
		vi.unstubAllGlobals();
	});

	// @us-11 @slice-11 @driving_port @contract-shape:pure-function
	it("offers Yes, Yes, if… and No on every Work Item in refinement, beside how many have voted", async () => {
		renderTheRefinementTab(gravitysRefinement());

		for (const referenceId of [
			"GR-058",
			ADVANCED_REPORTING,
			CONFIGURATION_MANAGEMENT,
		]) {
			const row = await theRowOf(referenceId);
			expect(theButton(row, "Yes")).toBeEnabled();
			expect(theButton(row, YES_IF)).toBeEnabled();
			expect(theButton(row, "No")).toBeEnabled();
			expect(row).toHaveTextContent("No votes");
		}
		expect(
			screen.getByRole("columnheader", { name: /^Doable within 7 days\?/ }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("columnheader", { name: "Votes" }),
		).toBeInTheDocument();
	});

	// @us-11 @slice-11 @driving_port @kpi-OUT-5510-K4-votes-outside-the-meeting @contract-shape:bounded-change
	it("asks for a name the first time, then casts the vote under it and shows it as the voter's own", async () => {
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				theRowAfter(CONFIGURATION_MANAGEMENT, {
					voteCount: 1,
					myVote: "Yes",
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);
		const prompt = await theNamePrompt();
		expect(
			within(prompt).getByText("Kept in this browser only."),
		).toBeVisible();
		expect(
			within(prompt).getByRole("button", { name: "Cancel" }),
		).toBeEnabled();
		await user.type(
			within(prompt).getByRole("textbox", { name: "Your name" }),
			JONAS,
		);
		await user.click(within(prompt).getByRole("button", { name: "Vote" }));

		const row = await theRowOf(CONFIGURATION_MANAGEMENT);
		await waitFor(() => expect(row).toHaveTextContent(ONE_VOTE));
		expect(sizingLogService.castVote).toHaveBeenCalledWith(
			GRAVITY_TEAM_ID,
			CONFIGURATION_MANAGEMENT,
			{ answer: "Yes", channel: "Web", voterName: JONAS },
			theStoredVoter()?.key,
		);
		expect(theButton(row, "Yes")).toHaveAttribute("aria-pressed", "true");
		expect(theButton(row, "No")).toHaveAttribute("aria-pressed", "false");
	});

	// @us-11 @slice-11 @contract-shape:bounded-change
	it("remembers the name, so the next vote takes one click and comes from the same browser", async () => {
		const key = aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 1,
					myVote: "No",
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await user.click(theButton(await theRowOf(ADVANCED_REPORTING), "No"));

		await waitFor(() =>
			expect(sizingLogService.castVote).toHaveBeenCalledWith(
				GRAVITY_TEAM_ID,
				ADVANCED_REPORTING,
				{ answer: "No", channel: "Web", voterName: JONAS },
				key,
			),
		);
		expect(screen.queryByRole("dialog")).toBeNull();
	});

	// @us-11 @slice-11 @boundary @contract-shape:bounded-change
	it("keeps only the voter's name and a long random key in this browser", async () => {
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				theRowAfter(CONFIGURATION_MANAGEMENT, {
					voteCount: 1,
					myVote: "Yes",
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);
		const prompt = await theNamePrompt();
		await user.type(
			within(prompt).getByRole("textbox", { name: "Your name" }),
			JONAS,
		);
		await user.click(within(prompt).getByRole("button", { name: "Vote" }));
		await waitFor(() => expect(sizingLogService.castVote).toHaveBeenCalled());

		expect(Object.keys(localStorage)).toEqual([VOTER_STORAGE_KEY]);
		expect(theStoredVoter()?.name).toBe(JONAS);
		expect(theStoredVoter()?.key?.length ?? 0).toBeGreaterThanOrEqual(32);
	});

	// @us-11 @slice-11 @error @contract-shape:unbounded-preservation
	it("casts nothing and keeps nothing when the name prompt is closed", async () => {
		const sizingLogService = aSizingLogService();
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await user.click(theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "No"));
		await user.click(
			within(await theNamePrompt()).getByRole("button", { name: "Cancel" }),
		);

		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
		expect(sizingLogService.castVote).not.toHaveBeenCalled();
		expect(localStorage).toHaveLength(0);
	});

	// @us-11 @slice-11 @error @contract-shape:unbounded-preservation
	it("will not vote under a blank name", async () => {
		const sizingLogService = aSizingLogService();
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);
		const prompt = await theNamePrompt();
		await user.type(
			within(prompt).getByRole("textbox", { name: "Your name" }),
			"   ",
		);

		expect(within(prompt).getByRole("button", { name: "Vote" })).toBeDisabled();
		expect(sizingLogService.castVote).not.toHaveBeenCalled();
	});

	// @us-11 @slice-11 @contract-shape:bounded-change
	it("lets the voter change the name their later votes carry, from the same browser", async () => {
		const key = aBrowserThatVotedBefore("Jonas");
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				theRowAfter(CONFIGURATION_MANAGEMENT, {
					voteCount: 1,
					myVote: "Yes",
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		const votes = await openTheVotesAndCommentsOf(
			user,
			CONFIGURATION_MANAGEMENT,
		);
		expect(votes).toHaveTextContent("Voting as Jonas");
		await user.click(
			within(votes).getByRole("button", { name: "Change your name" }),
		);
		const prompt = await theNamePrompt();
		const name = within(prompt).getByRole("textbox", { name: "Your name" });
		expect(name).toHaveValue("Jonas");
		await user.clear(name);
		await user.type(name, JONAS);
		await user.click(within(prompt).getByRole("button", { name: "Save" }));
		await closeTheVotesAndComments(
			user,
			await screen.findByRole("dialog", { name: /Votes and comments/ }),
		);
		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);

		await waitFor(() =>
			expect(sizingLogService.castVote).toHaveBeenCalledWith(
				GRAVITY_TEAM_ID,
				CONFIGURATION_MANAGEMENT,
				{ answer: "Yes", channel: "Web", voterName: JONAS },
				key,
			),
		);
	});

	// @us-11 @slice-11 @error @contract-shape:unbounded-preservation
	it("leaves the row as it was and says why when a vote is refused", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			castVote: vi
				.fn()
				.mockRejectedValue(
					new ApiError(429, "Request failed with status code 429"),
				),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		const row = await theRowOf(CONFIGURATION_MANAGEMENT);
		await user.click(theButton(row, "Yes"));

		expect(await screen.findByRole("alert")).toHaveTextContent(
			"Too many votes or comments from this browser. Try again in a minute.",
		);
		expect(row).toHaveTextContent("No votes");
		expect(theButton(row, "Yes")).toHaveAttribute("aria-pressed", "false");
	});

	// @us-11 @slice-11 @error @contract-shape:unbounded-preservation
	it.each([
		{
			why: "the work left refinement",
			refusal: new ApiError(
				409,
				"Request failed with status code 409",
				undefined,
				undefined,
				"work-item-not-in-refinement",
			),
			said: "This ticket is no longer in grooming.",
		},
		{
			why: "the name is missing",
			refusal: new ApiError(
				400,
				"Request failed with status code 400",
				undefined,
				undefined,
				"voter-name-required",
			),
			said: "Please give your name (at most 100 characters).",
		},
		{
			why: "the name is too long",
			refusal: new ApiError(
				400,
				"Request failed with status code 400",
				undefined,
				undefined,
				"voter-name-too-long",
			),
			said: "Please give your name (at most 100 characters).",
		},
		{
			why: "the server failed for a reason of its own",
			refusal: new ApiError(500, "The sizing log could not be written"),
			said: "The sizing log could not be written",
		},
	])(
		"says in plain words why a vote was refused when $why",
		async ({ refusal, said }) => {
			terms.current = {
				...defaultRefinementTerms,
				[TERMINOLOGY_KEYS.WORK_ITEM]: "Ticket",
				[REFINEMENT_KEY]: "Grooming",
			};
			aBrowserThatVotedBefore(JONAS);
			const sizingLogService = aSizingLogService({
				castVote: vi.fn().mockRejectedValue(refusal),
			});
			const { user } = renderTheRefinementTab(
				gravitysRefinement(),
				sizingLogService,
			);

			await user.click(
				theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
			);

			expect(await screen.findByRole("alert")).toHaveTextContent(said);
		},
	);

	// @us-11 @slice-11 @error @contract-shape:bounded-change
	it("mints the browser's key where the page is not served over a secure connection", async () => {
		const realCrypto = globalThis.crypto;
		vi.stubGlobal("crypto", {
			getRandomValues: realCrypto.getRandomValues.bind(realCrypto),
		});
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				theRowAfter(CONFIGURATION_MANAGEMENT, {
					voteCount: 1,
					myVote: "Yes",
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await voteUnderTheName(user, CONFIGURATION_MANAGEMENT, "Yes", JONAS);

		await waitFor(() => expect(sizingLogService.castVote).toHaveBeenCalled());
		expect(theStoredVoter()?.key).toMatch(A_HEX_KEY);
	});

	// @us-11 @slice-11 @error @contract-shape:bounded-change
	it("still votes, and keeps the name for the page, when the browser refuses to store it", async () => {
		const storage = localStorage;
		vi.stubGlobal("localStorage", {
			getItem: (key: string) => storage.getItem(key),
			setItem: () => {
				throw new DOMException("Storage is full", "QuotaExceededError");
			},
		});
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				theRowAfter(CONFIGURATION_MANAGEMENT, {
					voteCount: 1,
					myVote: "Yes",
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await voteUnderTheName(user, CONFIGURATION_MANAGEMENT, "Yes", JONAS);
		await waitFor(() =>
			expect(sizingLogService.castVote).toHaveBeenCalledTimes(1),
		);
		await user.click(theButton(await theRowOf(ADVANCED_REPORTING), "No"));

		await waitFor(() =>
			expect(sizingLogService.castVote).toHaveBeenCalledTimes(2),
		);
		expect(screen.queryByRole("dialog")).toBeNull();
		const [first, second] = vi.mocked(sizingLogService.castVote).mock.calls;
		expect(first[2]).toEqual({
			answer: "Yes",
			channel: "Web",
			voterName: JONAS,
		});
		expect(second[2]).toEqual({
			answer: "No",
			channel: "Web",
			voterName: JONAS,
		});
		expect(first[3]).toMatch(A_HEX_KEY);
		expect(second[3]).toBe(first[3]);
	});

	// @us-11 @slice-11 @boundary @contract-shape:unbounded-preservation
	it("takes a name of at most 100 characters", async () => {
		const { user } = renderTheRefinementTab(gravitysRefinement());

		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);
		const prompt = await theNamePrompt();
		const name = within(prompt).getByRole("textbox", { name: "Your name" });
		await user.type(name, "x".repeat(101));

		expect(name).toHaveValue("x".repeat(100));
		expect(within(prompt).getByRole("button", { name: "Vote" })).toBeEnabled();
	});

	it("keeps the name as it was when changing it is cancelled", async () => {
		aBrowserThatVotedBefore("Jonas");
		const { user } = renderTheRefinementTab(gravitysRefinement());

		const votes = await openTheVotesAndCommentsOf(
			user,
			CONFIGURATION_MANAGEMENT,
		);
		await user.click(
			within(votes).getByRole("button", { name: "Change your name" }),
		);
		const prompt = await theNamePrompt();
		await user.type(
			within(prompt).getByRole("textbox", { name: "Your name" }),
			" Weber",
		);
		await user.click(within(prompt).getByRole("button", { name: "Cancel" }));

		await waitFor(() =>
			expect(
				screen.queryByRole("dialog", { name: "Who is voting?" }),
			).toBeNull(),
		);
		expect(
			screen.getByRole("dialog", { name: /Votes and comments/ }),
		).toHaveTextContent("Voting as Jonas ·");
		expect(theStoredVoter()?.name).toBe("Jonas");
	});

	it("words a refusal in the terms the instance uses by the time the vote is refused", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			castVote: vi
				.fn()
				.mockRejectedValue(
					new ApiError(
						409,
						"Request failed with status code 409",
						undefined,
						undefined,
						"work-item-not-in-refinement",
					),
				),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);
		terms.current = {
			...defaultRefinementTerms,
			[TERMINOLOGY_KEYS.WORK_ITEM]: "Ticket",
			[REFINEMENT_KEY]: "Grooming",
		};

		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);

		expect(await screen.findByRole("alert")).toHaveTextContent(
			"This ticket is no longer in grooming.",
		);
	});

	// @us-11 @slice-11 @boundary @contract-shape:unbounded-preservation
	it("will not save a name longer than 100 characters", async () => {
		aBrowserThatVotedBefore("x".repeat(101));
		const { user } = renderTheRefinementTab(gravitysRefinement());

		const votes = await openTheVotesAndCommentsOf(
			user,
			CONFIGURATION_MANAGEMENT,
		);
		await user.click(
			within(votes).getByRole("button", { name: "Change your name" }),
		);
		const prompt = await theNamePrompt();

		expect(within(prompt).getByRole("button", { name: "Save" })).toBeDisabled();
	});

	// @us-11 @slice-11 @error @contract-shape:bounded-change
	it("sends one vote at a time from a row, and offers the row's answers again once the server has answered", async () => {
		aBrowserThatVotedBefore(JONAS);
		let answer: (row: IRefinementRow) => void = () => {};
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockImplementation(
				() =>
					new Promise<IRefinementRow>((resolve) => {
						answer = resolve;
					}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		const row = await theRowOf(CONFIGURATION_MANAGEMENT);
		await user.click(theButton(row, "Yes"));
		fireEvent.click(theButton(row, "No"));

		expect(sizingLogService.castVote).toHaveBeenCalledTimes(1);
		for (const label of ["Yes", YES_IF, "No"]) {
			expect(theButton(row, label)).toBeDisabled();
		}
		expect(theButton(await theRowOf(ADVANCED_REPORTING), "No")).toBeEnabled();

		answer(
			theRowAfter(CONFIGURATION_MANAGEMENT, { voteCount: 1, myVote: "Yes" }),
		);

		const answered = await theRowOf(CONFIGURATION_MANAGEMENT);
		await waitFor(() => expect(theButton(answered, "No")).toBeEnabled());
		expect(theButton(answered, "Yes")).toHaveAttribute("aria-pressed", "true");
	});

	// @us-11 @slice-11 @boundary @contract-shape:pure-function
	it("names the Work Item each row's answers are for", async () => {
		renderTheRefinementTab(gravitysRefinement());

		const row = await theRowOf(CONFIGURATION_MANAGEMENT);

		expect(
			within(row).getByRole("group", {
				name: `Your vote on ${CONFIGURATION_MANAGEMENT}`,
			}),
		).toBeInTheDocument();
	});

	// @us-11 @slice-11 @boundary @contract-shape:pure-function
	it("counts one vote in the singular and more in the plural", async () => {
		renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow("GR-058", "User activity tracking", "Next", { voteCount: 1 }),
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 3,
				}),
			]),
		);

		expect(await theRowOf("GR-058")).toHaveTextContent(ONE_VOTE);
		expect(await theRowOf(ADVANCED_REPORTING)).toHaveTextContent("3 votes");
	});

	// @us-11 @slice-11 @kpi-OUT-5510-K4-votes-outside-the-meeting @contract-shape:bounded-change
	it("reports each vote the server took to usage data, saying no cadence is set yet", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				theRowAfter(CONFIGURATION_MANAGEMENT, {
					voteCount: 1,
					myVote: "Yes",
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);
		await user.click(theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "No"));

		await waitFor(() => expect(reporter.current).toHaveBeenCalledTimes(2));
		expect(reporter.current).toHaveBeenNthCalledWith(1, {
			name: VOTE_CAST,
			sizingMoment: "NoCadence",
		});
		expect(reporter.current).toHaveBeenNthCalledWith(2, {
			name: VOTE_CAST,
			sizingMoment: "NoCadence",
		});
	});

	// @us-12 @slice-12 @kpi-OUT-5510-K4-votes-outside-the-meeting @contract-shape:bounded-change
	it("reports a 'Yes, if…' to usage data once, and never what its condition says", async () => {
		const condition = "only if the PDF export moves to its own Work Item";
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				theRowAfter(CONFIGURATION_MANAGEMENT, {
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
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), YES_IF),
		);
		const prompt = await screen.findByRole("dialog", { name: YES_IF });
		await user.click(
			within(prompt).getByRole("textbox", { name: "Condition" }),
		);
		await user.paste(condition);
		await user.click(within(prompt).getByRole("button", { name: "Vote" }));

		await waitFor(() => expect(reporter.current).toHaveBeenCalled());
		expect(sizingLogService.castVote).toHaveBeenCalledOnce();
		expect(reporter.current).toHaveBeenCalledOnce();
		expect(reporter.current).toHaveBeenCalledWith({
			name: VOTE_CAST,
			sizingMoment: "NoCadence",
		});
		expect(JSON.stringify(reporter.current.mock.calls)).not.toContain(
			condition,
		);
	});

	// @us-11 @slice-11 @error @kpi-OUT-5510-K4-votes-outside-the-meeting @contract-shape:unbounded-preservation
	it("reports nothing for a vote the server refused", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			castVote: vi
				.fn()
				.mockRejectedValue(
					new Error("That Work Item is no longer in refinement"),
				),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);

		await screen.findByRole("alert");
		expect(reporter.current).not.toHaveBeenCalled();
	});

	// @us-11 @slice-11 @boundary @contract-shape:pure-function
	it("still names the Work Item a condition is asked for once it has left refinement", async () => {
		aBrowserThatVotedBefore(JONAS);
		let answer: (row: IVotedRow) => void = () => {};
		const sizingLogService = aSizingLogService({
			castVote: vi.fn(
				() =>
					new Promise<IVotedRow>((resolve) => {
						answer = resolve;
					}),
			),
		});
		const votesSayReady = {
			readySource: "Votes" as const,
			nextRefinementDate: THURSDAY_THE_EIGHTH,
			daysUntilNextRefinement: 4,
			need: aNeedOfFiveToEight({ verdict: "Below" }),
		};
		const { user, refinementService } = renderTheRefinementTab(
			gravitysRefinement(votesSayReady),
			sizingLogService,
		);

		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);
		await user.click(theButton(await theRowOf(ADVANCED_REPORTING), YES_IF));
		const prompt = await screen.findByRole("dialog", { name: YES_IF });
		expect(prompt).toHaveTextContent(
			`${ADVANCED_REPORTING} Advanced reporting module`,
		);

		// The Yes made its Work Item ready, so the tab reads the Refinement again, and somebody has
		// taken the other Work Item out of refinement in the meantime.
		vi.mocked(refinementService.getRefinement).mockResolvedValue(
			gravitysRefinement(votesSayReady, [
				aRow(CONFIGURATION_MANAGEMENT, "Configuration management", "Backlog", {
					voteCount: 1,
					readiness: "Ready",
				}),
			]),
		);
		answer({
			...theRowAfter(CONFIGURATION_MANAGEMENT, {
				voteCount: 1,
				readiness: "Ready",
			}),
			madeReady: true,
		});

		await waitFor(() =>
			expect(refinementService.getRefinement).toHaveBeenCalledTimes(2),
		);
		await waitFor(() =>
			expect(screen.queryByText(/^GR-051: /)).not.toBeInTheDocument(),
		);
		const stillAsking = screen.getByRole("dialog", { name: YES_IF });
		expect(stillAsking).toHaveTextContent(ADVANCED_REPORTING);
		expect(stillAsking).not.toHaveTextContent("Advanced reporting module");
	});
});

describe("A signed-in voter votes under their account", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		reporter.current = vi.fn();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	// @us-15 @slice-15 @driving_port @contract-shape:bounded-change
	it.skip("casts the vote at once without asking for a name, and keeps nothing in the browser", async () => {
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				theRowAfter(CONFIGURATION_MANAGEMENT, {
					voteCount: 1,
					myVote: "Yes",
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement({ voterIdentity: "Account" }),
			sizingLogService,
		);

		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);

		await waitFor(() =>
			expect(sizingLogService.castVote).toHaveBeenCalledTimes(1),
		);
		const [, , vote] = vi.mocked(sizingLogService.castVote).mock.calls[0];
		expect(vote).toEqual({ answer: "Yes", channel: "Web" });
		expect(screen.queryByRole("dialog")).toBeNull();
		expect(localStorage).toHaveLength(0);
	});

	// @us-15 @slice-15 @boundary @contract-shape:pure-function
	it.skip("offers no way to change a name, because the account is the name", async () => {
		const { user } = renderTheRefinementTab(
			gravitysRefinement({ voterIdentity: "Account" }),
		);

		expect(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		).toBeEnabled();
		const votes = await openTheVotesAndCommentsOf(
			user,
			CONFIGURATION_MANAGEMENT,
		);

		expect(votes).not.toHaveTextContent("Voting as");
		expect(
			within(votes).queryByRole("button", { name: "Change your name" }),
		).toBeNull();
	});
});

describe("The votes and comments of a Work Item", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		reporter.current = vi.fn();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	// @us-11 @slice-11 @driving_port @contract-shape:pure-function
	it("opens from the Votes cell and says how the votes split", async () => {
		aBrowserThatVotedBefore(JONAS);
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 4,
					myVote: "Yes",
					split: { yes: 3, yesBut: 0, no: 1 },
				}),
			]),
			aSizingLogService({
				getLog: vi.fn().mockResolvedValue({
					entries: [],
					voters: { yes: [JONAS, "Ana", "Mo"], yesBut: [], no: ["Lea"] },
				}),
			}),
		);

		expect(await theRowOf(ADVANCED_REPORTING)).toHaveTextContent("4 votes");
		const votes = await openTheVotesAndCommentsOf(user, ADVANCED_REPORTING);

		expect(votes).toHaveTextContent("3 Yes · 0 Yes, if… · 1 No");
		expect(within(votes).getByRole("button", { name: "Close" })).toBeEnabled();
	});

	// @us-11 @slice-11 @driving_port @contract-shape:pure-function
	it("names the Work Item it shows the votes of in its title", async () => {
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 4,
					split: { yes: 3, yesBut: 0, no: 1 },
				}),
			]),
		);

		const votes = await openTheVotesAndCommentsOf(user, ADVANCED_REPORTING);

		expect(votes).toHaveAccessibleName(
			`${ADVANCED_REPORTING} Advanced reporting module · Votes and comments`,
		);
	});
});

describe("A voter takes back their own vote", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		reporter.current = vi.fn();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	const jonasVotedOnConfigurationManagementOnly = () =>
		gravitysRefinement({}, [
			aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
				voteCount: 2,
			}),
			theRowAfter(CONFIGURATION_MANAGEMENT, { voteCount: 1, myVote: "Yes" }),
		]);

	// @us-16 @slice-16 @contract-shape:pure-function
	it.skip("offers to take back a vote only where the reader has one", async () => {
		aBrowserThatVotedBefore(JONAS);
		const { user } = renderTheRefinementTab(
			jonasVotedOnConfigurationManagementOnly(),
		);

		const onTheirOwn = await openTheVotesAndCommentsOf(
			user,
			CONFIGURATION_MANAGEMENT,
		);
		expect(
			within(onTheirOwn).getByRole("button", { name: TAKE_BACK }),
		).toBeEnabled();
		await closeTheVotesAndComments(user, onTheirOwn);

		const onAnother = await openTheVotesAndCommentsOf(user, ADVANCED_REPORTING);
		expect(
			within(onAnother).queryByRole("button", { name: TAKE_BACK }),
		).toBeNull();
	});

	// @us-16 @slice-16 @driving_port @contract-shape:bounded-change
	it.skip("takes the vote back from this browser and shows the row as it now stands", async () => {
		const key = aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			takeBackMyVote: vi
				.fn()
				.mockResolvedValue(
					theRowAfter(CONFIGURATION_MANAGEMENT, { voteCount: 0, myVote: null }),
				),
		});
		const { user } = renderTheRefinementTab(
			jonasVotedOnConfigurationManagementOnly(),
			sizingLogService,
		);

		const votes = await openTheVotesAndCommentsOf(
			user,
			CONFIGURATION_MANAGEMENT,
		);
		await user.click(within(votes).getByRole("button", { name: TAKE_BACK }));

		const row = await theRowOf(CONFIGURATION_MANAGEMENT);
		await waitFor(() => expect(row).toHaveTextContent("No votes"));
		expect(sizingLogService.takeBackMyVote).toHaveBeenCalledWith(
			GRAVITY_TEAM_ID,
			CONFIGURATION_MANAGEMENT,
			key,
		);
		expect(screen.queryByRole("button", { name: TAKE_BACK })).toBeNull();
	});

	// @us-16 @slice-16 @error @contract-shape:unbounded-preservation
	it.skip("keeps the vote in place and says why when taking it back fails", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			takeBackMyVote: vi
				.fn()
				.mockRejectedValue(
					new Error("That Work Item is no longer in refinement"),
				),
		});
		const { user } = renderTheRefinementTab(
			jonasVotedOnConfigurationManagementOnly(),
			sizingLogService,
		);

		const votes = await openTheVotesAndCommentsOf(
			user,
			CONFIGURATION_MANAGEMENT,
		);
		await user.click(within(votes).getByRole("button", { name: TAKE_BACK }));

		expect(await screen.findByRole("alert")).toHaveTextContent(
			"That Work Item is no longer in refinement",
		);
		await closeTheVotesAndComments(user, votes);
		const row = await theRowOf(CONFIGURATION_MANAGEMENT);
		expect(row).toHaveTextContent(ONE_VOTE);
		expect(theButton(row, "Yes")).toHaveAttribute("aria-pressed", "true");
	});
});
