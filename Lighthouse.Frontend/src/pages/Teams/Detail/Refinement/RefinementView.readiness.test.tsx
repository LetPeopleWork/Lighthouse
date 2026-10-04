import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IRefinementRow,
	RowReadiness,
} from "../../../../models/Refinement/Refinement";
import {
	aBrowserThatVotedBefore,
	aRow,
	aSizingLogService,
	defaultRefinementTerms,
	gravitysRefinement,
	renderTheRefinementTab,
	theButton,
	theRowOf,
} from "../../../../tests/RefinementTabTestKit";
import { tipsToReady } from "./useVoteCasting";

/**
 * What the votes make of each Work Item, as the Refinement tab says it: Ready, how many more Yes votes
 * or voters it needs, or that it needs discussion - shown to everybody, voted or not. The heading counts
 * the Work Items the votes have made Ready on the same line, so the tab still shows the heading and one
 * line above the list. The vote that tips a Work Item to Ready is reported to usage data from the
 * browser that cast it.
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
const READINESS_REACHED = "TeamSizingReadinessReached";
const VOTE_CAST = "TeamSizingVoteCast";

const configurationManagement = (votes: Partial<IRefinementRow>) =>
	aRow(CONFIGURATION_MANAGEMENT, "Configuration management", "Backlog", votes);

const castingYesTurnsItInto = async (
	before: Partial<IRefinementRow>,
	after: Partial<IRefinementRow>,
) => {
	aBrowserThatVotedBefore("Jonas Weber");
	const sizingLogService = aSizingLogService({
		castVote: vi.fn().mockResolvedValue(configurationManagement(after)),
	});
	const { user } = renderTheRefinementTab(
		gravitysRefinement({}, [configurationManagement(before)]),
		sizingLogService,
	);

	await user.click(theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"));
	await waitFor(() =>
		expect(sizingLogService.castVote).toHaveBeenCalledTimes(1),
	);
	await waitFor(() =>
		expect(reporter.current).toHaveBeenCalledWith({
			name: VOTE_CAST,
			sizingMoment: "NoCadence",
		}),
	);
};

describe("The Refinement tab says what the votes make of each Work Item", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		reporter.current = vi.fn();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	// @us-13 @slice-13 @driving_port @kpi-OUT-5510-K5-ready-before-the-day @contract-shape:pure-function
	it("says on each row whether it is Ready, what is still missing, or that it needs discussion", async () => {
		renderTheRefinementTab(
			gravitysRefinement({ readyByVotesCount: 1 }, [
				aRow("GR-058", "User activity tracking", "Next", {
					voteCount: 3,
					readiness: "Ready",
					missingVotes: null,
				}),
				aRow("GR-051", "Advanced reporting module", "Analysing", {
					voteCount: 1,
					readiness: "MoreYesNeeded",
					missingVotes: 2,
				}),
				aRow("GR-059", "Advanced search filters", "Next", {
					voteCount: 2,
					readiness: "MoreVotersNeeded",
					missingVotes: 1,
				}),
				aRow("GR-054", "Public API versioning", "Analysing", {
					voteCount: 4,
					readiness: "NeedsDiscussion",
					missingVotes: null,
				}),
			]),
		);

		expect(await theRowOf("GR-058")).toHaveTextContent("Ready");
		expect(await theRowOf("GR-051")).toHaveTextContent("2 more Yes needed");
		expect(await theRowOf("GR-059")).toHaveTextContent("1 more voter needed");
		expect(await theRowOf("GR-054")).toHaveTextContent("Needs discussion");
	});

	// @us-13 @slice-13 @boundary @contract-shape:pure-function
	it("says one missing Yes in the singular and missing voters in the plural", async () => {
		renderTheRefinementTab(
			gravitysRefinement({}, [
				aRow("GR-051", "Advanced reporting module", "Analysing", {
					readiness: "MoreYesNeeded",
					missingVotes: 1,
				}),
				aRow("GR-059", "Advanced search filters", "Next", {
					readiness: "MoreVotersNeeded",
					missingVotes: 2,
				}),
			]),
		);

		expect(await theRowOf("GR-051")).toHaveTextContent("1 more Yes needed");
		expect(await theRowOf("GR-059")).toHaveTextContent("2 more voters needed");
	});

	// @us-13 @slice-13 @contract-shape:pure-function
	it("gives readiness its own column, right after the votes", async () => {
		renderTheRefinementTab(gravitysRefinement());

		await theRowOf(CONFIGURATION_MANAGEMENT);
		const columns = screen
			.getAllByRole("columnheader")
			.map((header) => header.textContent?.trim());

		expect(columns).toContain("Readiness");
		expect(columns.indexOf("Readiness")).toBe(columns.indexOf("Votes") + 1);
	});

	// @us-13 @slice-13 @contract-shape:pure-function
	it("counts the Work Items the votes made Ready in the heading, on the heading's own line", async () => {
		renderTheRefinementTab(gravitysRefinement({ readyByVotesCount: 1 }));

		expect(
			await screen.findByRole("heading", {
				name: "3 Work Items in Refinement · 1 ready by votes",
			}),
		).toBeVisible();
		expect(screen.getAllByRole("heading", { level: 2 })).toHaveLength(1);
	});

	// @us-13 @slice-13 @boundary @contract-shape:pure-function
	it("says so when no Work Item is Ready by votes yet", async () => {
		renderTheRefinementTab(gravitysRefinement({ readyByVotesCount: 0 }));

		expect(
			await screen.findByRole("heading", {
				name: "3 Work Items in Refinement · 0 ready by votes",
			}),
		).toBeVisible();
	});

	// @us-13 @slice-13 @kpi-OUT-5510-K5-ready-before-the-day @contract-shape:bounded-change
	it("reports the vote that makes a Work Item Ready to usage data", async () => {
		await castingYesTurnsItInto(
			{ voteCount: 2, readiness: "MoreYesNeeded", missingVotes: 1 },
			{ voteCount: 3, myVote: "Yes", readiness: "Ready", missingVotes: null },
		);

		expect(reporter.current).toHaveBeenCalledWith({
			name: READINESS_REACHED,
			sizingMoment: "NoCadence",
		});
	});

	// @us-13 @slice-13 @boundary @kpi-OUT-5510-K5-ready-before-the-day @contract-shape:unbounded-preservation
	it("reports no readiness for a vote on a Work Item that was Ready already", async () => {
		await castingYesTurnsItInto(
			{ voteCount: 3, readiness: "Ready", missingVotes: null },
			{ voteCount: 4, myVote: "Yes", readiness: "Ready", missingVotes: null },
		);

		expect(reporter.current).not.toHaveBeenCalledWith(
			expect.objectContaining({ name: READINESS_REACHED }),
		);
	});

	// @us-13 @slice-13 @boundary @kpi-OUT-5510-K5-ready-before-the-day @contract-shape:unbounded-preservation
	it("reports no readiness for a vote that leaves the Work Item short", async () => {
		await castingYesTurnsItInto(
			{ voteCount: 0, readiness: "MoreYesNeeded", missingVotes: 3 },
			{
				voteCount: 1,
				myVote: "Yes",
				readiness: "MoreYesNeeded",
				missingVotes: 2,
			},
		);

		expect(reporter.current).not.toHaveBeenCalledWith(
			expect.objectContaining({ name: READINESS_REACHED }),
		);
	});
});

const READINESSES: RowReadiness[] = [
	"Ready",
	"MoreYesNeeded",
	"MoreVotersNeeded",
	"NeedsDiscussion",
];

const TIPPING_PAIRS = [
	"MoreYesNeeded -> Ready",
	"MoreVotersNeeded -> Ready",
	"NeedsDiscussion -> Ready",
];

describe("A vote makes a Work Item Ready only when it was not Ready before", () => {
	it.each(
		READINESSES.flatMap((before) =>
			READINESSES.map((after) => ({
				before,
				after,
				reported: TIPPING_PAIRS.includes(`${before} -> ${after}`),
			})),
		),
	)(
		"$before -> $after reports readiness: $reported",
		({ before, after, reported }) => {
			expect(tipsToReady(before, after)).toBe(reported);
		},
	);
});
