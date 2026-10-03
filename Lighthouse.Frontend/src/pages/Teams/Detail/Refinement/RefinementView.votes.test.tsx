import { screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IRefinementRow } from "../../../../models/Refinement/Refinement";
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
	theStoredVoter,
	VOTER_STORAGE_KEY,
} from "../../../../tests/RefinementTabTestKit";

/**
 * Casting, seeing and taking back a sizing vote on the Refinement tab. Every row in refinement offers
 * Yes, "Yes, but…" and No; once a name is known one click casts the vote. Without sign-in the first vote
 * asks for the voter's name, and the browser keeps that name and a random key so later votes need no
 * name and stay this browser's. With sign-in nobody is asked anything. Until you vote on a Work Item you
 * see only how many have; afterwards you see how they split. Only your own vote can be taken back.
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

const CONFIGURATION_MANAGEMENT = "GR-073";
const ADVANCED_REPORTING = "GR-051";
const JONAS = "Jonas Weber";
const YES_BUT = "Yes, but…";
const ONE_VOTE = "1 vote";
const VOTE_CAST = "TeamSizingVoteCast";

const theRowAfter = (
	referenceId: string,
	votes: Partial<IRefinementRow>,
): IRefinementRow =>
	aRow(referenceId, "Configuration management", "Backlog", votes);

const theNamePrompt = () => screen.findByRole("dialog");

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

	// @us-11 @slice-11 @driving_port @contract-shape:pure-function
	it.skip("offers Yes, Yes but… and No on every Work Item in refinement, beside how many have voted", async () => {
		renderTheRefinementTab(gravitysRefinement());

		for (const referenceId of [
			"GR-058",
			ADVANCED_REPORTING,
			CONFIGURATION_MANAGEMENT,
		]) {
			const row = await theRowOf(referenceId);
			expect(theButton(row, "Yes")).toBeEnabled();
			expect(theButton(row, YES_BUT)).toBeEnabled();
			expect(theButton(row, "No")).toBeEnabled();
			expect(row).toHaveTextContent("No votes");
		}
		expect(
			screen.getByRole("columnheader", { name: "Your vote" }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("columnheader", { name: "Votes" }),
		).toBeInTheDocument();
	});

	// @us-11 @slice-11 @driving_port @kpi-OUT-5510-K4-votes-outside-the-meeting @contract-shape:bounded-change
	it.skip("asks for a name the first time, then casts the vote under it and shows it as the voter's own", async () => {
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
	it.skip("remembers the name, so the next vote takes one click and comes from the same browser", async () => {
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
	it.skip("keeps only the voter's name and a long random key in this browser", async () => {
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
	it.skip("casts nothing and keeps nothing when the name prompt is closed", async () => {
		const sizingLogService = aSizingLogService();
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), YES_BUT),
		);
		await user.click(
			within(await theNamePrompt()).getByRole("button", { name: "Cancel" }),
		);

		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
		expect(sizingLogService.castVote).not.toHaveBeenCalled();
		expect(localStorage).toHaveLength(0);
	});

	// @us-11 @slice-11 @error @contract-shape:unbounded-preservation
	it.skip("will not vote under a blank name", async () => {
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
	it.skip("lets the voter change the name their later votes carry, from the same browser", async () => {
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

		await user.click(
			await screen.findByRole("button", { name: /change your name/i }),
		);
		const prompt = await theNamePrompt();
		const name = within(prompt).getByRole("textbox", { name: "Your name" });
		expect(name).toHaveValue("Jonas");
		await user.clear(name);
		await user.type(name, JONAS);
		await user.click(within(prompt).getByRole("button", { name: "Save" }));
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
	it.skip("leaves the row as it was and says why when a vote is refused", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			castVote: vi
				.fn()
				.mockRejectedValue(
					new Error("Too many votes from this browser, try again in a minute"),
				),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement(),
			sizingLogService,
		);

		const row = await theRowOf(CONFIGURATION_MANAGEMENT);
		await user.click(theButton(row, "Yes"));

		expect(await screen.findByRole("alert")).toHaveTextContent(
			"Too many votes from this browser, try again in a minute",
		);
		expect(row).toHaveTextContent("No votes");
		expect(theButton(row, "Yes")).toHaveAttribute("aria-pressed", "false");
	});

	// @us-11 @slice-11 @boundary @contract-shape:pure-function
	it.skip("counts one vote in the singular and more in the plural", async () => {
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
	it.skip("reports each vote the server took to usage data, saying no cadence is set yet", async () => {
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

	// @us-11 @slice-11 @error @kpi-OUT-5510-K4-votes-outside-the-meeting @contract-shape:unbounded-preservation
	it.skip("reports nothing for a vote the server refused", async () => {
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
		renderTheRefinementTab(gravitysRefinement({ voterIdentity: "Account" }));

		const row = await theRowOf(CONFIGURATION_MANAGEMENT);

		expect(theButton(row, "Yes")).toBeEnabled();
		expect(
			screen.queryByRole("button", { name: /change your name/i }),
		).toBeNull();
	});
});

describe("Somebody who has not voted sees how many have, not how they split", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		reporter.current = vi.fn();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	// @us-14 @slice-14 @contract-shape:pure-function
	it.skip("shows only the count on a Work Item the reader has not voted on", async () => {
		renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 3,
					split: null,
				}),
			]),
		);

		const row = await theRowOf(ADVANCED_REPORTING);

		expect(row).toHaveTextContent("3 votes");
		expect(within(row).queryByText(/\d+ Yes\b/)).toBeNull();
	});

	// @us-14 @slice-14 @contract-shape:bounded-change
	it.skip("shows the split once the reader's own vote is in", async () => {
		aBrowserThatVotedBefore(JONAS);
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 4,
					myVote: "Yes",
					split: { yes: 3, yesBut: 0, no: 1 },
				}),
			),
		});
		const { user } = renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow(ADVANCED_REPORTING, "Advanced reporting module", "Analysing", {
					voteCount: 3,
					split: null,
				}),
			]),
			sizingLogService,
		);

		await user.click(theButton(await theRowOf(ADVANCED_REPORTING), "Yes"));

		const row = await theRowOf(ADVANCED_REPORTING);
		await waitFor(() =>
			expect(row).toHaveTextContent("3 Yes · 0 Yes, but… · 1 No"),
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
		renderTheRefinementTab(jonasVotedOnConfigurationManagementOnly());

		expect(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Take back my vote"),
		).toBeEnabled();
		expect(
			within(await theRowOf(ADVANCED_REPORTING)).queryByRole("button", {
				name: "Take back my vote",
			}),
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

		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Take back my vote"),
		);

		const row = await theRowOf(CONFIGURATION_MANAGEMENT);
		await waitFor(() => expect(row).toHaveTextContent("No votes"));
		expect(sizingLogService.takeBackMyVote).toHaveBeenCalledWith(
			GRAVITY_TEAM_ID,
			CONFIGURATION_MANAGEMENT,
			key,
		);
		expect(
			within(row).queryByRole("button", { name: "Take back my vote" }),
		).toBeNull();
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

		const row = await theRowOf(CONFIGURATION_MANAGEMENT);
		await user.click(theButton(row, "Take back my vote"));

		expect(await screen.findByRole("alert")).toHaveTextContent(
			"That Work Item is no longer in refinement",
		);
		expect(row).toHaveTextContent(ONE_VOTE);
		expect(theButton(row, "Yes")).toHaveAttribute("aria-pressed", "true");
	});
});
