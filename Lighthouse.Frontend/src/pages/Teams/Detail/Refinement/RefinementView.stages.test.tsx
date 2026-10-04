import { screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IRefinementRow } from "../../../../models/Refinement/Refinement";
import {
	aRow,
	defaultRefinementTerms,
	gravitysRefinement,
	renderTheRefinementTab,
	theRowOf,
} from "../../../../tests/RefinementTabTestKit";
import { describeStageBreakdown } from "./StageBreakdown";

/**
 * Stages and votes are two signals the Refinement tab shows side by side. A Team that sets no stage rule
 * sees the tab as before: the votes alone say what is ready. Once the Team sets a stage rule, a Stage
 * column follows the State column, the votes column is headed "Votes say", the heading counts the Work
 * Items whose stage is Ready, and a line under it breaks the list down by stage. Where the two signals
 * tell a different story the row carries a marker that says so in words, not colour alone.
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

// The marker's words are still to be approved with the sketch; the test holds the meaning, not the wording.
const SIGNALS_DISAGREE = /stage and (the )?votes disagree/i;

const staged = (
	referenceId: string,
	name: string,
	state: string,
	stage: IRefinementRow["stage"],
	votes: Partial<IRefinementRow> = {},
) =>
	aRow(referenceId, name, state, { stage, signalsDisagree: false, ...votes });

/**
 * Gravity with its stage rules: two Work Items Ready, three Being refined, four Waiting. GR-059 is Ready
 * by its stage while its votes still want more Yes votes, so the signals disagree on it.
 */
const gravityWithStages = () =>
	gravitysRefinement(
		{
			stagesConfigured: true,
			readySource: "Stages",
			readyCount: 2,
			readyByVotesCount: 1,
		},
		[
			staged("GR-058", "User activity tracking", "Next", "Ready", {
				voteCount: 3,
				readiness: "Ready",
				missingVotes: null,
			}),
			staged("GR-059", "Advanced search filters", "Next", "Ready", {
				voteCount: 1,
				readiness: "MoreYesNeeded",
				missingVotes: 2,
				signalsDisagree: true,
			}),
			staged(
				"GR-051",
				"Advanced reporting module",
				"Analysing",
				"BeingRefined",
			),
			staged("GR-054", "Public API versioning", "Analysing", "BeingRefined"),
			staged("GR-055", "Audit trail", "Analysing", "BeingRefined"),
			staged("GR-073", "Configuration management", "Backlog", "Waiting"),
			staged("GR-074", "Load testing framework", "Backlog", "Waiting"),
			staged("GR-075", "Bulk import", "Backlog", "Waiting"),
			staged("GR-076", "Dark mode", "Backlog", "Waiting"),
		],
	);

// The votes column can say "Ready" too, so a stage assertion has to look inside the Stage cell alone.
const theStageCellOf = async (referenceId: string): Promise<HTMLElement> => {
	const cell = (await theRowOf(referenceId)).querySelector<HTMLElement>(
		'[role="gridcell"][data-field="stage"]',
	);
	if (cell === null) {
		throw new Error(`${referenceId} has no Stage cell`);
	}
	return cell;
};

const columnHeaders = () =>
	screen
		.getAllByRole("columnheader")
		.map((header) => header.textContent?.trim() ?? "");

describe("The Refinement tab shows stages beside the votes", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		reporter.current = vi.fn();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	// @us-03 @slice-03 @driving_port @contract-shape:pure-function
	it("counts the Work Items whose stage is Ready in the heading and breaks the list down by stage", async () => {
		renderTheRefinementTab(gravityWithStages());

		expect(
			await screen.findByRole("heading", {
				name: /^9 Work Items in Refinement · 2 ready$/,
			}),
		).toBeVisible();
		expect(
			screen.getByText(/^2 Ready · 3 Being refined · 4 Waiting$/),
		).toBeVisible();
	});

	// @us-03 @slice-03 @driving_port @contract-shape:pure-function
	it("puts a Stage column straight after State and heads the votes column Votes say", async () => {
		renderTheRefinementTab(gravityWithStages());
		await theRowOf("GR-058");

		const headers = columnHeaders();
		expect(headers[headers.indexOf("State") + 1]).toBe("Stage");
		expect(headers).toContain("Votes say");
		expect(headers).not.toContain("Readiness");
	});

	// @us-03 @slice-03 @driving_port @contract-shape:pure-function
	it("says each row's stage in words", async () => {
		renderTheRefinementTab(gravityWithStages());

		expect(await theStageCellOf("GR-058")).toHaveTextContent(/^Ready$/);
		expect(await theStageCellOf("GR-051")).toHaveTextContent(/^Being refined$/);
		expect(await theStageCellOf("GR-073")).toHaveTextContent(/^Waiting$/);
	});

	// @us-03 @slice-03 @driving_port @contract-shape:pure-function
	it("marks a row whose stage and votes disagree, in words a screen reader reads too", async () => {
		renderTheRefinementTab(gravityWithStages());

		expect(
			within(await theRowOf("GR-059")).getByLabelText(SIGNALS_DISAGREE),
		).toBeInTheDocument();
	});


	// @us-03 @slice-03 @boundary @contract-shape:pure-function
	it("marks only the row where the two signals disagree", async () => {
		renderTheRefinementTab(gravityWithStages());

		expect(
			within(await theRowOf("GR-059")).getByLabelText(SIGNALS_DISAGREE),
		).toBeInTheDocument();
		for (const agreeing of ["GR-058", "GR-051", "GR-073"]) {
			expect(
				within(await theRowOf(agreeing)).queryByLabelText(SIGNALS_DISAGREE),
			).not.toBeInTheDocument();
		}
	});

	// @us-03 @slice-03 @boundary @contract-shape:pure-function
	// Nobody has voted on GR-060, so its votes hold no opinion yet: its Ready stage carries no marker.
	it("marks no row whose stage is Ready when nobody has voted on it yet", async () => {
		renderTheRefinementTab(
			gravitysRefinement(
				{ stagesConfigured: true, readySource: "Stages", readyCount: 2 },
				[
					staged("GR-059", "Advanced search filters", "Next", "Ready", {
						voteCount: 1,
						readiness: "MoreYesNeeded",
						missingVotes: 2,
						signalsDisagree: true,
					}),
					staged("GR-060", "Saved searches", "Next", "Ready", {
						voteCount: 0,
						readiness: "MoreYesNeeded",
						missingVotes: 3,
					}),
				],
			),
		);

		expect(
			within(await theRowOf("GR-059")).getByLabelText(SIGNALS_DISAGREE),
		).toBeInTheDocument();
		expect(
			within(await theRowOf("GR-060")).queryByLabelText(SIGNALS_DISAGREE),
		).not.toBeInTheDocument();
	});

	// @us-03 @slice-03 @boundary @contract-shape:pure-function
	it("says nobody is Ready yet rather than leaving the stage count out when no Work Item matches the Ready rule", async () => {
		renderTheRefinementTab(
			gravitysRefinement(
				{ stagesConfigured: true, readySource: "Stages", readyCount: 0 },
				[
					staged(
						"GR-051",
						"Advanced reporting module",
						"Analysing",
						"BeingRefined",
					),
					staged("GR-073", "Configuration management", "Backlog", "Waiting"),
				],
			),
		);

		expect(
			await screen.findByRole("heading", {
				name: /^2 Work Items in Refinement · 0 ready$/,
			}),
		).toBeVisible();
		expect(
			screen.getByText(/^0 Ready · 1 Being refined · 1 Waiting$/),
		).toBeVisible();
	});

	// @us-03 @us-13 @slice-03 @boundary @contract-shape:unbounded-preservation
	it("shows the tab as before when the Team sets no stage rule: no Stage column, no breakdown, ready by votes", async () => {
		renderTheRefinementTab(
			gravitysRefinement({
				stagesConfigured: false,
				readySource: "Votes",
				readyCount: 0,
				readyByVotesCount: 0,
			}),
		);

		expect(
			await screen.findByRole("heading", {
				name: /^3 Work Items in Refinement · 0 ready by votes$/,
			}),
		).toBeVisible();
		await theRowOf("GR-058");
		expect(columnHeaders()).not.toContain("Stage");
		expect(columnHeaders()).toContain("Readiness");
		expect(screen.queryByText(/ Being refined · /)).not.toBeInTheDocument();
		expect(screen.queryByLabelText(SIGNALS_DISAGREE)).not.toBeInTheDocument();
	});

	// @us-03 @slice-03 @boundary @contract-shape:pure-function
	it("says the Team's own words in the heading and the breakdown", async () => {
		terms.current = {
			...defaultRefinementTerms,
			workItem: "Ticket",
			workItems: "Tickets",
			refinement: "Grooming",
		};
		renderTheRefinementTab(gravityWithStages());

		expect(
			await screen.findByRole("heading", {
				name: /^9 Tickets in Grooming · 2 ready$/,
			}),
		).toBeVisible();
	});
});

describe("The stage breakdown line", () => {
	const rowIn = (stage: IRefinementRow["stage"]) =>
		aRow("GR-1", "Any", "Next", { stage });

	it.each([
		{ stages: [], line: "0 Ready · 0 Being refined · 0 Waiting" },
		{ stages: ["Ready"], line: "1 Ready · 0 Being refined · 0 Waiting" },
		{
			stages: ["Waiting", "BeingRefined", "Waiting", "Ready"],
			line: "1 Ready · 1 Being refined · 2 Waiting",
		},
		{
			stages: [null, "Waiting"],
			line: "0 Ready · 0 Being refined · 1 Waiting",
		},
	] as { stages: IRefinementRow["stage"][]; line: string }[])(
		"reads $line for the stages $stages",
		({ stages, line }) => {
			expect(describeStageBreakdown(stages.map(rowIn))).toBe(line);
		},
	);
});
