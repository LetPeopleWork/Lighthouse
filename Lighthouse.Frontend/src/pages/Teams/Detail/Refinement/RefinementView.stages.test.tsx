import { screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IRefinementRow } from "../../../../models/Refinement/Refinement";
import {
	aRow,
	defaultRefinementTerms,
	gravitysRefinement,
	OPEN_QUESTION_WARNING,
	renderTheRefinementTab,
	theRowOf,
	theWarningsCellOf,
} from "../../../../tests/RefinementTabTestKit";
import { describeStageBreakdown } from "./stageBreakdown";
import { describeDisagreement } from "./stageWording";

/**
 * Stages and votes are two signals the Refinement tab shows side by side. A Team that sets no stage rule
 * sees the tab as before: the votes alone say what is ready. Once the Team sets a stage rule, a Stage
 * column follows the State column, the votes column is headed "Votes say", and the heading is the list
 * broken down by stage, with no separate count. Where the two signals
 * tell a different story the row's Warnings cell says so in words, not colour alone; the Stage cell
 * holds the stage and nothing else.
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

// Either way round the two signals disagree, the reason names the stage; no other warning does.
const SIGNALS_DISAGREE = /the stage (says|is still)/i;

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
	it("heads the tab with the list broken down by stage, and counts it nowhere else", async () => {
		renderTheRefinementTab(gravityWithStages());

		expect(
			await screen.findByRole("heading", {
				name: /^2 Ready · 3 Being refined · 4 Waiting$/,
			}),
		).toBeVisible();
		expect(screen.getAllByText(/ Being refined · /)).toHaveLength(1);
		expect(screen.queryByText(/ in Refinement/)).not.toBeInTheDocument();
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
	it("warns about a row whose stage and votes disagree, in words a screen reader reads too, and leaves the Stage cell to the stage", async () => {
		renderTheRefinementTab(gravityWithStages());

		expect(
			within(await theWarningsCellOf("GR-059")).getByRole("button", {
				name: SIGNALS_DISAGREE,
			}),
		).toBeInTheDocument();
		const stageCell = await theStageCellOf("GR-059");
		expect(stageCell).toHaveTextContent(/^Ready$/);
		expect(
			within(stageCell).queryByLabelText(SIGNALS_DISAGREE),
		).not.toBeInTheDocument();
	});

	// @us-03 @slice-03 @driving_port @contract-shape:pure-function
	it("tells a screen reader which way the stage and votes disagree, not only that they do", async () => {
		renderTheRefinementTab(
			gravitysRefinement(
				{ stagesConfigured: true, readySource: "Stages", readyCount: 1 },
				[
					staged("GR-059", "Advanced search filters", "Next", "Ready", {
						voteCount: 1,
						readiness: "MoreYesNeeded",
						missingVotes: 2,
						signalsDisagree: true,
					}),
					staged("GR-073", "Configuration management", "Backlog", "Waiting", {
						voteCount: 3,
						readiness: "Ready",
						missingVotes: null,
						signalsDisagree: true,
					}),
				],
			),
		);

		expect(
			within(await theWarningsCellOf("GR-059")).getByRole("button", {
				name: "The stage says Ready, but the votes don't agree yet.",
			}),
		).toBeInTheDocument();
		expect(
			within(await theWarningsCellOf("GR-073")).getByRole("button", {
				name: "The votes say Ready, but the stage is still Waiting.",
			}),
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
				name: /^0 Ready · 1 Being refined · 1 Waiting$/,
			}),
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
	it("keeps the breakdown as the heading when the Team has its own words", async () => {
		terms.current = {
			...defaultRefinementTerms,
			workItem: "Ticket",
			workItems: "Tickets",
			refinement: "Grooming",
		};
		renderTheRefinementTab(gravityWithStages());

		expect(
			await screen.findByRole("heading", {
				name: /^2 Ready · 3 Being refined · 4 Waiting$/,
			}),
		).toBeVisible();
		expect(screen.queryByText(/ Tickets in Grooming/)).not.toBeInTheDocument();
	});
});

/**
 * One Warnings column, last in the grid, says what needs attention on a row: the stage and the votes
 * disagree, or somebody asked a question and has not voted yet. A row with nothing to warn about shows
 * nothing at all there, and sorting by it brings the rows that need attention together.
 */
describe("The Warnings column", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		reporter.current = vi.fn();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	const disagreeing = staged(
		"GR-059",
		"Advanced search filters",
		"Next",
		"Ready",
		{
			voteCount: 1,
			readiness: "MoreYesNeeded",
			missingVotes: 2,
			signalsDisagree: true,
		},
	);
	const asked = staged(
		"GR-054",
		"Public API versioning",
		"Analysing",
		"BeingRefined",
		{
			hasComments: true,
			hasOpenQuestion: true,
		},
	);

	// @us-12 @slice-12 @boundary @contract-shape:pure-function
	it("shows nothing at all on a row with nothing to warn about", async () => {
		renderTheRefinementTab(gravityWithStages());

		const clean = await theWarningsCellOf("GR-058");
		expect(within(clean).queryByRole("button")).not.toBeInTheDocument();
		expect(clean).toHaveTextContent(/^$/);
	});

	// @us-03 @us-12 @slice-12 @driving_port @contract-shape:pure-function
	it("lists every reason when a row has several, in one icon a screen reader reads whole", async () => {
		const { user } = renderTheRefinementTab(
			gravitysRefinement({ stagesConfigured: true, readySource: "Stages" }, [
				{ ...disagreeing, hasComments: true, hasOpenQuestion: true },
			]),
		);
		const reasons = [describeDisagreement("Ready"), OPEN_QUESTION_WARNING];

		const warning = within(await theWarningsCellOf("GR-059")).getByRole(
			"button",
			{ name: reasons.join(" ") },
		);
		await user.hover(warning);

		const listed = within(await screen.findByRole("tooltip")).getAllByRole(
			"listitem",
		);
		expect(listed.map((item) => item.textContent)).toEqual(reasons);
	});

	// @us-12 @slice-12 @driving_port @contract-shape:pure-function
	it("says a single reason as one sentence", async () => {
		const { user } = renderTheRefinementTab(
			gravitysRefinement({ stagesConfigured: true, readySource: "Stages" }, [
				disagreeing,
			]),
		);

		await user.hover(
			within(await theWarningsCellOf("GR-059")).getByRole("button"),
		);

		const tooltip = await screen.findByRole("tooltip");
		expect(tooltip).toHaveTextContent(
			/^The stage says Ready, but the votes don't agree yet\.$/,
		);
		expect(within(tooltip).queryByRole("list")).not.toBeInTheDocument();
	});

	// @us-12 @slice-12 @driving_port @contract-shape:pure-function
	it("brings the rows with warnings together when sorted by it", async () => {
		const { user } = renderTheRefinementTab(
			gravitysRefinement({ stagesConfigured: true, readySource: "Stages" }, [
				staged("GR-058", "User activity tracking", "Next", "Ready"),
				disagreeing,
				staged(
					"GR-051",
					"Advanced reporting module",
					"Analysing",
					"BeingRefined",
				),
				asked,
				staged("GR-073", "Configuration management", "Backlog", "Waiting"),
			]),
		);
		await theRowOf("GR-058");

		await user.click(screen.getByRole("columnheader", { name: "Warnings" }));

		const shown = screen
			.getAllByRole("row")
			.filter((row) => within(row).queryAllByRole("link").length > 0)
			.map(
				(row) =>
					within(row).getAllByRole("link")[0].textContent?.split(":")[0] ?? "",
			);
		const warned = [shown.indexOf("GR-059"), shown.indexOf("GR-054")].sort(
			(a, b) => a - b,
		);
		expect(shown).toHaveLength(5);
		expect(warned[1] - warned[0]).toBe(1);
	});

	// @us-12 @slice-12 @driving_port @contract-shape:pure-function
	it("puts the clean rows first when sorted by it, and the rows with warnings first when sorted again", async () => {
		const { user } = renderTheRefinementTab(
			gravitysRefinement({ stagesConfigured: true, readySource: "Stages" }, [
				disagreeing,
				staged("GR-058", "User activity tracking", "Next", "Ready"),
				asked,
				staged("GR-073", "Configuration management", "Backlog", "Waiting"),
			]),
		);
		await theRowOf("GR-058");
		const shownInOrder = () =>
			screen
				.getAllByRole("row")
				.filter((row) => within(row).queryAllByRole("link").length > 0)
				.map(
					(row) =>
						within(row).getAllByRole("link")[0].textContent?.split(":")[0] ??
						"",
				);
		const inAnyOrder = (referenceIds: string[]) =>
			[...referenceIds].sort((a, b) => a.localeCompare(b));
		const theWarned = ["GR-054", "GR-059"];
		const warningsHeader = screen.getByRole("columnheader", {
			name: "Warnings",
		});

		await user.click(warningsHeader);
		expect(inAnyOrder(shownInOrder().slice(2))).toEqual(theWarned);

		await user.click(warningsHeader);
		expect(inAnyOrder(shownInOrder().slice(0, 2))).toEqual(theWarned);
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
