import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { vi } from "vitest";
import SnackbarErrorHandler from "../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type {
	IRefinementNeed,
	IRefinementRow,
	IRefinementView,
	ISizingLog,
	NeedUnavailableReason,
} from "../models/Refinement/Refinement";
import { Team } from "../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../models/TerminologyKeys";
import RefinementView from "../pages/Teams/Detail/Refinement/RefinementView";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IRefinementService } from "../services/Api/RefinementService";
import type { ISizingLogService } from "../services/Api/SizingLogService";
import {
	createMockApiServiceContext,
	createMockFeatureService,
	createMockRbacService,
} from "./MockApiServiceProvider";

/**
 * Team Gravity's Refinement tab with sizing votes, for the tab's acceptance tests. The tab is rendered
 * through its real component tree; only what crosses the wire is stood in for - the tab's read and the
 * sizing log's writes - so what a test checks is what a voter sees and what the browser sends.
 *
 * The voter's name and key live in this browser's storage under one entry; tests clear storage before
 * each case so every case starts as a browser that has never voted.
 */

export const VOTER_STORAGE_KEY = "lighthouse:refinement:voter";

// Spelled out: the keys join the shared list together with the words the server seeds for them.
export const REFINEMENT_KEY = "refinement";

export const defaultRefinementTerms: Record<string, string> = {
	[TERMINOLOGY_KEYS.WORK_ITEM]: "Work Item",
	[TERMINOLOGY_KEYS.WORK_ITEMS]: "Work Items",
	[TERMINOLOGY_KEYS.SLE]: "SLE",
	[TERMINOLOGY_KEYS.CYCLE_TIME]: "Cycle Time",
	[REFINEMENT_KEY]: "Refinement",
	[TERMINOLOGY_KEYS.TEAM]: "Team",
};

export const GRAVITY_TEAM_ID = 7;

export const aRow = (
	referenceId: string,
	name: string,
	state: string,
	votes: Partial<IRefinementRow> = {},
): IRefinementRow => ({
	referenceId,
	name,
	url: `https://tracker.example/browse/${referenceId}`,
	state,
	parentReferenceId: "",
	voteCount: 0,
	myVote: null,
	split: { yes: 0, yesBut: 0, no: 0 },
	readiness: "MoreYesNeeded",
	missingVotes: 3,
	hasComments: false,
	hasOpenQuestion: false,
	...votes,
});

/** Gravity's refinement as a browser without sign-in reads it: three Work Items, nobody has voted. */
export const gravitysRefinement = (
	overrides: Partial<IRefinementView> = {},
	rows: IRefinementRow[] = [
		aRow("GR-058", "User activity tracking", "Next"),
		aRow("GR-051", "Advanced reporting module", "Analysing"),
		aRow("GR-073", "Configuration management", "Backlog"),
	],
): IRefinementView => ({
	refinementConfigured: true,
	workItems: rows,
	yardstick: { source: "Sle", days: 7, probability: 85 },
	voterIdentity: "SelfDeclared",
	readyByVotesCount: 0,
	...overrides,
});

/** Gravity's six Work Items in refinement, in backlog order. */
export const gravitysSixWorkItems = (): IRefinementRow[] => [
	aRow("GR-058", "User activity tracking", "Next"),
	aRow("GR-059", "Advanced search filters", "Next"),
	aRow("GR-051", "Advanced reporting module", "Analysing"),
	aRow("GR-054", "Public API versioning", "Analysing"),
	aRow("GR-073", "Configuration management", "Backlog"),
	aRow("GR-074", "Load testing framework", "Backlog"),
];

/** Likely to pull 5 to 8 Work Items before Gravity's next Refinement, read at 50% and 85%. */
export const aNeedOfFiveToEight = (
	overrides: Partial<IRefinementNeed> = {},
): IRefinementNeed => ({
	verdict: "Below",
	unavailableReason: null,
	low: 5,
	high: 8,
	lowPercentile: 50,
	highPercentile: 85,
	horizonWorkingDays: 4,
	...overrides,
});

/** No number, for this reason. */
export const noNeedBecause = (
	reason: NeedUnavailableReason,
): IRefinementNeed => ({
	verdict: null,
	unavailableReason: reason,
	low: null,
	high: null,
	lowPercentile: null,
	highPercentile: null,
	horizonWorkingDays: null,
});

/** Gravity refines on Thursdays; today is Sunday 4 October 2026, so the next Refinement is in four days. */
export const THURSDAY_THE_EIGHTH = "2026-10-08";

export const SUNDAY_THE_FOURTH = new Date(2026, 9, 4, 9, 0, 0);

const teamGravity = () => {
	const team = new Team();
	team.id = GRAVITY_TEAM_ID;
	team.name = "Team Gravity";
	return team;
};

/** A sizing log that answers every write with the row as the server would now show it. */
export const aSizingLogService = (
	overrides: Partial<ISizingLogService> = {},
): ISizingLogService => ({
	castVote: vi.fn(),
	addComment: vi.fn(),
	takeBackMyVote: vi.fn(),
	getLog: vi.fn().mockResolvedValue({
		entries: [],
	} satisfies ISizingLog),
	...overrides,
});

/** Who is looking: somebody who may change Gravity's settings, or somebody who may only read them. */
export type Onlooker = "TeamAdmin" | "Reader";

const anRbacServiceFor = (onlooker: Onlooker) => {
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
		isRbacEnabled: true,
		isSystemAdmin: false,
		canCreateTeam: onlooker === "TeamAdmin",
		canCreatePortfolio: false,
		adminTeamIds: onlooker === "TeamAdmin" ? [GRAVITY_TEAM_ID] : [],
		adminPortfolioIds: [],
	});
	return rbacService;
};

export const renderTheRefinementTab = (
	refinement: IRefinementView,
	sizingLogService: ISizingLogService = aSizingLogService(),
	onlooker: Onlooker = "TeamAdmin",
) => {
	const refinementService: IRefinementService = {
		getRefinement: vi.fn().mockResolvedValue(refinement),
	};
	const featureService = createMockFeatureService();
	featureService.getFeaturesByReferences = vi.fn().mockResolvedValue([]);
	const user = userEvent.setup();
	const services = createMockApiServiceContext({
		refinementService,
		sizingLogService,
		featureService,
		rbacService: anRbacServiceFor(onlooker),
	});
	const tabOf = (team: Team) => (
		<SnackbarErrorHandler>
			<ApiServiceContext.Provider value={services}>
				<RefinementView team={team} />
			</ApiServiceContext.Provider>
		</SnackbarErrorHandler>
	);

	const { rerender } = render(tabOf(teamGravity()));

	// The Team page keeps the tab mounted when the address moves to another Team; only the Team changes.
	const moveToTeam = (team: Team) => rerender(tabOf(team));

	return { refinementService, sizingLogService, user, moveToTeam };
};

export const theRowOf = async (referenceId: string): Promise<HTMLElement> => {
	const name = await screen.findByText(new RegExp(`^${referenceId}: `));
	const row = name.closest('[role="row"]');
	if (row === null) {
		throw new Error(`${referenceId} is not shown inside a grid row`);
	}
	return row as HTMLElement;
};

/** A row's Warnings cell: one icon naming every reason the row needs attention, or nothing at all. */
export const theWarningsCellOf = async (
	referenceId: string,
): Promise<HTMLElement> => {
	const cell = (await theRowOf(referenceId)).querySelector<HTMLElement>(
		'[role="gridcell"][data-field="warnings"]',
	);
	if (cell === null) {
		throw new Error(`${referenceId} has no Warnings cell`);
	}
	return cell;
};

export const OPEN_QUESTION_WARNING =
	"Somebody asked a question and has not voted yet.";

export const theButton = (row: HTMLElement, name: string | RegExp) =>
	within(row).getByRole("button", { name });

/** The Votes cell names what it opens, whatever count and markers it shows beside it. */
export const VOTES_AND_COMMENTS = /Votes and comments/;

/** Clicks a row's Votes cell and returns the Votes and comments dialog it opens. */
export const openTheVotesAndCommentsOf = async (
	user: ReturnType<typeof userEvent.setup>,
	referenceId: string,
): Promise<HTMLElement> => {
	await user.click(theButton(await theRowOf(referenceId), VOTES_AND_COMMENTS));
	return await screen.findByRole("dialog", { name: VOTES_AND_COMMENTS });
};

/** What this browser keeps about its voter, or null when it keeps nothing. */
export const theStoredVoter = (): { name?: string; key?: string } | null => {
	const stored = localStorage.getItem(VOTER_STORAGE_KEY);
	return stored === null ? null : JSON.parse(stored);
};

export const aBrowserThatVotedBefore = (name: string) => {
	const key = "a".repeat(32) + "b".repeat(32);
	localStorage.setItem(VOTER_STORAGE_KEY, JSON.stringify({ name, key }));
	return key;
};
